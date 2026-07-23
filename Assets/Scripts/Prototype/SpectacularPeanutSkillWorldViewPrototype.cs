using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(17600)]
    public sealed class SpectacularPeanutSkillWorldViewPrototype : MonoBehaviour
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
        private StageFlowController stageFlow;
        private RuntimeWorldViewPrototype worldView;
        private FieldInfo playerPositionField;
        private FieldInfo cooldownsField;
        private FieldInfo enemiesField;
        private FieldInfo worldRootField;
        private Transform effectRoot;
        private readonly float[] previousCooldowns = new float[8];
        private Sprite ringSprite;
        private Sprite slashSprite;
        private Sprite petalSprite;
        private Sprite podSprite;
        private Sprite coreSprite;

        public bool UsesEightUniqueSpectacleSequences => true;
        public bool ReplacesLegacyGenericSkillEffects => true;
        public string LastVisualSequence { get; private set; } = "연출 대기";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SpectacularPeanutSkillWorldViewPrototype>() != null) return;
            GameObject go = new GameObject("PeanutWarriorSpectacularSkillWorldView");
            DontDestroyOnLoad(go);
            go.AddComponent<SpectacularPeanutSkillWorldViewPrototype>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 30; frame++)
            {
                arena = FindFirstObjectByType<CombatPrototypeArena>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
                if (arena != null && stageFlow != null && worldView != null) break;
                yield return null;
            }

            if (arena == null || stageFlow == null || worldView == null)
            {
                enabled = false;
                yield break;
            }

            CombatEffectWorldViewPrototype legacy = FindFirstObjectByType<CombatEffectWorldViewPrototype>();
            if (legacy != null) legacy.enabled = false;

            Type arenaType = typeof(CombatPrototypeArena);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            cooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject root = new GameObject("Spectacular Peanut Skill Effects");
            root.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            effectRoot = root.transform;
            CreateSprites();

            float[] cooldowns = ReadCooldowns();
            if (cooldowns != null)
                Array.Copy(cooldowns, previousCooldowns, Mathf.Min(cooldowns.Length, previousCooldowns.Length));
        }

        private void Update()
        {
            if (effectRoot == null) return;
            float[] cooldowns = ReadCooldowns();
            if (cooldowns == null || cooldowns.Length < 8) return;
            int start = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;

            for (int i = 0; i < cooldowns.Length && i < previousCooldowns.Length; i++)
            {
                bool cast = i >= start && i < start + 4 && cooldowns[i] > previousCooldowns[i] + 1f;
                previousCooldowns[i] = cooldowns[i];
                if (!cast) continue;
                StartCoroutine(PlaySkillSequence(i));
            }
        }

        private IEnumerator PlaySkillSequence(int index)
        {
            Vector3 player = ToWorld(ReadPlayerPosition());
            Vector3 target = GetTargetWorldPosition();
            Color gold = new Color(1f, 0.78f, 0.16f);
            Color paleGold = new Color(1f, 0.94f, 0.58f);
            Color rootGreen = new Color(0.30f, 0.88f, 0.38f);
            Color crimson = new Color(1f, 0.24f, 0.16f);

            switch (index)
            {
                case 0:
                    LastVisualSequence = "껍질 회전참";
                    SpawnRing(player, gold, 0.7f, 4.5f, 0.65f);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * 45f;
                        Vector3 offset = Quaternion.Euler(0f, 0f, angle) * Vector3.right * 1.15f;
                        SpawnOrbitingSlash(player + offset, player, angle + 90f, gold, 0.72f, 1.8f, 0.55f);
                    }
                    yield return new WaitForSeconds(0.18f);
                    SpawnRing(target, paleGold, 0.35f, 3.1f, 0.42f);
                    break;

                case 1:
                    LastVisualSequence = "낙화검우";
                    Vector3 flower = target + Vector3.up * 3.2f;
                    SpawnFlower(flower, gold, 2.6f, 1.1f);
                    yield return new WaitForSeconds(0.22f);
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 landing = target + (Vector3)(UnityEngine.Random.insideUnitCircle * 3.5f);
                        Vector3 start = landing + Vector3.up * UnityEngine.Random.Range(4.2f, 6.2f);
                        SpawnMovingSlash(start, landing, UnityEngine.Random.Range(-12f, 12f), paleGold, 0.68f, 0.42f);
                        if (i % 4 == 3) yield return new WaitForSeconds(0.06f);
                    }
                    yield return new WaitForSeconds(0.34f);
                    SpawnRing(target, gold, 0.6f, 6.8f, 0.62f);
                    break;

                case 2:
                    LastVisualSequence = "지맥꼬투리진";
                    SpawnRing(target, rootGreen, 0.4f, 5.8f, 0.75f);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * 45f;
                        Vector3 end = target + Quaternion.Euler(0f, 0f, angle) * Vector3.right * 4.2f;
                        SpawnMovingSlash(target, end, angle, rootGreen, 0.42f, 0.62f);
                    }
                    yield return new WaitForSeconds(0.22f);
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 podPosition = target + (Vector3)(UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.8f, 3.2f));
                        SpawnPodBurst(podPosition, i == 5 ? gold : rootGreen, i == 5 ? 2.2f : 1.1f);
                        yield return new WaitForSeconds(0.09f);
                    }
                    break;

                case 3:
                    LastVisualSequence = "왕실 꼬투리 천개";
                    SpawnPodPortal(target + Vector3.up * 2.6f, gold, 5.4f, 1.25f);
                    yield return new WaitForSeconds(0.25f);
                    for (int i = 0; i < 24; i++)
                    {
                        float angle = Mathf.Lerp(-78f, 78f, i / 23f);
                        Vector3 start = target + Vector3.up * 2.3f + Quaternion.Euler(0f, 0f, angle) * Vector3.up * 1.2f;
                        Vector3 end = target + Quaternion.Euler(0f, 0f, angle - 90f) * Vector3.right * 4.8f;
                        SpawnMovingSlash(start, end, angle - 90f, paleGold, 0.62f, 0.52f);
                    }
                    yield return new WaitForSeconds(0.44f);
                    SpawnGiantSword(target + Vector3.up * 5.4f, target, gold, 2.8f, 0.62f);
                    SpawnRing(target, gold, 0.7f, 9.2f, 0.78f);
                    break;

                case 4:
                    LastVisualSequence = "갑각해방";
                    SpawnRing(player, paleGold, 0.5f, 3.4f, 0.52f);
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * 60f;
                        Vector3 orbit = player + Quaternion.Euler(0f, 0f, angle) * Vector3.up * 1.7f;
                        SpawnOrbitingSlash(orbit, target, angle, gold, 0.9f, 2.3f, 0.8f);
                    }
                    yield return new WaitForSeconds(0.38f);
                    SpawnPolygonRing(target, gold, 6, 2.8f, 0.72f);
                    break;

                case 5:
                    LastVisualSequence = "땅콩 연환검";
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * 45f;
                        Vector3 start = target + Quaternion.Euler(0f, 0f, angle) * Vector3.right * 2.2f;
                        SpawnMovingSlash(start, target, angle + 135f, i % 2 == 0 ? gold : crimson, 1.05f, 0.20f);
                        yield return new WaitForSeconds(0.055f);
                    }
                    yield return new WaitForSeconds(0.18f);
                    SpawnRing(target, paleGold, 0.3f, 4.8f, 0.38f);
                    for (int i = 0; i < 8; i++)
                        SpawnSlash(target, i * 45f, crimson, 1.5f, 0.36f);
                    break;

                case 6:
                    LastVisualSequence = "낙화귀근";
                    SpawnFlower(target + Vector3.up * 3f, gold, 2.4f, 1.2f);
                    SpawnRootCrown(target + Vector3.down * 2.4f, rootGreen, 2.6f, 1.2f);
                    yield return new WaitForSeconds(0.45f);
                    for (int i = 0; i < 10; i++)
                    {
                        float x = Mathf.Lerp(-2.8f, 2.8f, i / 9f);
                        SpawnMovingSlash(target + new Vector3(x, 3.4f, 0f), target + new Vector3(-x * 0.25f, 0f, 0f), -90f, gold, 0.52f, 0.38f);
                        SpawnMovingSlash(target + new Vector3(-x, -3.0f, 0f), target + new Vector3(x * 0.25f, 0f, 0f), 90f, rootGreen, 0.52f, 0.38f);
                    }
                    yield return new WaitForSeconds(0.42f);
                    SpawnRing(target, paleGold, 5.5f, 0.2f, 0.55f);
                    break;

                case 7:
                    LastVisualSequence = "황금핵 천단";
                    Vector3 core = player + Vector3.up * 2.4f;
                    SpawnCore(core, gold, 0.5f, 4.2f, 1.05f);
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = i * 30f;
                        Vector3 start = core + Quaternion.Euler(0f, 0f, angle) * Vector3.right * 4.8f;
                        SpawnMovingSlash(start, core, angle + 180f, paleGold, 0.40f, 0.72f);
                    }
                    yield return new WaitForSeconds(0.76f);
                    SpawnGiantSword(target + Vector3.up * 7.0f, target + Vector3.down * 1.5f, paleGold, 4.4f, 0.80f);
                    yield return new WaitForSeconds(0.28f);
                    SpawnRing(target, gold, 0.8f, 12f, 1.0f);
                    SpawnSlash(target, -90f, Color.white, 5.2f, 0.72f);
                    break;
            }
        }

        private void CreateSprites()
        {
            ringSprite = CreateRingSprite(128);
            slashSprite = CreateSlashSprite(128, 28);
            petalSprite = CreatePetalSprite(72, 96);
            podSprite = CreatePodSprite(112, 82);
            coreSprite = CreateCoreSprite(128);
        }

        private void SpawnRing(Vector3 position, Color color, float from, float to, float duration)
        {
            SpriteRenderer renderer = CreateRenderer("Skill Ring", ringSprite, color, 28);
            renderer.transform.position = position;
            StartCoroutine(ScaleFade(renderer, from, to, duration, 60f));
        }

        private void SpawnSlash(Vector3 position, float angle, Color color, float scale, float duration)
        {
            SpriteRenderer renderer = CreateRenderer("Skill Slash", slashSprite, color, 30);
            renderer.transform.position = position;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            StartCoroutine(ScaleFade(renderer, scale * 0.45f, scale * 1.45f, duration, 12f));
        }

        private void SpawnMovingSlash(Vector3 start, Vector3 end, float angle, Color color, float scale, float duration)
        {
            SpriteRenderer renderer = CreateRenderer("Moving Sword", slashSprite, color, 31);
            renderer.transform.position = start;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            StartCoroutine(MoveScaleFade(renderer, start, end, scale * 0.55f, scale * 1.35f, duration));
        }

        private void SpawnOrbitingSlash(Vector3 start, Vector3 end, float angle, Color color, float scale, float arc, float duration)
        {
            SpriteRenderer renderer = CreateRenderer("Orbiting Carapace Sword", slashSprite, color, 32);
            renderer.transform.position = start;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            StartCoroutine(ArcMoveFade(renderer, start, end, arc, scale, duration));
        }

        private void SpawnFlower(Vector3 position, Color color, float scale, float duration)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                SpriteRenderer petal = CreateRenderer("Golden Flower Petal", petalSprite, color, 27);
                petal.transform.position = position + Quaternion.Euler(0f, 0f, angle) * Vector3.up * scale * 0.42f;
                petal.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                StartCoroutine(ScaleFade(petal, 0.15f, scale, duration, 18f));
            }
            SpawnRing(position, color, 0.2f, scale * 1.7f, duration);
        }

        private void SpawnPodBurst(Vector3 position, Color color, float scale)
        {
            SpriteRenderer pod = CreateRenderer("Leyline Pod", podSprite, color, 29);
            pod.transform.position = position;
            StartCoroutine(ScaleFade(pod, 0.2f, scale, 0.52f, 0f));
            for (int i = 0; i < 5; i++) SpawnSlash(position, i * 72f, color, scale * 0.65f, 0.42f);
        }

        private void SpawnPodPortal(Vector3 position, Color color, float scale, float duration)
        {
            SpriteRenderer pod = CreateRenderer("Royal Pod Armory", podSprite, color, 26);
            pod.transform.position = position;
            pod.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            StartCoroutine(ScaleFade(pod, 0.3f, scale, duration, 16f));
            SpawnRing(position, color, 0.4f, scale * 1.45f, duration);
        }

        private void SpawnPolygonRing(Vector3 center, Color color, int sides, float radius, float duration)
        {
            for (int i = 0; i < sides; i++)
            {
                float angle = i * 360f / sides;
                Vector3 point = center + Quaternion.Euler(0f, 0f, angle) * Vector3.right * radius;
                SpawnMovingSlash(point, center, angle + 180f, color, 0.9f, duration);
            }
        }

        private void SpawnRootCrown(Vector3 position, Color color, float scale, float duration)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.Lerp(-70f, 70f, i / 9f);
                SpriteRenderer root = CreateRenderer("Root Sword", slashSprite, color, 27);
                root.transform.position = position;
                root.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
                StartCoroutine(ScaleFade(root, 0.1f, scale, duration, -12f));
            }
        }

        private void SpawnCore(Vector3 position, Color color, float from, float to, float duration)
        {
            SpriteRenderer core = CreateRenderer("Golden Life Core", coreSprite, color, 35);
            core.transform.position = position;
            StartCoroutine(ScaleFade(core, from, to, duration, 120f));
            SpawnRing(position, Color.white, 0.4f, to * 1.4f, duration);
        }

        private void SpawnGiantSword(Vector3 start, Vector3 end, Color color, float scale, float duration)
        {
            SpriteRenderer sword = CreateRenderer("Giant Heavenly Sword", slashSprite, color, 38);
            sword.transform.position = start;
            sword.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            StartCoroutine(MoveScaleFade(sword, start, end, scale * 0.65f, scale * 1.35f, duration));
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
                color.a = 1f - t * 0.86f;
                renderer.color = color;
                yield return null;
            }
            if (renderer != null) Destroy(renderer.gameObject);
        }

        private IEnumerator ArcMoveFade(SpriteRenderer renderer, Vector3 start, Vector3 end, float arc, float scale, float duration)
        {
            float elapsed = 0f;
            while (renderer != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 position = Vector3.Lerp(start, end, t);
                position.y += Mathf.Sin(t * Mathf.PI) * arc;
                renderer.transform.position = position;
                renderer.transform.Rotate(0f, 0f, 360f * Time.deltaTime);
                renderer.transform.localScale = Vector3.one * Mathf.Lerp(scale * 0.5f, scale * 1.4f, t);
                Color color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
                yield return null;
            }
            if (renderer != null) Destroy(renderer.gameObject);
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
            if (stageFlow.Phase == StageFlowPhase.BossBattle)
            {
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
            Texture2D texture = TransparentTexture(size, size, "SpectacleRing");
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.44f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / 5f);
                    if (alpha > 0f) texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return FinalizeSprite(texture, new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateSlashSprite(int width, int height)
        {
            Texture2D texture = TransparentTexture(width, height, "SpectacleSlash");
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float vertical = 1f - Mathf.Abs((y / (height - 1f)) * 2f - 1f);
                    float horizontal = Mathf.Sin((x / (width - 1f)) * Mathf.PI);
                    float alpha = Mathf.Pow(Mathf.Clamp01(vertical * horizontal), 1.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return FinalizeSprite(texture, new Vector2(0.5f, 0.5f), 48f);
        }

        private static Sprite CreatePetalSprite(int width, int height)
        {
            Texture2D texture = TransparentTexture(width, height, "GoldenPetal");
            Vector2 center = new Vector2(width * 0.5f, height * 0.42f);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Vector2 d = new Vector2((x - center.x) / (width * 0.38f), (y - center.y) / (height * 0.48f));
                    float shape = d.x * d.x + d.y * d.y;
                    float alpha = shape <= 1f ? Mathf.Clamp01(1f - shape) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            return FinalizeSprite(texture, new Vector2(0.5f, 0.12f), 64f);
        }

        private static Sprite CreatePodSprite(int width, int height)
        {
            Texture2D texture = TransparentTexture(width, height, "RoyalPod");
            Vector2 left = new Vector2(width * 0.34f, height * 0.5f);
            Vector2 right = new Vector2(width * 0.66f, height * 0.5f);
            float radius = height * 0.36f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float distance = Mathf.Min(Vector2.Distance(new Vector2(x, y), left), Vector2.Distance(new Vector2(x, y), right));
                    float alpha = distance <= radius ? Mathf.Clamp01(1f - distance / radius) : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            return FinalizeSprite(texture, new Vector2(0.5f, 0.5f), 72f);
        }

        private static Sprite CreateCoreSprite(int size)
        {
            Texture2D texture = TransparentTexture(size, size, "GoldenCore");
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.46f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float normalized = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = normalized <= 1f ? Mathf.Pow(1f - normalized, 0.45f) : 0f;
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
