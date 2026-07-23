using System;
using System.Collections.Generic;
using System.Text;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Replaces the temporary circle presentation with an illustrated sprite atlas.
    /// Combat data, damage, cooldowns and stage flow remain authoritative elsewhere.
    /// </summary>
    [DefaultExecutionOrder(17750)]
    public sealed class ProceduralBattleArtPrototype : MonoBehaviour
    {
        private const int AtlasColumns = 6;
        private const int AtlasRows = 4;
        private const float WorldTileSize = 1.45f;

        private enum UnitKind
        {
            Hero = 0,
            CompanionBlade = 1,
            CompanionGuard = 2,
            CompanionMage = 3,
            Mold = 4,
            Weevil = 5,
            Predator = 6,
            Mycelium = 7,
            Invader = 8,
            BossMold = 9,
            BossBeetle = 10,
            BossPortal = 11
        }

        private sealed class IllustratedUnit
        {
            public Transform Root;
            public Transform Visual;
            public SpriteRenderer Renderer;
            public SpriteRenderer Aura;
            public UnitKind Kind;
            public float Seed;
            public Vector3 BaseScale;
        }

        private readonly Dictionary<Transform, IllustratedUnit> units =
            new Dictionary<Transform, IllustratedUnit>();
        private readonly List<Transform> staleUnits = new List<Transform>();
        private readonly List<IllustratedUnit> companions = new List<IllustratedUnit>();
        private readonly Sprite[] atlasSprites = new Sprite[24];

        private StageFlowController stageFlow;
        private Transform worldRoot;
        private Transform environmentRoot;
        private Transform heroRoot;
        private Camera worldCamera;
        private Texture2D atlasTexture;
        private float scanTimer;
        private float ambientTimer;
        private int currentTheme = -1;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<ProceduralBattleArtPrototype>() != null) return;
            GameObject root = new GameObject("Peanut Illustrated Battle Art");
            DontDestroyOnLoad(root);
            root.AddComponent<ProceduralBattleArtPrototype>();
        }

        private void Start()
        {
            stageFlow = FindFirstObjectByType<StageFlowController>();
            StringBuilder atlasBase64 = new StringBuilder(90112);
            for (int index = 0; index < 8; index++)
            {
                TextAsset chunk = Resources.Load<TextAsset>(
                    $"PeanutWarrior/AtlasChunks/peanut_battle_atlas_{index:00}");
                if (chunk == null)
                {
                    Debug.LogError($"Peanut battle atlas chunk {index:00} was not found.");
                    enabled = false;
                    return;
                }
                atlasBase64.Append(chunk.text.Trim());
            }

            try
            {
                byte[] imageBytes = Convert.FromBase64String(atlasBase64.ToString());
                atlasTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!atlasTexture.LoadImage(imageBytes, false))
                    throw new InvalidOperationException("Texture2D.LoadImage returned false.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to decode the Peanut battle atlas: {exception.Message}");
                enabled = false;
                return;
            }

            atlasTexture.name = "Peanut Illustrated Battle Atlas";
            atlasTexture.filterMode = FilterMode.Bilinear;
            atlasTexture.wrapMode = TextureWrapMode.Clamp;
            CreateAtlasSprites();
            initialized = true;

            if (stageFlow != null)
                stageFlow.StateChanged += HandleStageChanged;
        }

        private void OnDestroy()
        {
            if (stageFlow != null)
                stageFlow.StateChanged -= HandleStageChanged;
        }

        private void Update()
        {
            if (!initialized) return;

            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.25f;
                ResolveWorld();
                ScanBattlefield();
                EnsureCompanions();
                RefreshTheme();
            }

            AnimateUnits();
            AnimateCompanions();
            AnimateEnvironment();
        }

        private void ResolveWorld()
        {
            if (worldRoot != null) return;

            GameObject found = GameObject.Find("Runtime 2D World");
            if (found == null) return;

            worldRoot = found.transform;
            worldCamera = Camera.main;
            environmentRoot = new GameObject("Illustrated Stage Environment").transform;
            environmentRoot.SetParent(worldRoot, false);
        }

        private void CreateAtlasSprites()
        {
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
                atlasSprites[index].name = $"PeanutBattleArt_{index:00}";
            }
        }

        private void ScanBattlefield()
        {
            if (worldRoot == null) return;

            staleUnits.Clear();
            foreach (Transform key in units.Keys)
            {
                if (key == null) staleUnits.Add(key);
            }
            for (int i = 0; i < staleUnits.Count; i++)
                units.Remove(staleUnits[i]);

            Transform[] all = worldRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform candidate = all[i];
                if (candidate == null || units.ContainsKey(candidate)) continue;

                if (candidate.name == "Peanut Warrior")
                {
                    heroRoot = candidate;
                    Decorate(candidate, UnitKind.Hero);
                    continue;
                }

                if (candidate.name == "Monster View")
                {
                    Decorate(candidate, ResolveMonsterKind(ReadLabel(candidate)));
                    continue;
                }

                if (candidate.name == "Boss View")
                    Decorate(candidate, ResolveBossKind());
            }
        }

        private void Decorate(Transform root, UnitKind kind)
        {
            Transform oldBody = root.Find("Body");
            if (oldBody != null)
                oldBody.gameObject.SetActive(false);

            GameObject visualObject = new GameObject("Illustrated Visual");
            visualObject.transform.SetParent(root, false);
            visualObject.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = atlasSprites[(int)kind];
            renderer.sortingOrder = IsBoss(kind) ? 6 : 5;

            Vector3 scale = ScaleFor(kind);
            visualObject.transform.localScale = scale;

            SpriteRenderer aura = null;
            if (kind == UnitKind.Hero || IsBoss(kind))
            {
                GameObject auraObject = new GameObject("Illustrated Aura");
                auraObject.transform.SetParent(root, false);
                auraObject.transform.localPosition = Vector3.zero;
                aura = auraObject.AddComponent<SpriteRenderer>();
                aura.sprite = atlasSprites[kind == UnitKind.Hero ? 12 : ThemeSkillIndex()];
                aura.color = kind == UnitKind.Hero
                    ? new Color(1f, 0.86f, 0.32f, 0.28f)
                    : new Color(1f, 1f, 1f, 0.18f);
                aura.sortingOrder = 1;
                auraObject.transform.localScale = kind == UnitKind.Hero
                    ? Vector3.one * 1.25f
                    : Vector3.one * 1.55f;
            }

            units[root] = new IllustratedUnit
            {
                Root = root,
                Visual = visualObject.transform,
                Renderer = renderer,
                Aura = aura,
                Kind = kind,
                Seed = UnityEngine.Random.Range(0f, 10f),
                BaseScale = scale
            };
        }

        private void EnsureCompanions()
        {
            if (worldRoot == null || heroRoot == null || companions.Count > 0) return;

            CreateCompanion(UnitKind.CompanionBlade);
            CreateCompanion(UnitKind.CompanionGuard);
            CreateCompanion(UnitKind.CompanionMage);
        }

        private void CreateCompanion(UnitKind kind)
        {
            GameObject root = new GameObject($"Support Peanut {kind}");
            root.transform.SetParent(worldRoot, false);

            GameObject visual = new GameObject("Illustrated Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = atlasSprites[(int)kind];
            renderer.sortingOrder = 5;

            Vector3 scale = Vector3.one * 0.70f;
            visual.transform.localScale = scale;

            companions.Add(new IllustratedUnit
            {
                Root = root.transform,
                Visual = visual.transform,
                Renderer = renderer,
                Kind = kind,
                Seed = UnityEngine.Random.Range(0f, 10f),
                BaseScale = scale
            });
        }

        private void AnimateUnits()
        {
            foreach (IllustratedUnit unit in units.Values)
            {
                if (unit == null || unit.Root == null || unit.Visual == null) continue;

                float speed = IsBoss(unit.Kind) ? 2.2f : 4.2f;
                float bob = Mathf.Sin(Time.time * speed + unit.Seed) *
                    (IsBoss(unit.Kind) ? 0.035f : 0.025f);
                float breathe = 1f + Mathf.Sin(Time.time * (speed * 0.72f) + unit.Seed) * 0.018f;

                unit.Visual.localPosition = new Vector3(0f, 0.08f + bob, 0f);
                unit.Visual.localScale = unit.BaseScale * breathe;
                unit.Visual.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Sin(Time.time * speed * 0.45f + unit.Seed) * 1.2f);

                Transform oldBody = unit.Root.Find("Body");
                SpriteRenderer oldRenderer = oldBody != null
                    ? oldBody.GetComponent<SpriteRenderer>()
                    : null;
                if (oldRenderer != null)
                    unit.Renderer.flipX = oldRenderer.flipX;

                if (unit.Aura != null)
                {
                    unit.Aura.transform.Rotate(0f, 0f,
                        (unit.Kind == UnitKind.Hero ? -22f : 14f) * Time.deltaTime);
                    float pulse = 1f + Mathf.Sin(Time.time * 3.4f + unit.Seed) * 0.08f;
                    unit.Aura.transform.localScale =
                        Vector3.one * (unit.Kind == UnitKind.Hero ? 1.25f : 1.55f) * pulse;
                }
            }
        }

        private void AnimateCompanions()
        {
            if (heroRoot == null || companions.Count != 3) return;

            Vector3[] offsets =
            {
                new Vector3(-1.15f, -0.58f, 0f),
                new Vector3(0f, -1.00f, 0f),
                new Vector3(1.15f, -0.58f, 0f)
            };

            for (int i = 0; i < companions.Count; i++)
            {
                IllustratedUnit companion = companions[i];
                if (companion.Root == null) continue;

                float orbit = Time.time * 0.42f + (i * 2.094f);
                Vector3 target = heroRoot.position + offsets[i] +
                    new Vector3(Mathf.Cos(orbit) * 0.10f, Mathf.Sin(orbit * 1.7f) * 0.08f, 0f);

                companion.Root.position = Vector3.Lerp(
                    companion.Root.position,
                    target,
                    1f - Mathf.Exp(-7f * Time.deltaTime));

                companion.Visual.localScale = companion.BaseScale *
                    (1f + Mathf.Sin(Time.time * 4.5f + companion.Seed) * 0.025f);
                companion.Visual.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Sin(Time.time * 2.8f + companion.Seed) * 2f);
            }
        }

        private void HandleStageChanged()
        {
            currentTheme = -1;
        }

        private void RefreshTheme()
        {
            if (worldRoot == null || environmentRoot == null) return;

            int theme = stageFlow == null ? 0 : Mathf.Abs(stageFlow.World - 1) % 4;
            if (theme == currentTheme) return;
            currentTheme = theme;

            for (int i = environmentRoot.childCount - 1; i >= 0; i--)
                Destroy(environmentRoot.GetChild(i).gameObject);

            ApplyThemePalette(theme);

            Vector3[] positions =
            {
                new Vector3(-7.1f, -3.25f, 0f),
                new Vector3(-4.65f, 2.95f, 0f),
                new Vector3(-1.9f, -3.35f, 0f),
                new Vector3(1.2f, 3.05f, 0f),
                new Vector3(4.1f, -3.25f, 0f),
                new Vector3(7.0f, 2.75f, 0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject decor = new GameObject($"Theme Decor {i + 1}");
                decor.transform.SetParent(environmentRoot, false);
                decor.transform.localPosition = positions[i];
                decor.transform.localRotation =
                    Quaternion.Euler(0f, 0f, i % 2 == 0 ? -4f : 4f);
                decor.transform.localScale =
                    Vector3.one * (i % 3 == 0 ? 1.25f : 0.92f);

                SpriteRenderer renderer = decor.AddComponent<SpriteRenderer>();
                renderer.sprite = atlasSprites[20 + theme];
                renderer.color = new Color(1f, 1f, 1f, i < 2 ? 0.72f : 0.92f);
                renderer.sortingOrder = -16 + (i % 2);
            }

            GameObject centerpiece = new GameObject("Theme Centerpiece");
            centerpiece.transform.SetParent(environmentRoot, false);
            centerpiece.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            centerpiece.transform.localScale = Vector3.one * 2.6f;
            SpriteRenderer centerRenderer = centerpiece.AddComponent<SpriteRenderer>();
            centerRenderer.sprite = atlasSprites[20 + theme];
            centerRenderer.color = new Color(1f, 1f, 1f, 0.13f);
            centerRenderer.sortingOrder = -18;
        }

        private void ApplyThemePalette(int theme)
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera != null)
            {
                Color[] cameraColors =
                {
                    new Color(0.12f, 0.24f, 0.10f),
                    new Color(0.16f, 0.12f, 0.14f),
                    new Color(0.08f, 0.13f, 0.14f),
                    new Color(0.07f, 0.05f, 0.16f)
                };
                worldCamera.backgroundColor = cameraColors[theme];
            }

            GameObject floor = GameObject.Find("Peanut Field");
            if (floor != null)
            {
                SpriteRenderer renderer = floor.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    Color[] floorColors =
                    {
                        new Color(0.27f, 0.43f, 0.17f),
                        new Color(0.25f, 0.20f, 0.18f),
                        new Color(0.16f, 0.23f, 0.22f),
                        new Color(0.13f, 0.08f, 0.25f)
                    };
                    renderer.color = floorColors[theme];
                }
            }

            GameObject[] patches = GameObject.FindGameObjectsWithTag("Untagged");
            for (int i = 0; i < patches.Length; i++)
            {
                if (patches[i].name != "Field Patch") continue;
                SpriteRenderer renderer = patches[i].GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                renderer.color = theme == 0
                    ? new Color(0.53f, 0.68f, 0.26f, 0.40f)
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        private void AnimateEnvironment()
        {
            if (environmentRoot == null) return;

            ambientTimer += Time.deltaTime;
            for (int i = 0; i < environmentRoot.childCount; i++)
            {
                Transform child = environmentRoot.GetChild(i);
                if (child == null) continue;
                float sway = Mathf.Sin(ambientTimer * 0.7f + i * 1.3f) * 1.5f;
                child.localRotation = Quaternion.Euler(0f, 0f, sway);
            }
        }

        private UnitKind ResolveMonsterKind(string label)
        {
            if (label.Contains("곰팡이")) return UnitKind.Mold;
            if (label.Contains("바구미")) return UnitKind.Weevil;
            if (label.Contains("포식")) return UnitKind.Predator;
            if (label.Contains("균사")) return UnitKind.Mycelium;
            return UnitKind.Invader;
        }

        private UnitKind ResolveBossKind()
        {
            int theme = stageFlow == null ? 0 : Mathf.Abs(stageFlow.World - 1) % 4;
            if (theme == 0 || theme == 1) return UnitKind.BossMold;
            if (theme == 2) return UnitKind.BossBeetle;
            return UnitKind.BossPortal;
        }

        private int ThemeSkillIndex()
        {
            int theme = stageFlow == null ? 0 : Mathf.Abs(stageFlow.World - 1) % 4;
            return 16 + theme;
        }

        private static string ReadLabel(Transform root)
        {
            Transform labelTransform = root.Find("Label");
            TextMesh label = labelTransform != null
                ? labelTransform.GetComponent<TextMesh>()
                : null;
            return label != null ? label.text : string.Empty;
        }

        private static bool IsBoss(UnitKind kind)
        {
            return kind == UnitKind.BossMold ||
                   kind == UnitKind.BossBeetle ||
                   kind == UnitKind.BossPortal;
        }

        private static Vector3 ScaleFor(UnitKind kind)
        {
            switch (kind)
            {
                case UnitKind.Hero:
                    return Vector3.one * 1.22f;
                case UnitKind.BossMold:
                case UnitKind.BossBeetle:
                case UnitKind.BossPortal:
                    return Vector3.one * 1.58f;
                default:
                    return Vector3.one * 0.92f;
            }
        }
    }
}
