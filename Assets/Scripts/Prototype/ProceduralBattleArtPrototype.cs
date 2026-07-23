using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Generates the illustrated battle sprites in memory and binds them directly to
    /// RuntimeWorldViewPrototype. No external atlas decoding is used, so damaged
    /// resource files cannot disable the battle art.
    /// </summary>
    [DefaultExecutionOrder(25000)]
    public sealed class ProceduralBattleArtPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const int ArtCount = 24;

        private readonly Sprite[] art = new Sprite[ArtCount];
        private readonly HashSet<int> cleanedRoots = new HashSet<int>();
        private readonly List<Transform> companions = new List<Transform>();

        private RuntimeWorldViewPrototype worldView;
        private StageFlowController stageFlow;
        private FieldInfo playerViewField;
        private FieldInfo enemyViewsField;
        private FieldInfo worldRootField;
        private GameObject worldRoot;
        private Transform environmentRoot;
        private Transform playerRoot;
        private Camera worldCamera;
        private int currentTheme = -1;
        private bool reflectionReady;
        private bool artReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<ProceduralBattleArtPrototype>() != null) return;
            GameObject root = new GameObject("Peanut Runtime Illustrated Battle Art");
            DontDestroyOnLoad(root);
            root.AddComponent<ProceduralBattleArtPrototype>();
        }

        private void Awake()
        {
            for (int index = 0; index < art.Length; index++)
            {
                try
                {
                    art[index] = RuntimeArt.Create(index);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[PeanutArt] Sprite {index:00} used fallback: {exception.Message}");
                    art[index] = RuntimeArt.Fallback(index);
                }
            }

            artReady = true;
            Debug.Log("[PeanutArt] Runtime illustrated sprites generated: 24 ready. External atlas disabled.");
        }

        private void LateUpdate()
        {
            if (!artReady || !ResolveWorld()) return;
            ApplyPlayer();
            ApplyEnemies();
            EnsureCompanions();
            AnimateCompanions();
            ApplyTheme();
        }

        private bool ResolveWorld()
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
                    return false;
                }
                reflectionReady = true;
            }

            worldRoot = worldRootField.GetValue(worldView) as GameObject;
            if (worldRoot == null) return false;

            if (environmentRoot == null)
            {
                RemoveEnvironment("Illustrated Stage Environment");
                RemoveEnvironment("Direct Illustrated Stage Environment");
                RemoveEnvironment("Generated Illustrated Stage Environment");
                environmentRoot = new GameObject("Runtime Illustrated Stage Environment").transform;
                environmentRoot.SetParent(worldRoot.transform, false);
                worldCamera = Camera.main;
                currentTheme = -1;
            }
            return true;
        }

        private void RemoveEnvironment(string objectName)
        {
            Transform existing = worldRoot.transform.Find(objectName);
            if (existing != null) Destroy(existing.gameObject);
        }

        private void ApplyPlayer()
        {
            SpriteRenderer body = ReadBody(playerViewField.GetValue(worldView));
            if (body == null) return;
            playerRoot = body.transform.parent;
            ApplySprite(body, art[0], 1.28f, false);
            MoveHud(playerRoot, 1.12f, 0.83f);
        }

        private void ApplyEnemies()
        {
            IDictionary views = enemyViewsField.GetValue(worldView) as IDictionary;
            if (views == null) return;
            int fallback = 0;
            foreach (DictionaryEntry pair in views)
            {
                SpriteRenderer body = ReadBody(pair.Value);
                if (body == null)
                {
                    fallback++;
                    continue;
                }

                bool boss = ReadBool(pair.Value, "IsBoss");
                TextMesh label = ReadLabel(pair.Value);
                string text = label != null ? label.text : string.Empty;
                int spriteIndex = boss ? ResolveBoss() : ResolveMonster(text, fallback);
                ApplySprite(body, art[spriteIndex], boss ? 1.45f : 1.08f, boss);
                MoveHud(body.transform.parent, boss ? 1.48f : 1.00f, boss ? 1.16f : 0.75f);
                fallback++;
            }
        }

        private void ApplySprite(SpriteRenderer body, Sprite sprite, float scale, bool boss)
        {
            if (body == null || sprite == null) return;
            Transform root = body.transform.parent;
            if (root != null && cleanedRoots.Add(root.GetInstanceID()))
            {
                string[] oldNames = { "Procedural Visual", "Illustrated Visual", "Illustrated Aura" };
                for (int index = 0; index < oldNames.Length; index++)
                {
                    Transform old = root.Find(oldNames[index]);
                    if (old != null) Destroy(old.gameObject);
                }
            }

            body.gameObject.SetActive(true);
            body.sprite = sprite;
            body.color = Color.white;
            body.sortingOrder = boss ? 7 : 5;
            body.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            body.transform.localScale = Vector3.one * scale;
            Transform highlight = body.transform.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(false);
        }

        private static void MoveHud(Transform root, float labelY, float healthY)
        {
            if (root == null) return;
            Transform label = root.Find("Label");
            if (label != null) label.localPosition = new Vector3(0f, labelY, 0f);
            Transform health = root.Find("Health Back");
            if (health != null) health.localPosition = new Vector3(0f, healthY, 0f);
            Transform shadow = root.Find("Shadow");
            if (shadow != null) shadow.localPosition = new Vector3(0f, -0.58f, 0f);
        }

        private void EnsureCompanions()
        {
            if (worldRoot == null || playerRoot == null || companions.Count == 3) return;
            for (int index = companions.Count; index < 3; index++)
            {
                GameObject companion = new GameObject($"Illustrated Support Peanut {index + 1}");
                companion.transform.SetParent(worldRoot.transform, false);
                SpriteRenderer renderer = companion.AddComponent<SpriteRenderer>();
                renderer.sprite = art[index + 1];
                renderer.color = Color.white;
                renderer.sortingOrder = 5;
                companion.transform.localScale = Vector3.one * 0.78f;
                companions.Add(companion.transform);
            }
        }

        private void AnimateCompanions()
        {
            if (playerRoot == null || companions.Count != 3) return;
            Vector3[] offsets =
            {
                new Vector3(-1.28f, -0.66f, 0f),
                new Vector3(0f, -1.12f, 0f),
                new Vector3(1.28f, -0.66f, 0f)
            };

            for (int index = 0; index < companions.Count; index++)
            {
                Transform companion = companions[index];
                if (companion == null) continue;
                float phase = Time.time * 0.48f + index * 2.094f;
                Vector3 target = playerRoot.position + offsets[index] +
                    new Vector3(Mathf.Cos(phase) * 0.09f, Mathf.Sin(phase * 1.6f) * 0.08f, 0f);
                companion.position = Vector3.Lerp(companion.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
                companion.localScale = Vector3.one * 0.78f *
                    (1f + Mathf.Sin(Time.time * 4.2f + index) * 0.028f);
                companion.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * 2.5f);
            }
        }

        private void ApplyTheme()
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

            Vector3[] positions =
            {
                new Vector3(-7.1f, -3.15f),
                new Vector3(-5.2f, 2.9f),
                new Vector3(-2.4f, -3.25f),
                new Vector3(2.2f, 3.0f),
                new Vector3(5.0f, -3.15f),
                new Vector3(7.0f, 2.75f)
            };
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject decor = new GameObject($"Theme Decor {index + 1}");
                decor.transform.SetParent(environmentRoot, false);
                decor.transform.localPosition = positions[index];
                decor.transform.localScale = Vector3.one * (index % 3 == 0 ? 1.22f : 0.92f);
                SpriteRenderer renderer = decor.AddComponent<SpriteRenderer>();
                renderer.sprite = art[20 + theme];
                renderer.color = new Color(1f, 1f, 1f, index < 2 ? 0.70f : 0.92f);
                renderer.sortingOrder = -16;
            }
        }

        private int ResolveBoss()
        {
            int world = stageFlow == null ? 1 : stageFlow.World;
            return 9 + Mathf.Abs(world - 1) % 3;
        }

        private static int ResolveMonster(string label, int fallback)
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

        private static class RuntimeArt
        {
            private const int Size = 96;
            private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
            private static readonly Color32 Outline = new Color32(35, 23, 23, 255);
            private static readonly Color32 Shell = new Color32(177, 105, 40, 255);
            private static readonly Color32 ShellLight = new Color32(235, 164, 66, 255);
            private static readonly Color32 White = new Color32(250, 247, 229, 255);

            public static Sprite Create(int index)
            {
                Canvas canvas = new Canvas(Size, Size);
                if (index <= 3) DrawPeanut(canvas, index);
                else if (index <= 8) DrawMonster(canvas, index - 4);
                else if (index <= 11) DrawBoss(canvas, index - 9);
                else if (index <= 19) DrawEffect(canvas, index - 12);
                else DrawDecor(canvas, index - 20);
                return canvas.ToSprite($"PeanutRuntimeArt_{index:00}");
            }

            public static Sprite Fallback(int index)
            {
                Canvas canvas = new Canvas(Size, Size);
                canvas.Ellipse(48, 48, 34, 34, Outline);
                canvas.Ellipse(48, 48, 29, 29, Palette(index));
                return canvas.ToSprite($"PeanutFallback_{index:00}");
            }

            private static void DrawPeanut(Canvas c, int kind)
            {
                Color32 accent = kind == 0 ? new Color32(42, 112, 218, 255) :
                    kind == 1 ? new Color32(238, 137, 35, 255) :
                    kind == 2 ? new Color32(42, 160, 104, 255) :
                    new Color32(126, 62, 194, 255);

                c.Ellipse(48, 49, 29, 38, Outline);
                c.Ellipse(48, 49, 24, 33, Shell);
                c.Line(35, 23, 38, 75, ShellLight, 3);
                c.Line(61, 23, 58, 75, ShellLight, 3);
                c.Rect(27, 24, 69, 42, Outline);
                c.Rect(30, 27, 66, 39, accent);
                c.Triangle(48, 9, 41, 25, 55, 25, accent);
                c.Ellipse(40, 49, 3, 4, White);
                c.Ellipse(56, 49, 3, 4, White);
                c.Line(41, 62, 48, 66, Outline, 2);
                c.Line(48, 66, 55, 62, Outline, 2);

                if (kind == 0 || kind == 1)
                {
                    c.Line(66, 64, 82, 22, White, 5);
                    c.Line(66, 64, 82, 22, new Color32(91, 151, 220, 255), 2);
                    c.Line(60, 61, 73, 67, Outline, 5);
                }
                else if (kind == 2)
                {
                    c.Ellipse(27, 58, 16, 20, Outline);
                    c.Ellipse(27, 58, 12, 16, accent);
                    c.Line(27, 43, 27, 73, White, 2);
                }
                else
                {
                    c.Line(71, 67, 78, 24, Outline, 5);
                    c.Ellipse(80, 20, 8, 8, new Color32(219, 102, 255, 255));
                    c.Triangle(48, 7, 25, 29, 71, 29, accent);
                }
            }

            private static void DrawMonster(Canvas c, int kind)
            {
                if (kind == 0)
                {
                    c.Ellipse(48, 59, 29, 25, Outline);
                    c.Ellipse(48, 59, 24, 20, new Color32(88, 205, 102, 255));
                    c.Ellipse(34, 30, 13, 13, Outline);
                    c.Ellipse(34, 30, 9, 9, new Color32(104, 224, 120, 255));
                    c.Ellipse(50, 24, 15, 15, Outline);
                    c.Ellipse(50, 24, 11, 11, new Color32(104, 224, 120, 255));
                    c.Ellipse(66, 33, 12, 12, Outline);
                    c.Ellipse(66, 33, 8, 8, new Color32(104, 224, 120, 255));
                }
                else if (kind == 1)
                {
                    c.Ellipse(48, 50, 27, 35, Outline);
                    c.Ellipse(48, 50, 21, 29, new Color32(145, 86, 45, 255));
                    c.Line(48, 20, 48, 78, ShellLight, 3);
                    c.Line(25, 48, 71, 48, Outline, 3);
                    c.Line(33, 22, 17, 7, Outline, 3);
                    c.Line(63, 22, 79, 7, Outline, 3);
                }
                else if (kind == 2)
                {
                    c.Triangle(48, 12, 15, 74, 81, 74, Outline);
                    c.Triangle(48, 20, 22, 69, 74, 69, new Color32(222, 66, 70, 255));
                    c.Ellipse(38, 48, 4, 5, White);
                    c.Ellipse(58, 48, 4, 5, White);
                    c.Triangle(48, 58, 38, 72, 58, 72, Outline);
                }
                else if (kind == 3)
                {
                    c.Ellipse(48, 52, 27, 30, Outline);
                    c.Ellipse(48, 52, 21, 24, new Color32(104, 66, 166, 255));
                    c.Ellipse(48, 25, 31, 14, Outline);
                    c.Ellipse(48, 23, 26, 10, new Color32(167, 101, 225, 255));
                    c.Ellipse(32, 20, 5, 5, White);
                    c.Ellipse(48, 14, 5, 5, White);
                    c.Ellipse(64, 20, 5, 5, White);
                }
                else
                {
                    c.Triangle(48, 12, 17, 75, 79, 75, Outline);
                    c.Triangle(48, 21, 24, 68, 72, 68, new Color32(50, 186, 205, 255));
                    c.Ellipse(38, 47, 5, 6, new Color32(255, 242, 99, 255));
                    c.Ellipse(58, 47, 5, 6, new Color32(255, 242, 99, 255));
                }
                c.Ellipse(40, 56, 3, 4, Outline);
                c.Ellipse(56, 56, 3, 4, Outline);
            }

            private static void DrawBoss(Canvas c, int kind)
            {
                Color32 accent = kind == 0 ? new Color32(225, 67, 165, 255) :
                    kind == 1 ? new Color32(72, 146, 232, 255) :
                    new Color32(163, 67, 240, 255);
                c.Ellipse(48, 49, 39, 40, accent);
                c.Ellipse(48, 49, 27, 34, Outline);
                c.Ellipse(48, 49, 22, 29, Shell);
                c.Rect(29, 24, 67, 41, accent);
                c.Ellipse(39, 49, 4, 5, White);
                c.Ellipse(57, 49, 4, 5, White);
                c.Triangle(48, 7, 37, 27, 43, 25, ShellLight);
                c.Triangle(48, 7, 53, 25, 59, 27, ShellLight);
                if (kind == 1)
                {
                    c.Triangle(15, 18, 8, 63, 34, 43, accent);
                    c.Triangle(81, 18, 62, 43, 88, 63, accent);
                    c.Line(48, 20, 48, 78, White, 3);
                }
                if (kind == 2)
                {
                    c.Ring(48, 49, 44, 36, new Color32(91, 223, 255, 255), 5);
                    c.Ring(48, 49, 38, 31, new Color32(212, 91, 255, 255), 4);
                }
            }

            private static void DrawEffect(Canvas c, int kind)
            {
                Color32[] colors =
                {
                    new Color32(255, 190, 35, 255),
                    new Color32(255, 76, 48, 255),
                    new Color32(65, 207, 91, 255),
                    new Color32(63, 151, 240, 255),
                    new Color32(190, 68, 241, 255),
                    new Color32(255, 235, 92, 255),
                    new Color32(41, 210, 218, 255),
                    new Color32(255, 143, 31, 255)
                };
                Color32 color = colors[kind];
                if (kind == 0) c.Arc(48, 48, 36, 195, 345, color, 7);
                else if (kind == 1)
                {
                    c.Ellipse(48, 48, 33, 33, color);
                    c.Ellipse(48, 48, 13, 13, White);
                }
                else if (kind == 2)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f;
                        c.Triangle(
                            48, 48,
                            48 + Mathf.RoundToInt(Mathf.Cos(angle - .18f) * 36),
                            48 + Mathf.RoundToInt(Mathf.Sin(angle - .18f) * 36),
                            48 + Mathf.RoundToInt(Mathf.Cos(angle + .18f) * 36),
                            48 + Mathf.RoundToInt(Mathf.Sin(angle + .18f) * 36),
                            color);
                    }
                }
                else if (kind == 3)
                {
                    for (int x = 23; x <= 73; x += 10) c.Line(x, 18, x - 9, 78, color, 4);
                }
                else if (kind == 4)
                {
                    c.Ring(48, 48, 34, 25, color, 6);
                    c.Line(48, 16, 48, 80, White, 3);
                    c.Line(16, 48, 80, 48, White, 3);
                }
                else if (kind == 5)
                {
                    c.Ellipse(48, 48, 34, 34, color);
                    c.Ellipse(48, 48, 18, 18, White);
                    c.Ring(48, 48, 39, 35, color, 4);
                }
                else if (kind == 6)
                {
                    c.Line(35, 13, 56, 42, color, 8);
                    c.Line(56, 42, 39, 54, color, 8);
                    c.Line(39, 54, 61, 83, color, 8);
                }
                else
                {
                    c.Arc(48, 48, 36, 280, 80, color, 8);
                    c.Triangle(76, 27, 84, 48, 63, 42, White);
                }
            }

            private static void DrawDecor(Canvas c, int kind)
            {
                if (kind == 0)
                {
                    c.Ellipse(48, 76, 15, 8, Outline);
                    c.Ellipse(48, 74, 11, 6, ShellLight);
                    c.Line(48, 68, 48, 28, new Color32(75, 145, 52, 255), 5);
                    c.Ellipse(32, 42, 15, 8, new Color32(80, 175, 65, 255));
                    c.Ellipse(64, 35, 15, 8, new Color32(80, 175, 65, 255));
                    c.Ellipse(34, 57, 15, 8, new Color32(80, 175, 65, 255));
                }
                else if (kind == 1)
                {
                    c.Rect(19, 43, 77, 83, Outline);
                    c.Rect(24, 48, 72, 78, new Color32(101, 61, 39, 255));
                    c.Rect(31, 30, 65, 51, Outline);
                    c.Ellipse(48, 26, 23, 12, new Color32(127, 70, 162, 255));
                    c.Rect(31, 56, 42, 77, White);
                    c.Rect(54, 56, 65, 77, White);
                }
                else if (kind == 2)
                {
                    c.Triangle(22, 81, 38, 27, 52, 81, new Color32(45, 198, 217, 255));
                    c.Triangle(44, 81, 61, 20, 75, 81, new Color32(104, 76, 230, 255));
                }
                else
                {
                    c.Ring(48, 48, 40, 30, new Color32(118, 35, 218, 255), 7);
                    c.Ring(48, 48, 31, 22, new Color32(40, 216, 237, 255), 5);
                    c.Ellipse(48, 48, 18, 25, new Color32(39, 16, 68, 255));
                }
            }

            private static Color32 Palette(int index)
            {
                Color32[] colors =
                {
                    new Color32(46, 119, 218, 255),
                    new Color32(238, 137, 35, 255),
                    new Color32(56, 181, 100, 255),
                    new Color32(152, 70, 211, 255)
                };
                return colors[Mathf.Abs(index) % colors.Length];
            }

            private sealed class Canvas
            {
                private readonly int width;
                private readonly int height;
                private readonly Color32[] pixels;

                public Canvas(int width, int height)
                {
                    this.width = width;
                    this.height = height;
                    pixels = new Color32[width * height];
                    for (int index = 0; index < pixels.Length; index++) pixels[index] = Transparent;
                }

                public Sprite ToSprite(string name)
                {
                    Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                    {
                        name = name + "_Texture",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    texture.SetPixels32(pixels);
                    texture.Apply(false, false);
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, width, height),
                        new Vector2(.5f, .5f),
                        66f,
                        0,
                        SpriteMeshType.FullRect);
                    sprite.name = name;
                    return sprite;
                }

                public void Rect(int x0, int y0, int x1, int y1, Color32 color)
                {
                    for (int y = Mathf.Max(0, y0); y <= Mathf.Min(height - 1, y1); y++)
                        for (int x = Mathf.Max(0, x0); x <= Mathf.Min(width - 1, x1); x++) Blend(x, y, color);
                }

                public void Ellipse(int cx, int cy, int rx, int ry, Color32 color)
                {
                    int rx2 = rx * rx;
                    int ry2 = ry * ry;
                    int limit = rx2 * ry2;
                    for (int y = -ry; y <= ry; y++)
                        for (int x = -rx; x <= rx; x++)
                            if (x * x * ry2 + y * y * rx2 <= limit) Blend(cx + x, cy + y, color);
                }

                public void Ring(int cx, int cy, int outer, int inner, Color32 color, int thickness)
                {
                    int outer2 = outer * outer;
                    int innerRadius = Mathf.Max(1, inner - thickness);
                    int inner2 = innerRadius * innerRadius;
                    for (int y = -outer; y <= outer; y++)
                        for (int x = -outer; x <= outer; x++)
                        {
                            int distance = x * x + y * y;
                            if (distance <= outer2 && distance >= inner2) Blend(cx + x, cy + y, color);
                        }
                }

                public void Line(int x0, int y0, int x1, int y1, Color32 color, int thickness)
                {
                    int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                    for (int index = 0; index <= steps; index++)
                    {
                        float t = steps == 0 ? 0f : (float)index / steps;
                        int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                        int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                        Ellipse(x, y, thickness, thickness, color);
                    }
                }

                public void Triangle(int ax, int ay, int bx, int by, int cx, int cy, Color32 color)
                {
                    int minX = Mathf.Min(ax, Mathf.Min(bx, cx));
                    int maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
                    int minY = Mathf.Min(ay, Mathf.Min(by, cy));
                    int maxY = Mathf.Max(ay, Mathf.Max(by, cy));
                    float area = Edge(ax, ay, bx, by, cx, cy);
                    if (Mathf.Abs(area) < .001f) return;
                    for (int y = minY; y <= maxY; y++)
                        for (int x = minX; x <= maxX; x++)
                        {
                            float edge0 = Edge(bx, by, cx, cy, x, y);
                            float edge1 = Edge(cx, cy, ax, ay, x, y);
                            float edge2 = Edge(ax, ay, bx, by, x, y);
                            if ((edge0 >= 0 && edge1 >= 0 && edge2 >= 0) ||
                                (edge0 <= 0 && edge1 <= 0 && edge2 <= 0)) Blend(x, y, color);
                        }
                }

                public void Arc(int cx, int cy, int radius, float startDegrees, float endDegrees, Color32 color, int thickness)
                {
                    float span = endDegrees - startDegrees;
                    if (span <= 0) span += 360f;
                    int steps = Mathf.CeilToInt(span * radius / 20f);
                    for (int index = 0; index <= steps; index++)
                    {
                        float angle = (startDegrees + span * index / Mathf.Max(1, steps)) * Mathf.Deg2Rad;
                        Ellipse(
                            cx + Mathf.RoundToInt(Mathf.Cos(angle) * radius),
                            cy + Mathf.RoundToInt(Mathf.Sin(angle) * radius),
                            thickness,
                            thickness,
                            color);
                    }
                }

                private static float Edge(float ax, float ay, float bx, float by, float px, float py)
                {
                    return (px - ax) * (by - ay) - (py - ay) * (bx - ax);
                }

                private void Blend(int x, int y, Color32 source)
                {
                    if (x < 0 || x >= width || y < 0 || y >= height || source.a == 0) return;
                    int pixelIndex = y * width + x;
                    Color32 destination = pixels[pixelIndex];
                    float sourceAlpha = source.a / 255f;
                    float destinationAlpha = destination.a / 255f;
                    float outputAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
                    if (outputAlpha <= .0001f)
                    {
                        pixels[pixelIndex] = Transparent;
                        return;
                    }
                    pixels[pixelIndex] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(
                            (source.r * sourceAlpha + destination.r * destinationAlpha * (1f - sourceAlpha)) / outputAlpha), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(
                            (source.g * sourceAlpha + destination.g * destinationAlpha * (1f - sourceAlpha)) / outputAlpha), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(
                            (source.b * sourceAlpha + destination.b * destinationAlpha * (1f - sourceAlpha)) / outputAlpha), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(outputAlpha * 255f), 0, 255));
                }
            }
        }
    }
}
