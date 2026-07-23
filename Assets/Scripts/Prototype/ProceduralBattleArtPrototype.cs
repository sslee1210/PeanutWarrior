using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Loads the illustrated battle atlas and applies it directly to the real
    /// RuntimeWorldViewPrototype SpriteRenderers. The atlas loader deliberately
    /// removes invalid characters and per-chunk padding before rebuilding one
    /// valid Base64 payload, so Git line endings or chunk boundaries cannot leave
    /// the original circle placeholders visible.
    /// </summary>
    [DefaultExecutionOrder(25000)]
    public sealed class ProceduralBattleArtPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const int AtlasColumns = 6;
        private const int AtlasRows = 4;
        private const int AtlasChunkCount = 8;
        private const float WorldTileSize = 1.45f;

        private RuntimeWorldViewPrototype worldView;
        private StageFlowController stageFlow;
        private FieldInfo playerViewField;
        private FieldInfo enemyViewsField;
        private FieldInfo worldRootField;

        private readonly Sprite[] atlasSprites = new Sprite[AtlasColumns * AtlasRows];
        private readonly HashSet<int> cleanedRoots = new HashSet<int>();
        private readonly List<Transform> companions = new List<Transform>();

        private Texture2D atlasTexture;
        private GameObject worldRoot;
        private Transform environmentRoot;
        private Transform playerRoot;
        private Camera worldCamera;
        private int currentTheme = -1;
        private bool atlasReady;
        private bool reflectionReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<ProceduralBattleArtPrototype>() != null) return;

            GameObject root = new GameObject("Peanut Direct Battle Art Binder");
            DontDestroyOnLoad(root);
            root.AddComponent<ProceduralBattleArtPrototype>();
        }

        private void Awake()
        {
            atlasReady = LoadAtlas();
            if (atlasReady) return;

            Debug.LogError("[PeanutArt] Illustrated atlas could not be loaded.");
            enabled = false;
        }

        private void LateUpdate()
        {
            if (!atlasReady || !ResolveRuntimeView()) return;

            ApplyPlayerArt();
            ApplyEnemyArt();
            EnsureCompanions();
            AnimateCompanions();
            ApplyStageTheme();
        }

        private bool LoadAtlas()
        {
            StringBuilder sanitized = new StringBuilder(100000);

            for (int index = 0; index < AtlasChunkCount; index++)
            {
                string path = $"PeanutWarrior/AtlasChunks/peanut_battle_atlas_{index:00}";
                TextAsset chunk = Resources.Load<TextAsset>(path);
                if (chunk == null)
                {
                    Debug.LogError($"[PeanutArt] Missing Resources asset: {path}");
                    return false;
                }

                AppendBase64Payload(sanitized, chunk.text);
            }

            if (sanitized.Length == 0)
            {
                Debug.LogError("[PeanutArt] Atlas payload is empty after sanitizing the chunks.");
                return false;
            }

            int remainder = sanitized.Length % 4;
            if (remainder == 1)
            {
                Debug.LogError($"[PeanutArt] Atlas payload has an unrecoverable Base64 length: {sanitized.Length}.");
                return false;
            }

            if (remainder > 0)
                sanitized.Append('=', 4 - remainder);

            try
            {
                byte[] bytes = Convert.FromBase64String(sanitized.ToString());
                if (!HasPngSignature(bytes))
                {
                    Debug.LogError($"[PeanutArt] Decoded atlas is not a PNG. Byte count: {bytes.Length}.");
                    return false;
                }

                atlasTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "Peanut Battle Atlas",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                if (!atlasTexture.LoadImage(bytes, false))
                {
                    Debug.LogError("[PeanutArt] Texture2D.LoadImage returned false.");
                    return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PeanutArt] Atlas decode failed after sanitizing: {exception}");
                return false;
            }

            if (atlasTexture.width % AtlasColumns != 0 || atlasTexture.height % AtlasRows != 0)
            {
                Debug.LogError(
                    $"[PeanutArt] Atlas size {atlasTexture.width}x{atlasTexture.height} is not divisible by " +
                    $"{AtlasColumns} columns and {AtlasRows} rows.");
                return false;
            }

            int tileWidth = atlasTexture.width / AtlasColumns;
            int tileHeight = atlasTexture.height / AtlasRows;
            float pixelsPerUnit = tileWidth / WorldTileSize;

            for (int index = 0; index < atlasSprites.Length; index++)
            {
                int column = index % AtlasColumns;
                int rowFromTop = index / AtlasColumns;
                int x = column * tileWidth;
                int y = atlasTexture.height - ((rowFromTop + 1) * tileHeight);

                atlasSprites[index] = Sprite.Create(
                    atlasTexture,
                    new Rect(x, y, tileWidth, tileHeight),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                atlasSprites[index].name = $"PeanutBattleSprite_{index:00}";
            }

            Debug.Log(
                $"[PeanutArt] Atlas loaded: {atlasTexture.width}x{atlasTexture.height}, " +
                $"{atlasSprites.Length} sprites ready, sanitized payload {sanitized.Length} chars.");
            return true;
        }

        private static void AppendBase64Payload(StringBuilder destination, string source)
        {
            if (string.IsNullOrEmpty(source)) return;

            for (int index = 0; index < source.Length; index++)
            {
                char value = source[index];
                bool alphaNumeric =
                    (value >= 'A' && value <= 'Z') ||
                    (value >= 'a' && value <= 'z') ||
                    (value >= '0' && value <= '9');

                if (alphaNumeric || value == '+' || value == '/')
                    destination.Append(value);

                // '=' is intentionally discarded here. Padding is valid only once,
                // at the end of the complete payload, and is rebuilt after all chunks.
            }
        }

        private static bool HasPngSignature(byte[] bytes)
        {
            return bytes != null &&
                   bytes.Length >= 8 &&
                   bytes[0] == 0x89 &&
                   bytes[1] == 0x50 &&
                   bytes[2] == 0x4E &&
                   bytes[3] == 0x47 &&
                   bytes[4] == 0x0D &&
                   bytes[5] == 0x0A &&
                   bytes[6] == 0x1A &&
                   bytes[7] == 0x0A;
        }

        private bool ResolveRuntimeView()
        {
            if (worldView == null)
            {
                worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                reflectionReady = false;
            }

            if (worldView == null) return false;

            if (!reflectionReady)
            {
                Type type = typeof(RuntimeWorldViewPrototype);
                playerViewField = type.GetField("playerView", PrivateInstance);
                enemyViewsField = type.GetField("enemyViews", PrivateInstance);
                worldRootField = type.GetField("worldRoot", PrivateInstance);

                if (playerViewField == null || enemyViewsField == null || worldRootField == null)
                {
                    Debug.LogError("[PeanutArt] RuntimeWorldViewPrototype fields were not found.");
                    enabled = false;
                    return false;
                }

                reflectionReady = true;
            }

            worldRoot = worldRootField.GetValue(worldView) as GameObject;
            if (worldRoot == null) return false;

            if (environmentRoot == null)
            {
                Transform oldEnvironment = worldRoot.transform.Find("Illustrated Stage Environment");
                if (oldEnvironment != null) Destroy(oldEnvironment.gameObject);

                Transform directEnvironment = worldRoot.transform.Find("Direct Illustrated Stage Environment");
                if (directEnvironment != null) Destroy(directEnvironment.gameObject);

                environmentRoot = new GameObject("Direct Illustrated Stage Environment").transform;
                environmentRoot.SetParent(worldRoot.transform, false);
                worldCamera = Camera.main;
                currentTheme = -1;
            }

            return true;
        }

        private void ApplyPlayerArt()
        {
            object view = playerViewField.GetValue(worldView);
            SpriteRenderer body = ReadBody(view);
            if (body == null) return;

            playerRoot = body.transform.parent;
            ApplySprite(body, atlasSprites[0], 1.22f, false);
            MoveHud(playerRoot, 1.02f, 0.76f);
        }

        private void ApplyEnemyArt()
        {
            IDictionary views = enemyViewsField.GetValue(worldView) as IDictionary;
            if (views == null) return;

            int fallbackIndex = 0;
            foreach (DictionaryEntry pair in views)
            {
                object view = pair.Value;
                SpriteRenderer body = ReadBody(view);
                if (body == null)
                {
                    fallbackIndex++;
                    continue;
                }

                bool boss = ReadBool(view, "IsBoss");
                TextMesh label = ReadLabel(view);
                string labelText = label != null ? label.text : string.Empty;
                int spriteIndex = boss
                    ? ResolveBossSpriteIndex()
                    : ResolveMonsterSpriteIndex(labelText, fallbackIndex);

                ApplySprite(body, atlasSprites[spriteIndex], boss ? 1.20f : 1.02f, boss);
                MoveHud(body.transform.parent, boss ? 1.30f : 0.92f, boss ? 1.02f : 0.69f);
                fallbackIndex++;
            }
        }

        private void ApplySprite(SpriteRenderer body, Sprite sprite, float visualScale, bool boss)
        {
            if (body == null || sprite == null) return;

            Transform root = body.transform.parent;
            if (root != null) RemoveLegacyOverlay(root);

            body.gameObject.SetActive(true);
            body.sprite = sprite;
            body.color = Color.white;
            body.sortingOrder = boss ? 6 : 5;
            body.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            body.transform.localScale = Vector3.one * visualScale;

            Transform highlight = body.transform.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(false);
        }

        private void RemoveLegacyOverlay(Transform root)
        {
            if (root == null || !cleanedRoots.Add(root.GetInstanceID())) return;

            string[] names = { "Procedural Visual", "Illustrated Visual", "Illustrated Aura" };
            for (int index = 0; index < names.Length; index++)
            {
                Transform child = root.Find(names[index]);
                if (child != null) Destroy(child.gameObject);
            }
        }

        private static void MoveHud(Transform root, float labelY, float healthY)
        {
            if (root == null) return;

            Transform label = root.Find("Label");
            if (label != null) label.localPosition = new Vector3(0f, labelY, 0f);

            Transform health = root.Find("Health Back");
            if (health != null) health.localPosition = new Vector3(0f, healthY, 0f);

            Transform shadow = root.Find("Shadow");
            if (shadow != null) shadow.localPosition = new Vector3(0f, -0.55f, 0f);
        }

        private void EnsureCompanions()
        {
            if (worldRoot == null || playerRoot == null || companions.Count == 3) return;

            for (int index = companions.Count; index < 3; index++)
            {
                GameObject companion = new GameObject($"Illustrated Support Peanut {index + 1}");
                companion.transform.SetParent(worldRoot.transform, false);

                SpriteRenderer renderer = companion.AddComponent<SpriteRenderer>();
                renderer.sprite = atlasSprites[index + 1];
                renderer.color = Color.white;
                renderer.sortingOrder = 5;
                companion.transform.localScale = Vector3.one * 0.72f;
                companions.Add(companion.transform);
            }
        }

        private void AnimateCompanions()
        {
            if (playerRoot == null || companions.Count != 3) return;

            Vector3[] offsets =
            {
                new Vector3(-1.20f, -0.62f, 0f),
                new Vector3(0f, -1.02f, 0f),
                new Vector3(1.20f, -0.62f, 0f)
            };

            for (int index = 0; index < companions.Count; index++)
            {
                Transform companion = companions[index];
                if (companion == null) continue;

                float phase = Time.time * 0.45f + index * 2.094f;
                Vector3 target = playerRoot.position + offsets[index] +
                    new Vector3(Mathf.Cos(phase) * 0.08f, Mathf.Sin(phase * 1.6f) * 0.07f, 0f);

                companion.position = Vector3.Lerp(
                    companion.position,
                    target,
                    1f - Mathf.Exp(-8f * Time.deltaTime));

                float breathe = 1f + Mathf.Sin(Time.time * 4.2f + index) * 0.025f;
                companion.localScale = Vector3.one * 0.72f * breathe;
                companion.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * 2f);
            }
        }

        private void ApplyStageTheme()
        {
            if (environmentRoot == null) return;

            int theme = stageFlow == null ? 0 : Mathf.Abs(stageFlow.World - 1) % 4;
            if (theme == currentTheme) return;
            currentTheme = theme;

            for (int index = environmentRoot.childCount - 1; index >= 0; index--)
                Destroy(environmentRoot.GetChild(index).gameObject);

            Color[] cameraColors =
            {
                new Color(0.10f, 0.22f, 0.09f),
                new Color(0.15f, 0.09f, 0.12f),
                new Color(0.07f, 0.12f, 0.13f),
                new Color(0.06f, 0.035f, 0.15f)
            };

            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera != null) worldCamera.backgroundColor = cameraColors[theme];

            Transform floor = worldRoot.transform.Find("Peanut Field");
            if (floor != null)
            {
                SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>();
                if (floorRenderer != null)
                {
                    Color[] floorColors =
                    {
                        new Color(0.28f, 0.45f, 0.18f),
                        new Color(0.24f, 0.16f, 0.16f),
                        new Color(0.13f, 0.20f, 0.20f),
                        new Color(0.12f, 0.06f, 0.24f)
                    };
                    floorRenderer.color = floorColors[theme];
                }
            }

            Vector3[] positions =
            {
                new Vector3(-7.1f, -3.15f, 0f),
                new Vector3(-5.2f, 2.9f, 0f),
                new Vector3(-2.4f, -3.25f, 0f),
                new Vector3(2.2f, 3.0f, 0f),
                new Vector3(5.0f, -3.15f, 0f),
                new Vector3(7.0f, 2.75f, 0f)
            };

            for (int index = 0; index < positions.Length; index++)
            {
                GameObject decor = new GameObject($"Theme Decor {index + 1}");
                decor.transform.SetParent(environmentRoot, false);
                decor.transform.localPosition = positions[index];
                decor.transform.localScale = Vector3.one * (index % 3 == 0 ? 1.22f : 0.92f);

                SpriteRenderer renderer = decor.AddComponent<SpriteRenderer>();
                renderer.sprite = atlasSprites[20 + theme];
                renderer.color = new Color(1f, 1f, 1f, index < 2 ? 0.70f : 0.92f);
                renderer.sortingOrder = -16;
            }
        }

        private int ResolveBossSpriteIndex()
        {
            int world = stageFlow == null ? 1 : stageFlow.World;
            return 9 + Mathf.Abs(world - 1) % 3;
        }

        private static int ResolveMonsterSpriteIndex(string label, int fallback)
        {
            if (label.Contains("곰팡이")) return 4;
            if (label.Contains("바구미")) return 5;
            if (label.Contains("포식")) return 6;
            if (label.Contains("균사")) return 7;
            if (label.Contains("침공")) return 8;
            return 4 + Mathf.Abs(fallback) % 5;
        }

        private static SpriteRenderer ReadBody(object view)
        {
            if (view == null) return null;
            FieldInfo field = view.GetType().GetField("Body", PublicInstance);
            return field != null ? field.GetValue(view) as SpriteRenderer : null;
        }

        private static TextMesh ReadLabel(object view)
        {
            if (view == null) return null;
            FieldInfo field = view.GetType().GetField("Label", PublicInstance);
            return field != null ? field.GetValue(view) as TextMesh : null;
        }

        private static bool ReadBool(object view, string fieldName)
        {
            if (view == null) return false;
            FieldInfo field = view.GetType().GetField(fieldName, PublicInstance);
            return field != null && Convert.ToBoolean(field.GetValue(view));
        }
    }
}
