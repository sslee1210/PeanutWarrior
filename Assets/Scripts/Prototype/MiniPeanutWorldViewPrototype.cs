using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Mirrors the three logical support peanuts into the battlefield using the
    /// illustrated companion sprites loaded by ProceduralBattleArtPrototype.
    /// </summary>
    [DefaultExecutionOrder(26100)]
    public sealed class MiniPeanutWorldViewPrototype : MonoBehaviour
    {
        private sealed class PetView
        {
            public GameObject Root;
            public SpriteRenderer Body;
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
        private ProceduralBattleArtPrototype rasterArt;
        private FieldInfo minisField;
        private FieldInfo unlockedField;
        private FieldInfo worldRootField;
        private Transform petRoot;
        private Sprite shadowSprite;

        public int VisibleMiniCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < views.Count; index++)
                    if (views[index]?.Root != null && views[index].Root.activeInHierarchy) count++;
                return count;
            }
        }

        public int VisiblePetCount => VisibleMiniCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<MiniPeanutWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("Peanut Illustrated Support World View");
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
            rasterArt = FindFirstObjectByType<ProceduralBattleArtPrototype>();
            if (idle == null || arena == null || worldView == null || rasterArt == null)
            {
                Debug.LogError("[PeanutSupports] Required support or raster-art system is missing.");
                enabled = false;
                yield break;
            }

            int waitFrames = 0;
            while (!rasterArt.ArtReady && waitFrames < 120)
            {
                waitFrames++;
                yield return null;
            }
            if (!rasterArt.ArtReady)
            {
                Debug.LogError("[PeanutSupports] Illustrated companion sprites were not initialized.");
                enabled = false;
                yield break;
            }

            minisField = typeof(IdleSystemsPrototype).GetField("minis", PrivateInstance);
            unlockedField = typeof(CombatPrototypeArena).GetField("miniSlotsUnlocked", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject rootObject = new GameObject("Illustrated Support Peanuts");
            rootObject.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            petRoot = rootObject.transform;
            shadowSprite = CreateShadowSprite();
            BuildViews();
            Debug.Log("[PeanutSupports] Sword, shield and mage companion sprites connected.");
        }

        private void BuildViews()
        {
            for (int index = 0; index < 3; index++)
            {
                GameObject root = new GameObject($"Illustrated Support Peanut {index + 1}");
                root.transform.SetParent(petRoot, false);
                root.transform.localScale = Vector3.one * 1.05f;

                GameObject shadow = new GameObject("Shadow");
                shadow.transform.SetParent(root.transform, false);
                shadow.transform.localPosition = new Vector3(0f, -0.48f, 0f);
                shadow.transform.localScale = new Vector3(1.2f, 0.65f, 1f);
                SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = shadowSprite;
                shadowRenderer.color = new Color(0f, 0f, 0f, 0.28f);
                shadowRenderer.sortingOrder = 5;

                SpriteRenderer body = root.AddComponent<SpriteRenderer>();
                body.sprite = rasterArt.GetUnitSprite(index + 1);
                body.color = Color.white;
                body.sortingOrder = 9;

                TrailRenderer trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.14f;
                trail.startWidth = 0.15f;
                trail.endWidth = 0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.sortingOrder = 7;

                views.Add(new PetView
                {
                    Root = root,
                    Body = body,
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

            for (int index = 0; index < views.Count; index++)
            {
                PetView view = views[index];
                bool active = unlocked && pets != null && index < pets.Count && pets[index] != null;
                if (view.Root.activeSelf != active) view.Root.SetActive(active);
                if (!active) continue;

                object pet = pets[index];
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

                Color elementColor = ElementColor(index);
                view.Body.color = Color.white;
                view.Trail.startColor = new Color(elementColor.r, elementColor.g, elementColor.b, 0.58f);
                view.Trail.endColor = new Color(elementColor.r, elementColor.g, elementColor.b, 0f);

                int stars = progression == null
                    ? 1
                    : progression.GetStars((PetProgressionPrototype.PetElement)index);
                float baseScale = 1.02f + Mathf.Min(0.22f, (stars - 1) * 0.05f);
                float pulse = Mathf.Sin(Time.time * 5f + index) * 0.018f;
                view.Root.transform.localScale = Vector3.one * (baseScale + pulse);
            }
        }

        private static Sprite CreateShadowSprite()
        {
            const int width = 48;
            const int height = 20;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "IllustratedSupportShadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 normalized = new Vector2(
                        (x - center.x) / center.x,
                        (y - center.y) / center.y);
                    float alpha = Mathf.Clamp01(1f - normalized.sqrMagnitude) * 0.7f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                48f,
                0,
                SpriteMeshType.FullRect);
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
    }
}
