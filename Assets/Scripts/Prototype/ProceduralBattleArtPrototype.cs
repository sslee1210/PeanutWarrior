using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Loads the illustrated raster art packed in Resources text assets and binds it
    /// directly to the prototype combat world. The source files are verified before
    /// Texture2D.LoadImage is called so corrupt data cannot silently render shapes.
    /// </summary>
    [DefaultExecutionOrder(25000)]
    public sealed class ProceduralBattleArtPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const string DataRoot = "PeanutWarrior/RuntimeArtData/";
        private const int VisualByteLength = 15458;
        private const int BackgroundByteLength = 15623;
        private const string VisualSha256 = "f677f2503d363f422d1fbb66c874edb897dc354d6586f918349802075798d6b4";
        private const string BackgroundSha256 = "541713e1545440fb3c7fdb3cac6330320f5bb2fe300e89c7f25b54c51fda566a";

        private readonly Sprite[] unitSprites = new Sprite[12];
        private readonly Sprite[] effectSprites = new Sprite[8];
        private readonly Sprite[] backgroundSprites = new Sprite[4];
        private readonly HashSet<int> cleanedRoots = new HashSet<int>();

        private RuntimeWorldViewPrototype worldView;
        private StageFlowController stageFlow;
        private FieldInfo playerViewField;
        private FieldInfo enemyViewsField;
        private FieldInfo worldRootField;
        private GameObject worldRoot;
        private Transform playerRoot;
        private SpriteRenderer stageBackground;
        private Camera worldCamera;
        private Texture2D visualTexture;
        private Texture2D backgroundTexture;
        private int currentTheme = -1;
        private float lastAspect = -1f;
        private bool reflectionReady;
        private bool artReady;

        public bool ArtReady => artReady;
        public Sprite GetUnitSprite(int index) =>
            index >= 0 && index < unitSprites.Length ? unitSprites[index] : null;
        public Sprite GetEffectSprite(int index) =>
            index >= 0 && index < effectSprites.Length ? effectSprites[index] : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<ProceduralBattleArtPrototype>() != null) return;
            GameObject host = new GameObject("Peanut Illustrated Raster Art");
            DontDestroyOnLoad(host);
            host.AddComponent<ProceduralBattleArtPrototype>();
        }

        private void Awake()
        {
            try
            {
                visualTexture = LoadVerifiedTexture(
                    new[] { "visuals_00", "visuals_01" },
                    VisualByteLength,
                    VisualSha256,
                    "illustrated visuals");
                backgroundTexture = LoadVerifiedTexture(
                    new[] { "backgrounds_00", "backgrounds_01" },
                    BackgroundByteLength,
                    BackgroundSha256,
                    "stage backgrounds");

                BuildSprites();
                artReady = true;
                Debug.Log("[PeanutArt] Raster art loaded: 20 visuals, 4 backgrounds.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PeanutArt] Raster art initialization failed: {exception.Message}");
                enabled = false;
            }
        }

        private Texture2D LoadVerifiedTexture(
            IReadOnlyList<string> chunkNames,
            int expectedByteLength,
            string expectedSha256,
            string label)
        {
            System.Text.StringBuilder encoded = new System.Text.StringBuilder();
            for (int index = 0; index < chunkNames.Count; index++)
            {
                string resourcePath = DataRoot + chunkNames[index];
                TextAsset chunk = Resources.Load<TextAsset>(resourcePath);
                if (chunk == null)
                    throw new InvalidOperationException($"Missing {label} resource: {resourcePath}");
                encoded.Append(chunk.text.Trim());
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(encoded.ToString());
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException($"Invalid Base64 in {label}: {exception.Message}");
            }

            if (bytes.Length != expectedByteLength)
                throw new InvalidOperationException(
                    $"Unexpected {label} byte length. Expected {expectedByteLength}, got {bytes.Length}.");

            string actualHash;
            using (SHA256 sha = SHA256.Create())
            {
                actualHash = BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
            if (!string.Equals(actualHash, expectedSha256, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"SHA-256 mismatch for {label}. Expected {expectedSha256}, got {actualHash}.");

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Peanut " + label,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!texture.LoadImage(bytes, false))
                throw new InvalidOperationException($"Texture2D.LoadImage failed for {label}.");
            return texture;
        }

        private void BuildSprites()
        {
            if (visualTexture.width != 384 || visualTexture.height != 192)
                throw new InvalidOperationException(
                    $"Unexpected visual sheet size: {visualTexture.width}x{visualTexture.height}.");
            if (backgroundTexture.width != 384 || backgroundTexture.height != 216)
                throw new InvalidOperationException(
                    $"Unexpected background sheet size: {backgroundTexture.width}x{backgroundTexture.height}.");

            for (int index = 0; index < unitSprites.Length; index++)
            {
                int row = index / 6;
                int column = index % 6;
                float y = row == 0 ? 128f : 64f;
                unitSprites[index] = CreateSprite(
                    visualTexture,
                    new Rect(column * 64f, y, 64f, 64f),
                    64f,
                    $"Peanut Unit {index:00}");
            }

            for (int index = 0; index < effectSprites.Length; index++)
            {
                effectSprites[index] = CreateSprite(
                    visualTexture,
                    new Rect(index * 48f, 0f, 48f, 64f),
                    64f,
                    $"Peanut Effect {index:00}");
            }

            Rect[] backgroundRects =
            {
                new Rect(0f, 108f, 192f, 108f),
                new Rect(192f, 108f, 192f, 108f),
                new Rect(0f, 0f, 192f, 108f),
                new Rect(192f, 0f, 192f, 108f)
            };
            for (int index = 0; index < backgroundSprites.Length; index++)
            {
                backgroundSprites[index] = CreateSprite(
                    backgroundTexture,
                    backgroundRects[index],
                    10f,
                    $"Peanut Stage Background {index:00}");
            }
        }

        private static Sprite CreateSprite(Texture2D texture, Rect rect, float pixelsPerUnit, string name)
        {
            Sprite sprite = Sprite.Create(
                texture,
                rect,
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        private void LateUpdate()
        {
            if (!artReady || !ResolveWorld()) return;
            ApplyPlayer();
            ApplyEnemies();
            ApplyStageBackground();
        }

        private bool ResolveWorld()
        {
            if (worldView == null)
            {
                worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                reflectionReady = false;
                worldRoot = null;
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

            GameObject resolvedRoot = worldRootField.GetValue(worldView) as GameObject;
            if (resolvedRoot == null) return false;
            if (resolvedRoot != worldRoot)
            {
                worldRoot = resolvedRoot;
                stageBackground = null;
                playerRoot = null;
                cleanedRoots.Clear();
                currentTheme = -1;
                lastAspect = -1f;
                PrepareWorldBackground();
            }
            return true;
        }

        private void PrepareWorldBackground()
        {
            HideLegacyBackground(worldRoot.transform);
            Transform existing = worldRoot.transform.Find("Illustrated Stage Background");
            if (existing == null)
            {
                GameObject backgroundObject = new GameObject("Illustrated Stage Background");
                backgroundObject.transform.SetParent(worldRoot.transform, false);
                stageBackground = backgroundObject.AddComponent<SpriteRenderer>();
            }
            else
            {
                stageBackground = existing.GetComponent<SpriteRenderer>();
                if (stageBackground == null) stageBackground = existing.gameObject.AddComponent<SpriteRenderer>();
            }
            stageBackground.sortingOrder = -100;
            stageBackground.color = Color.white;
            worldCamera = Camera.main;
        }

        private static void HideLegacyBackground(Transform root)
        {
            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                if (child.name == "Peanut Field" || child.name == "Field Patch")
                {
                    SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                    if (renderer != null) renderer.enabled = false;
                }
            }
        }

        private void ApplyPlayer()
        {
            SpriteRenderer body = ReadBody(playerViewField.GetValue(worldView));
            if (body == null) return;
            playerRoot = body.transform.parent;
            ApplySprite(body, unitSprites[0], 1.48f, 8);
            CleanHud(playerRoot, false);
        }

        private void ApplyEnemies()
        {
            IDictionary views = enemyViewsField.GetValue(worldView) as IDictionary;
            if (views == null) return;

            int fallback = 0;
            foreach (DictionaryEntry entry in views)
            {
                SpriteRenderer body = ReadBody(entry.Value);
                if (body == null)
                {
                    fallback++;
                    continue;
                }

                bool boss = ReadBool(entry.Value, "IsBoss");
                TextMesh label = ReadLabel(entry.Value);
                string enemyName = label != null ? label.text : string.Empty;
                int spriteIndex = boss ? ResolveBossIndex() : ResolveMonsterIndex(enemyName, fallback);
                ApplySprite(body, unitSprites[spriteIndex], boss ? 2.15f : 1.35f, boss ? 9 : 7);
                CleanHud(body.transform.parent, boss);
                fallback++;
            }
        }

        private void ApplySprite(SpriteRenderer body, Sprite sprite, float scale, int sortingOrder)
        {
            if (body == null || sprite == null) return;
            Transform root = body.transform.parent;
            if (root != null && cleanedRoots.Add(root.GetInstanceID()))
            {
                string[] obsoleteNames = { "Procedural Visual", "Illustrated Visual", "Illustrated Aura" };
                for (int index = 0; index < obsoleteNames.Length; index++)
                {
                    Transform obsolete = root.Find(obsoleteNames[index]);
                    if (obsolete != null) Destroy(obsolete.gameObject);
                }
            }

            body.gameObject.SetActive(true);
            body.enabled = true;
            body.sprite = sprite;
            body.color = Color.white;
            body.sortingOrder = sortingOrder;
            body.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            body.transform.localScale = Vector3.one * scale;
            Transform highlight = body.transform.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(false);
        }

        private static void CleanHud(Transform root, bool boss)
        {
            if (root == null) return;
            Transform label = root.Find("Label");
            if (label != null)
            {
                label.gameObject.SetActive(boss);
                label.localPosition = new Vector3(0f, 1.72f, 0f);
            }
            Transform health = root.Find("Health Back");
            if (health != null)
            {
                health.gameObject.SetActive(boss);
                health.localPosition = new Vector3(0f, 1.42f, 0f);
            }
            Transform shadow = root.Find("Shadow");
            if (shadow != null)
            {
                shadow.localPosition = new Vector3(0f, -0.56f, 0f);
                shadow.localScale = boss ? new Vector3(2f, 1.25f, 1f) : new Vector3(1.2f, 0.8f, 1f);
            }
        }

        private void ApplyStageBackground()
        {
            if (stageBackground == null) return;
            int theme = stageFlow == null ? 0 : Mathf.Abs(stageFlow.World - 1) % 4;
            if (theme != currentTheme)
            {
                currentTheme = theme;
                stageBackground.sprite = backgroundSprites[theme];
            }

            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null || stageBackground.sprite == null) return;
            float aspect = Mathf.Max(0.1f, worldCamera.aspect);
            if (Mathf.Abs(aspect - lastAspect) < 0.001f) return;
            lastAspect = aspect;
            float cameraHeight = worldCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * aspect;
            Vector2 spriteSize = stageBackground.sprite.bounds.size;
            float coverScale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y) * 1.03f;
            stageBackground.transform.localPosition = Vector3.zero;
            stageBackground.transform.localScale = new Vector3(coverScale, coverScale, 1f);
        }

        private int ResolveBossIndex()
        {
            int world = stageFlow == null ? 1 : stageFlow.World;
            return 9 + Mathf.Abs(world - 1) % 3;
        }

        private static int ResolveMonsterIndex(string label, int fallback)
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
