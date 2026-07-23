using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(17700)]
    public sealed class AdvancementSkillEvolutionWorldViewPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const float SourceLeft = 55f;
        private const float SourceRight = 705f;
        private const float SourceTop = 155f;
        private const float SourceBottom = 425f;
        private const float WorldHalfWidth = 8.2f;
        private const float WorldHalfHeight = 4.2f;

        private CombatPrototypeArena arena;
        private RuntimeWorldViewPrototype worldView;
        private SkillManagementPrototype skills;
        private AdvancementProgressionPrototype advancement;
        private FieldInfo cooldownsField;
        private FieldInfo playerPositionField;
        private FieldInfo enemiesField;
        private FieldInfo worldRootField;
        private readonly float[] previousCooldowns = new float[8];
        private Transform effectRoot;
        private Sprite ringSprite;
        private Sprite slashSprite;
        private Sprite coreSprite;

        public bool UsesPerTierVisualEvolution => true;
        public bool ScalesEffectObjectCounts => true;
        public bool ChangesAdvancementColorTheme => true;
        public bool ShowsAdvancementAscensionBurst => true;
        public int LastRenderedTier { get; private set; }
        public int LastRenderedObjectCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<AdvancementSkillEvolutionWorldViewPrototype>() != null) return;
            GameObject go = new GameObject("PeanutWarriorAdvancementSkillEvolutionWorldView");
            DontDestroyOnLoad(go);
            go.AddComponent<AdvancementSkillEvolutionWorldViewPrototype>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 40; frame++)
            {
                arena = FindFirstObjectByType<CombatPrototypeArena>();
                worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
                skills = FindFirstObjectByType<SkillManagementPrototype>();
                advancement = FindFirstObjectByType<AdvancementProgressionPrototype>();
                if (arena != null && worldView != null && skills != null && advancement != null) break;
                yield return null;
            }

            if (arena == null || worldView == null || skills == null || advancement == null)
            {
                enabled = false;
                yield break;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            cooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject root = new GameObject("Advancement Skill Evolution Effects");
            root.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            effectRoot = root.transform;
            ringSprite = CreateRingSprite(128);
            slashSprite = CreateSlashSprite(128, 26);
            coreSprite = CreateCoreSprite(128);

            float[] cooldowns = ReadCooldowns();
            if (cooldowns != null)
                Array.Copy(cooldowns, previousCooldowns, Mathf.Min(cooldowns.Length, previousCooldowns.Length));

            advancement.AdvancementChanged += HandleAdvancementChanged;
        }

        private void OnDestroy()
        {
            if (advancement != null) advancement.AdvancementChanged -= HandleAdvancementChanged;
        }

        private void Update()
        {
            if (effectRoot == null) return;
            float[] cooldowns = ReadCooldowns();
            if (cooldowns == null || cooldowns.Length < 8) return;

            for (int index = 0; index < cooldowns.Length && index < previousCooldowns.Length; index++)
            {
                bool cast = cooldowns[index] > previousCooldowns[index] + 1f;
                previousCooldowns[index] = cooldowns[index];
                if (cast) StartCoroutine(PlayEvolutionOverlay(index));
            }
        }

        private IEnumerator PlayEvolutionOverlay(int index)
        {
            int tier = skills.CurrentAdvancementTier;
            int rank = skills.CurrentEvolutionRank;
            int requestedObjects = skills.GetSkillVisualObjectCount(index);
            float range = skills.GetSkillRangeMultiplier(index);
            Color primary = AdvancementColor(tier);
            Color secondary = Color.Lerp(primary, Color.white, 0.52f);
            Vector3 player = ToWorld(ReadPlayerPosition());
            Vector3 target = GetTargetWorldPosition();
            LastRenderedTier = tier;
            LastRenderedObjectCount = requestedObjects;

            if (tier > 0) SpawnEvolutionLabel(target + Vector3.up * 1.8f, skills.GetEvolutionGradeName() + " 진화", primary);

            switch (index)
            {
                case 0:
                {
                    int blades = Mathf.Min(6 + tier * 2, 20);
                    for (int i = 0; i < rank; i++) SpawnRing(player, i * 0.35f + 0.7f, (3.0f + i * 0.7f) * range, primary, 0.62f);
                    for (int i = 0; i < blades; i++)
                    {
                        float angle = i * 360f / blades;
                        Vector3 start = player + Quaternion.Euler(0f, 0f, angle) * Vector3.right * (1.0f + tier * 0.08f);
                        SpawnMovingSlash(start, target, angle + 90f, i % 2 == 0 ? primary : secondary, 0.75f + tier * 0.05f, 0.48f);
                    }
                    break;
                }
                case 1:
                {
                    int swords = Mathf.Min(8 + tier * 4, 38);
                    SpawnRing(target + Vector3.up * 3.0f, 0.4f, 3.2f * range, primary, 0.85f);
                    for (int i = 0; i < swords; i++)
                    {
                        Vector3 landing = target + (Vector3)(UnityEngine.Random.insideUnitCircle * 3.2f * range);
                        Vector3 start = landing + Vector3.up * UnityEngine.Random.Range(4.4f, 6.4f);
                        SpawnMovingSlash(start, landing, -90f, i % 3 == 0 ? primary : secondary, 0.58f + tier * 0.035f, 0.42f);
                        if (i % 8 == 7) yield return new WaitForSeconds(0.035f);
                    }
                    for (int i = 0; i < rank; i++) SpawnRing(target, 0.4f + i * 0.3f, (4.8f + i) * range, secondary, 0.55f);
                    break;
                }
                case 2:
                {
                    int rays = 6 + tier;
                    int pods = 2 + tier;
                    for (int i = 0; i < rays; i++)
                    {
                        float angle = i * 360f / rays;
                        Vector3 end = target + Quaternion.Euler(0f, 0f, angle) * Vector3.right * 4.2f * range;
                        SpawnMovingSlash(target, end, angle, primary, 0.5f + tier * 0.04f, 0.55f);
                    }
                    yield return new WaitForSeconds(0.12f);
                    for (int i = 0; i < pods; i++)
                    {
                        float angle = i * 360f / pods;
                        Vector3 point = target + Quaternion.Euler(0f, 0f, angle) * Vector3.right * (1.0f + i * 0.25f) * range;
                        SpawnCore(point, primary, 0.25f, 1.0f + tier * 0.10f, 0.48f);
                        SpawnRing(point, 0.2f, 1.8f + tier * 0.12f, secondary, 0.48f);
                    }
                    break;
                }
                case 3:
                {
                    int swords = Mathf.Min(12 + tier * 5, 48);
                    for (int i = 0; i < rank; i++) SpawnRing(target + Vector3.up * 2.4f, 0.5f + i * 0.3f, (4.2f + i) * range, primary, 0.9f);
                    for (int i = 0; i < swords; i++)
                    {
                        float angle = Mathf.Lerp(-88f, 88f, i / Mathf.Max(1f, swords - 1f));
                        Vector3 start = target + Vector3.up * 3f + Quaternion.Euler(0f, 0f, angle) * Vector3.up * 1.8f;
                        Vector3 end = target + Quaternion.Euler(0f, 0f, angle - 90f) * Vector3.right * 5.5f * range;
                        SpawnMovingSlash(start, end, angle - 90f, i % 4 == 0 ? primary : secondary, 0.62f + tier * 0.04f, 0.50f);
                    }
                    for (int i = 0; i < rank; i++)
                        SpawnMovingSlash(target + Vector3.up * (5.0f + i), target + Vector3.down * 0.5f, -90f, secondary, 2.4f + i * 0.8f, 0.72f);
                    break;
                }
                case 4:
                {
                    int wings = Mathf.Min(6 + tier * 2, 20);
                    for (int i = 0; i < wings; i++)
                    {
                        float angle = i * 360f / wings;
                        Vector3 start = player + Quaternion.Euler(0f, 0f, angle) * Vector3.up * (1.4f + tier * 0.08f);
                        SpawnMovingSlash(start, target, angle + 180f, i % 2 == 0 ? primary : secondary, 0.85f + tier * 0.04f, 0.65f);
                    }
                    for (int i = 0; i < rank; i++) SpawnRing(target, 0.4f, (2.6f + i * 0.9f) * range, primary, 0.62f);
                    break;
                }
                case 5:
                {
                    int afterImages = Mathf.Min(8 + tier * 2, 24);
                    for (int i = 0; i < afterImages; i++)
                    {
                        float angle = i * 360f / afterImages;
                        Vector3 start = target + Quaternion.Euler(0f, 0f, angle) * Vector3.right * (2.2f + tier * 0.08f);
                        SpawnMovingSlash(start, target, angle + 135f, i % 2 == 0 ? primary : secondary, 0.95f + tier * 0.04f, 0.24f);
                    }
                    yield return new WaitForSeconds(0.18f);
                    for (int i = 0; i < rank; i++) SpawnRing(target, 0.25f, (4.0f + i * 1.1f) * range, secondary, 0.42f);
                    break;
                }
                case 6:
                {
                    int converging = Mathf.Min(6 + tier * 2, 22);
                    for (int i = 0; i < rank; i++)
                    {
                        SpawnRing(target + Vector3.up * 2.6f, 0.4f, (2.8f + i * 0.7f) * range, primary, 0.92f);
                        SpawnRing(target + Vector3.down * 2.2f, 0.4f, (2.8f + i * 0.7f) * range, secondary, 0.92f);
                    }
                    for (int i = 0; i < converging; i++)
                    {
                        float x = Mathf.Lerp(-3.4f, 3.4f, i / Mathf.Max(1f, converging - 1f));
                        SpawnMovingSlash(target + new Vector3(x, 3.4f, 0f), target, -90f, primary, 0.56f + tier * 0.03f, 0.46f);
                        SpawnMovingSlash(target + new Vector3(-x, -3.0f, 0f), target, 90f, secondary, 0.56f + tier * 0.03f, 0.46f);
                    }
                    break;
                }
                case 7:
                {
                    int heavenlyCuts = skills.GetSkillHitCount(7);
                    SpawnCore(player + Vector3.up * 2.5f, primary, 0.5f, 4.2f + tier * 0.28f, 0.95f);
                    for (int i = 0; i < 8 + tier * 2; i++)
                    {
                        float angle = i * 360f / (8 + tier * 2);
                        Vector3 start = player + Vector3.up * 2.5f + Quaternion.Euler(0f, 0f, angle) * Vector3.right * 4.8f;
                        SpawnMovingSlash(start, player + Vector3.up * 2.5f, angle + 180f, secondary, 0.46f, 0.65f);
                    }
                    yield return new WaitForSeconds(0.35f);
                    for (int i = 0; i < heavenlyCuts; i++)
                    {
                        float offset = (i - (heavenlyCuts - 1) * 0.5f) * 1.0f;
                        SpawnMovingSlash(target + new Vector3(offset, 7f, 0f), target + new Vector3(-offset, -1.4f, 0f), -90f + i * 8f, i == heavenlyCuts - 1 ? Color.white : primary, 3.4f + tier * 0.16f, 0.78f);
                    }
                    for (int i = 0; i < rank; i++) SpawnRing(target, 0.7f, (8.0f + i * 1.5f) * range, primary, 0.88f);
                    break;
                }
            }
        }

        private void HandleAdvancementChanged()
        {
            if (effectRoot != null) StartCoroutine(PlayAdvancementAscension());
        }

        private IEnumerator PlayAdvancementAscension()
        {
            int tier = skills.CurrentAdvancementTier;
            Color primary = AdvancementColor(tier);
            Vector3 player = ToWorld(ReadPlayerPosition());
            SpawnEvolutionLabel(player + Vector3.up * 2.2f, skills.CurrentAdvancementName, primary);
            int rings = 2 + skills.CurrentEvolutionRank;
            for (int i = 0; i < rings; i++)
            {
                SpawnRing(player, 0.3f + i * 0.15f, 3.2f + i * 1.2f, i % 2 == 0 ? primary : Color.white, 0.75f);
                yield return new WaitForSeconds(0.08f);
            }
            int blades = 8 + tier * 2;
            for (int i = 0; i < blades; i++)
            {
                float angle = i * 360f / blades;
                Vector3 start = player + Quaternion.Euler(0f, 0f, angle) * Vector3.right * 4.2f;
                SpawnMovingSlash(start, player, angle + 180f, primary, 0.75f + tier * 0.04f, 0.65f);
            }
        }

        private Color AdvancementColor(int tier)
        {
            return tier switch
            {
                0 => new Color(0.82f, 0.72f, 0.25f),
                1 => new Color(0.96f, 0.50f, 0.14f),
                2 => new Color(1.00f, 0.82f, 0.20f),
                3 => new Color(1.00f, 0.26f, 0.08f),
                4 => new Color(0.24f, 0.84f, 1.00f),
                5 => new Color(0.62f, 0.30f, 1.00f),
                6 => new Color(1.00f, 0.72f, 0.10f),
                7 => new Color(0.94f, 0.20f, 1.00f),
                _ => Color.white
            };
        }

        private void SpawnRing(Vector3 position, float from, float to, Color color, float duration)
        {
            SpriteRenderer renderer = CreateRenderer("Evolution Ring", ringSprite, color, 40);
            renderer.transform.position = position;
            StartCoroutine(ScaleFade(renderer, from, to, duration, 90f));
        }

        private void SpawnCore(Vector3 position, Color color, float from, float to, float duration)
        {
            SpriteRenderer renderer = CreateRenderer("Evolution Core", coreSprite, color, 44);
            renderer.transform.position = position;
            StartCoroutine(ScaleFade(renderer, from, to, duration, 120f));
        }

        private void SpawnMovingSlash(Vector3 start, Vector3 end, float angle, Color color, float scale, float duration)
        {
            SpriteRenderer renderer = CreateRenderer("Evolution Sword", slashSprite, color, 43);
            renderer.transform.position = start;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            StartCoroutine(MoveScaleFade(renderer, start, end, scale * 0.55f, scale * 1.35f, duration));
        }

        private void SpawnEvolutionLabel(Vector3 position, string value, Color color)
        {
            GameObject go = new GameObject("Skill Evolution Label");
            go.transform.SetParent(effectRoot, false);
            go.transform.position = position;
            TextMesh text = go.AddComponent<TextMesh>();
            text.text = value;
            text.alignment = TextAlignment.Center;
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 42;
            text.characterSize = 0.055f;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = 50;
            StartCoroutine(FloatFadeText(text, 0.82f));
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color color, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(effectRoot, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private IEnumerator ScaleFade(SpriteRenderer renderer, float from, float to, float duration, float rotationSpeed)
        {
            float elapsed = 0f;
            while (renderer != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                renderer.transform.localScale = Vector3.one * Mathf.Lerp(from, to, EaseOut(t));
                renderer.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
                Color color = renderer.color;
                color.a = Mathf.Sin(t * Mathf.PI);
                renderer.color = color;
                yield return null;
            }
            if (renderer != null) Destroy(renderer.gameObject);
        }

        private IEnumerator MoveScaleFade(SpriteRenderer renderer, Vector3 start, Vector3 end, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (renderer != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                renderer.transform.position = Vector3.Lerp(start, end, EaseOut(t));
                renderer.transform.localScale = Vector3.one * Mathf.Lerp(from, to, t);
                Color color = renderer.color;
                color.a = 1f - t * 0.82f;
                renderer.color = color;
                yield return null;
            }
            if (renderer != null) Destroy(renderer.gameObject);
        }

        private IEnumerator FloatFadeText(TextMesh text, float duration)
        {
            float elapsed = 0f;
            Vector3 start = text.transform.position;
            while (text != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                text.transform.position = start + Vector3.up * t * 0.9f;
                Color color = text.color;
                color.a = 1f - t;
                text.color = color;
                yield return null;
            }
            if (text != null) Destroy(text.gameObject);
        }

        private float[] ReadCooldowns()
        {
            return cooldownsField?.GetValue(arena) as float[];
        }

        private Vector2 ReadPlayerPosition()
        {
            return playerPositionField == null ? new Vector2(380f, 280f) : (Vector2)playerPositionField.GetValue(arena);
        }

        private Vector3 GetTargetWorldPosition()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null || enemies.Count == 0) return ToWorld(ReadPlayerPosition());
            object target = enemies[0];
            for (int i = 0; i < enemies.Count; i++)
            {
                object candidate = enemies[i];
                FieldInfo bossField = candidate?.GetType().GetField("IsBoss", PublicInstance);
                if (bossField != null && Convert.ToBoolean(bossField.GetValue(candidate)))
                {
                    target = candidate;
                    break;
                }
            }
            FieldInfo positionField = target?.GetType().GetField("Position", PublicInstance);
            Vector2 source = positionField == null ? ReadPlayerPosition() : (Vector2)positionField.GetValue(target);
            return ToWorld(source);
        }

        private static Vector3 ToWorld(Vector2 source)
        {
            float x = Mathf.Lerp(-WorldHalfWidth, WorldHalfWidth, Mathf.InverseLerp(SourceLeft, SourceRight, source.x));
            float y = Mathf.Lerp(WorldHalfHeight, -WorldHalfHeight, Mathf.InverseLerp(SourceTop, SourceBottom, source.y));
            return new Vector3(x, y, 0f);
        }

        private static float EaseOut(float t)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        }

        private static Sprite CreateRingSprite(int size)
        {
            Texture2D texture = TransparentTexture(size, size, "AdvancementEvolutionRing");
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.43f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / 5f);
                    if (alpha > 0f) texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            return FinalizeSprite(texture, new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateSlashSprite(int width, int height)
        {
            Texture2D texture = TransparentTexture(width, height, "AdvancementEvolutionSlash");
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float vertical = 1f - Mathf.Abs((y / (height - 1f)) * 2f - 1f);
                    float horizontal = Mathf.Sin((x / (width - 1f)) * Mathf.PI);
                    float alpha = Mathf.Pow(Mathf.Clamp01(vertical * horizontal), 1.35f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            return FinalizeSprite(texture, new Vector2(0.5f, 0.5f), 48f);
        }

        private static Sprite CreateCoreSprite(int size)
        {
            Texture2D texture = TransparentTexture(size, size, "AdvancementEvolutionCore");
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.46f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float normalized = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = normalized <= 1f ? Mathf.Pow(1f - normalized, 0.4f) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            return FinalizeSprite(texture, new Vector2(0.5f, 0.5f), size);
        }

        private static Texture2D TransparentTexture(int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) texture.SetPixel(x, y, clear);
            return texture;
        }

        private static Sprite FinalizeSprite(Texture2D texture, Vector2 pivot, float pixelsPerUnit)
        {
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, pixelsPerUnit);
        }
    }
}
