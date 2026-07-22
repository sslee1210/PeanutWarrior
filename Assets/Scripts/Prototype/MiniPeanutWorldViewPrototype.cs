using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Mirrors the three logical mini peanuts into the real 2D battlefield. Minis stay
    /// untargetable and immortal; this component is presentation-only.
    /// </summary>
    [DefaultExecutionOrder(17600)]
    public sealed class MiniPeanutWorldViewPrototype : MonoBehaviour
    {
        private sealed class MiniView
        {
            public GameObject Root;
            public SpriteRenderer Body;
            public TextMesh Label;
            public Vector3 PreviousPosition;
            public TrailRenderer Trail;
        }

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const float SourceLeft = 55f;
        private const float SourceRight = 705f;
        private const float SourceTop = 155f;
        private const float SourceBottom = 425f;
        private const float WorldHalfWidth = 8.2f;
        private const float WorldHalfHeight = 4.2f;

        private readonly List<MiniView> views = new List<MiniView>();
        private IdleSystemsPrototype idle;
        private CombatPrototypeArena arena;
        private RuntimeWorldViewPrototype worldView;
        private FieldInfo minisField;
        private FieldInfo unlockedField;
        private FieldInfo advancementField;
        private FieldInfo worldRootField;
        private Sprite miniSprite;
        private Transform miniRoot;

        public int VisibleMiniCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < views.Count; i++)
                    if (views[i]?.Root != null && views[i].Root.activeInHierarchy) count++;
                return count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<MiniPeanutWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMiniWorldViewPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<MiniPeanutWorldViewPrototype>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            idle = FindFirstObjectByType<IdleSystemsPrototype>();
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (idle == null || arena == null || worldView == null)
            {
                enabled = false;
                yield break;
            }

            minisField = typeof(IdleSystemsPrototype).GetField("minis", PrivateInstance);
            unlockedField = typeof(CombatPrototypeArena).GetField("miniSlotsUnlocked", PrivateInstance);
            advancementField = typeof(CombatPrototypeArena).GetField("advancementTier", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject rootObject = new GameObject("Mini Peanuts");
            rootObject.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            miniRoot = rootObject.transform;
            miniSprite = CreateMiniSprite();
            BuildViews();
        }

        private void BuildViews()
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject root = new GameObject($"Mini Peanut {i + 1}");
                root.transform.SetParent(miniRoot, false);
                root.transform.localScale = Vector3.one * 0.55f;

                GameObject shadow = new GameObject("Shadow");
                shadow.transform.SetParent(root.transform, false);
                shadow.transform.localPosition = new Vector3(0f, -0.28f, 0f);
                shadow.transform.localScale = new Vector3(1f, 0.42f, 1f);
                SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = miniSprite;
                shadowRenderer.color = new Color(0f, 0f, 0f, 0.25f);
                shadowRenderer.sortingOrder = 4;

                SpriteRenderer body = root.AddComponent<SpriteRenderer>();
                body.sprite = miniSprite;
                body.sortingOrder = 8;

                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(root.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                TextMesh label = labelObject.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontSize = 34;
                label.characterSize = 0.075f;
                label.color = Color.white;
                label.GetComponent<MeshRenderer>().sortingOrder = 12;

                TrailRenderer trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.12f;
                trail.startWidth = 0.18f;
                trail.endWidth = 0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.sortingOrder = 7;

                views.Add(new MiniView
                {
                    Root = root,
                    Body = body,
                    Label = label,
                    Trail = trail,
                    PreviousPosition = root.transform.position
                });
            }
        }

        private void LateUpdate()
        {
            if (idle == null || arena == null || views.Count == 0) return;
            bool unlocked = unlockedField != null && Convert.ToBoolean(unlockedField.GetValue(arena));
            IList minis = minisField?.GetValue(idle) as IList;
            int advancement = advancementField == null ? 0 : Convert.ToInt32(advancementField.GetValue(arena));

            for (int i = 0; i < views.Count; i++)
            {
                MiniView view = views[i];
                bool active = unlocked && minis != null && i < minis.Count && minis[i] != null;
                if (view.Root.activeSelf != active) view.Root.SetActive(active);
                if (!active) continue;

                object mini = minis[i];
                Type miniType = mini.GetType();
                Vector2 source = ReadVector2(miniType, mini, "Position");
                int element = ReadEnum(miniType, mini, "Element");
                Vector3 target = SourceToWorld(source);
                view.Root.transform.position = Vector3.Lerp(
                    view.Root.transform.position,
                    target,
                    1f - Mathf.Exp(-24f * Time.deltaTime));

                float speed = Vector3.Distance(view.PreviousPosition, view.Root.transform.position) /
                              Mathf.Max(Time.deltaTime, 0.0001f);
                view.Trail.emitting = speed > 2.2f;
                view.PreviousPosition = view.Root.transform.position;
                view.Body.color = ElementColor(element);
                view.Trail.startColor = new Color(view.Body.color.r, view.Body.color.g, view.Body.color.b, 0.65f);
                view.Trail.endColor = new Color(view.Body.color.r, view.Body.color.g, view.Body.color.b, 0f);
                view.Label.text = $"미니 {i + 1}\n{ElementName(element)} · 전직 {Mathf.Max(0, advancement - 1)}";
                view.Root.transform.localScale = Vector3.one * (0.55f + Mathf.Sin(Time.time * 6f + i) * 0.02f);
            }
        }

        private static Sprite CreateMiniSprite()
        {
            const int width = 40;
            const int height = 48;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "ProceduralMiniPeanut";
            texture.filterMode = FilterMode.Bilinear;
            Vector2 upper = new Vector2(width * 0.5f, height * 0.34f);
            Vector2 lower = new Vector2(width * 0.5f, height * 0.68f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float upperDistance = Vector2.Distance(new Vector2((point.x - upper.x) / 14f, (point.y - upper.y) / 12f), Vector2.zero);
                    float lowerDistance = Vector2.Distance(new Vector2((point.x - lower.x) / 14f, (point.y - lower.y) / 12f), Vector2.zero);
                    float alpha = Mathf.Clamp01((1f - Mathf.Min(upperDistance, lowerDistance)) * 4f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 48f);
        }

        private static Vector2 ReadVector2(Type type, object instance, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, PublicInstance);
            return field == null ? Vector2.zero : (Vector2)field.GetValue(instance);
        }

        private static int ReadEnum(Type type, object instance, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, PublicInstance);
            return field == null ? 0 : Convert.ToInt32(field.GetValue(instance));
        }

        private static Vector3 SourceToWorld(Vector2 source)
        {
            float x = Mathf.Lerp(-WorldHalfWidth, WorldHalfWidth, Mathf.InverseLerp(SourceLeft, SourceRight, source.x));
            float y = Mathf.Lerp(WorldHalfHeight, -WorldHalfHeight, Mathf.InverseLerp(SourceTop, SourceBottom, source.y));
            return new Vector3(x, y, 0f);
        }

        private static Color ElementColor(int element)
        {
            return element switch
            {
                0 => new Color(1f, 0.35f, 0.12f),
                1 => new Color(0.34f, 0.82f, 1f),
                2 => new Color(0.78f, 0.52f, 1f),
                _ => new Color(0.9f, 0.72f, 0.26f)
            };
        }

        private static string ElementName(int element)
        {
            return element switch
            {
                0 => "화염",
                1 => "냉기",
                2 => "번개",
                _ => "미니"
            };
        }
    }
}
