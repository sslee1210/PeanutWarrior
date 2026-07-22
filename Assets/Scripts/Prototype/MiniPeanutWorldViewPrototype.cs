using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Mirrors the three logical pets into the real 2D battlefield. Pets are
    /// untargetable and immortal; their level and stars come from PetProgression.
    /// </summary>
    [DefaultExecutionOrder(17600)]
    public sealed class MiniPeanutWorldViewPrototype : MonoBehaviour
    {
        private sealed class PetView
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

        private readonly List<PetView> views = new List<PetView>();
        private IdleSystemsPrototype idle;
        private CombatPrototypeArena arena;
        private RuntimeWorldViewPrototype worldView;
        private PetProgressionPrototype progression;
        private FieldInfo minisField;
        private FieldInfo unlockedField;
        private FieldInfo worldRootField;
        private Sprite petSprite;
        private Transform petRoot;

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

        public int VisiblePetCount => VisibleMiniCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<MiniPeanutWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPetWorldViewPrototype");
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
            progression = FindFirstObjectByType<PetProgressionPrototype>();
            if (idle == null || arena == null || worldView == null)
            {
                enabled = false;
                yield break;
            }

            minisField = typeof(IdleSystemsPrototype).GetField("minis", PrivateInstance);
            unlockedField = typeof(CombatPrototypeArena).GetField("miniSlotsUnlocked", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject rootObject = new GameObject("Peanut Pets");
            rootObject.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            petRoot = rootObject.transform;
            petSprite = CreatePetSprite();
            BuildViews();
        }

        private void BuildViews()
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject root = new GameObject($"Peanut Pet {i + 1}");
                root.transform.SetParent(petRoot, false);
                root.transform.localScale = Vector3.one * 0.58f;

                GameObject shadow = new GameObject("Shadow");
                shadow.transform.SetParent(root.transform, false);
                shadow.transform.localPosition = new Vector3(0f, -0.30f, 0f);
                shadow.transform.localScale = new Vector3(1f, 0.42f, 1f);
                SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = petSprite;
                shadowRenderer.color = new Color(0f, 0f, 0f, 0.25f);
                shadowRenderer.sortingOrder = 4;

                SpriteRenderer body = root.AddComponent<SpriteRenderer>();
                body.sprite = petSprite;
                body.sortingOrder = 8;

                GameObject glowObject = new GameObject("Element Glow");
                glowObject.transform.SetParent(root.transform, false);
                glowObject.transform.localScale = Vector3.one * 1.18f;
                SpriteRenderer glow = glowObject.AddComponent<SpriteRenderer>();
                glow.sprite = petSprite;
                glow.color = new Color(1f, 1f, 1f, 0.16f);
                glow.sortingOrder = 7;

                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(root.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.82f, 0f);
                TextMesh label = labelObject.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontSize = 34;
                label.characterSize = 0.070f;
                label.color = Color.white;
                label.GetComponent<MeshRenderer>().sortingOrder = 12;

                TrailRenderer trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.12f;
                trail.startWidth = 0.18f;
                trail.endWidth = 0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.sortingOrder = 7;

                views.Add(new PetView
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
            if (progression == null) progression = FindFirstObjectByType<PetProgressionPrototype>();

            bool unlocked = progression != null
                ? progression.IsUnlocked
                : unlockedField != null && Convert.ToBoolean(unlockedField.GetValue(arena));
            IList pets = minisField?.GetValue(idle) as IList;

            for (int i = 0; i < views.Count; i++)
            {
                PetView view = views[i];
                bool active = unlocked && pets != null && i < pets.Count && pets[i] != null;
                if (view.Root.activeSelf != active) view.Root.SetActive(active);
                if (!active) continue;

                object pet = pets[i];
                Type petType = pet.GetType();
                Vector2 source = ReadVector2(petType, pet, "Position");
                Vector3 target = SourceToWorld(source);
                view.Root.transform.position = Vector3.Lerp(
                    view.Root.transform.position,
                    target,
                    1f - Mathf.Exp(-24f * Time.deltaTime));

                float speed = Vector3.Distance(view.PreviousPosition, view.Root.transform.position) /
                              Mathf.Max(Time.deltaTime, 0.0001f);
                view.Trail.emitting = speed > 2.2f;
                view.PreviousPosition = view.Root.transform.position;

                Color elementColor = ElementColor(i);
                view.Body.color = elementColor;
                view.Trail.startColor = new Color(elementColor.r, elementColor.g, elementColor.b, 0.65f);
                view.Trail.endColor = new Color(elementColor.r, elementColor.g, elementColor.b, 0f);

                int level = progression == null ? 1 : progression.GetLevel((PetProgressionPrototype.PetElement)i);
                int stars = progression == null ? 1 : progression.GetStars((PetProgressionPrototype.PetElement)i);
                string name = progression == null
                    ? ElementName(i) + " 펫"
                    : progression.GetDisplayName((PetProgressionPrototype.PetElement)i);
                view.Label.text = $"{name}\nLv.{level} · {BuildStars(stars)}";

                float baseScale = 0.56f + Mathf.Min(0.16f, (stars - 1) * 0.035f);
                float pulse = Mathf.Sin(Time.time * 6f + i) * 0.018f;
                view.Root.transform.localScale = Vector3.one * (baseScale + pulse);
            }
        }

        private static string BuildStars(int count)
        {
            count = Mathf.Clamp(count, 1, 5);
            return new string('★', count) + new string('☆', 5 - count);
        }

        private static Sprite CreatePetSprite()
        {
            const int width = 40;
            const int height = 48;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "ProceduralPeanutPet";
            texture.filterMode = FilterMode.Bilinear;
            Vector2 upper = new Vector2(width * 0.5f, height * 0.34f);
            Vector2 lower = new Vector2(width * 0.5f, height * 0.68f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float upperDistance = Vector2.Distance(
                        new Vector2((point.x - upper.x) / 14f, (point.y - upper.y) / 12f), Vector2.zero);
                    float lowerDistance = Vector2.Distance(
                        new Vector2((point.x - lower.x) / 14f, (point.y - lower.y) / 12f), Vector2.zero);
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
                _ => "펫"
            };
        }
    }
}
