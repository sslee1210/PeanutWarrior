using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Builds a zero-asset 2D scene view for the existing combat prototype.
    /// The combat model remains authoritative; this component mirrors it into
    /// real GameObjects with SpriteRenderer, Rigidbody2D, Collider2D, trails,
    /// hit flashes, shadows, health bars and floating damage text.
    /// </summary>
    public sealed class RuntimeWorldViewPrototype : MonoBehaviour
    {
        private sealed class UnitView
        {
            public GameObject Root;
            public SpriteRenderer Body;
            public SpriteRenderer Shadow;
            public Transform HealthFill;
            public TextMesh Label;
            public float LastHp;
            public float FlashTimer;
            public bool IsBoss;
        }

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const float SourceLeft = 55f;
        private const float SourceRight = 705f;
        private const float SourceTop = 155f;
        private const float SourceBottom = 425f;
        private const float WorldHalfWidth = 8.2f;
        private const float WorldHalfHeight = 4.2f;

        private readonly Dictionary<object, UnitView> enemyViews = new Dictionary<object, UnitView>();
        private readonly List<object> staleEnemies = new List<object>();

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo enemiesField;
        private FieldInfo playerPositionField;
        private FieldInfo playerHpField;
        private PropertyInfo playerMaxHpProperty;
        private GameObject worldRoot;
        private UnitView playerView;
        private Sprite unitSprite;
        private Sprite shadowSprite;
        private Camera worldCamera;
        private Vector3 previousPlayerPosition;
        private float shakeTimer;
        private float shakeStrength;
        private Vector3 cameraBasePosition;
        private bool worldVisible = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateView()
        {
            if (FindFirstObjectByType<RuntimeWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorRuntimeWorldView");
            DontDestroyOnLoad(root);
            root.AddComponent<RuntimeWorldViewPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            playerHpField = arenaType.GetField("playerHp", PrivateInstance);
            playerMaxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);

            CreateSprites();
            BuildWorld();
        }

        private void CreateSprites()
        {
            Texture2D unitTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            unitTexture.name = "RuntimeUnitTexture";
            unitTexture.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(15.5f, 15.5f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((15.5f - distance) * 1.8f);
                    unitTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            unitTexture.Apply();
            unitSprite = Sprite.Create(unitTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);

            Texture2D shadowTexture = new Texture2D(32, 16, TextureFormat.RGBA32, false);
            shadowTexture.name = "RuntimeShadowTexture";
            shadowTexture.filterMode = FilterMode.Bilinear;
            Vector2 shadowCenter = new Vector2(15.5f, 7.5f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Vector2 normalized = new Vector2((x - shadowCenter.x) / 15.5f, (y - shadowCenter.y) / 7.5f);
                    float alpha = Mathf.Clamp01((1f - normalized.sqrMagnitude) * 0.35f);
                    shadowTexture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }
            shadowTexture.Apply();
            shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, 32, 16), new Vector2(0.5f, 0.5f), 32f);
        }

        private void BuildWorld()
        {
            worldRoot = new GameObject("Runtime 2D World");
            worldRoot.transform.SetParent(transform, false);

            BuildCamera();
            BuildBackground();
            playerView = CreateUnitView("Peanut Warrior", false, new Color(0.86f, 0.58f, 0.18f), "땅콩전사");
            playerView.Root.transform.localScale = Vector3.one * 0.95f;

            Rigidbody2D body = playerView.Root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            CircleCollider2D collider = playerView.Root.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            TrailRenderer trail = playerView.Root.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.startWidth = 0.42f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.9f, 0.35f, 0.75f);
            trail.endColor = new Color(1f, 0.45f, 0.1f, 0f);
            trail.sortingOrder = 5;
        }

        private void BuildCamera()
        {
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                GameObject cameraObject = new GameObject("Runtime World Camera");
                worldCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 5.4f;
            worldCamera.backgroundColor = new Color(0.34f, 0.48f, 0.30f);
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
            cameraBasePosition = worldCamera.transform.position;
        }

        private void BuildBackground()
        {
            GameObject floor = new GameObject("Peanut Field");
            floor.transform.SetParent(worldRoot.transform, false);
            SpriteRenderer renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = unitSprite;
            renderer.color = new Color(0.48f, 0.62f, 0.31f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(WorldHalfWidth * 2.15f, WorldHalfHeight * 2.2f);
            renderer.sortingOrder = -20;

            for (int i = 0; i < 18; i++)
            {
                GameObject patch = new GameObject("Field Patch");
                patch.transform.SetParent(worldRoot.transform, false);
                patch.transform.position = new Vector3(
                    UnityEngine.Random.Range(-WorldHalfWidth, WorldHalfWidth),
                    UnityEngine.Random.Range(-WorldHalfHeight, WorldHalfHeight),
                    0f);
                patch.transform.localScale = new Vector3(
                    UnityEngine.Random.Range(0.2f, 0.55f),
                    UnityEngine.Random.Range(0.08f, 0.2f),
                    1f);
                SpriteRenderer patchRenderer = patch.AddComponent<SpriteRenderer>();
                patchRenderer.sprite = unitSprite;
                patchRenderer.color = new Color(0.66f, 0.76f, 0.34f, 0.55f);
                patchRenderer.sortingOrder = -19;
            }
        }

        private UnitView CreateUnitView(string name, bool boss, Color color, string labelText)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(worldRoot.transform, false);

            GameObject shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(root.transform, false);
            shadowObject.transform.localPosition = new Vector3(0f, -0.36f, 0f);
            SpriteRenderer shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = shadowSprite;
            shadow.sortingOrder = 0;

            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(root.transform, false);
            SpriteRenderer body = bodyObject.AddComponent<SpriteRenderer>();
            body.sprite = unitSprite;
            body.color = color;
            body.sortingOrder = 2;

            GameObject highlight = new GameObject("Highlight");
            highlight.transform.SetParent(bodyObject.transform, false);
            highlight.transform.localPosition = new Vector3(-0.16f, 0.18f, 0f);
            highlight.transform.localScale = Vector3.one * 0.28f;
            SpriteRenderer highlightRenderer = highlight.AddComponent<SpriteRenderer>();
            highlightRenderer.sprite = unitSprite;
            highlightRenderer.color = new Color(1f, 1f, 1f, 0.42f);
            highlightRenderer.sortingOrder = 3;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, boss ? 0.9f : 0.72f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = labelText;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.09f;
            label.fontSize = 42;
            label.color = Color.white;
            MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
            labelRenderer.sortingOrder = 10;

            GameObject healthBack = new GameObject("Health Back");
            healthBack.transform.SetParent(root.transform, false);
            healthBack.transform.localPosition = new Vector3(0f, boss ? 0.66f : 0.52f, 0f);
            healthBack.transform.localScale = new Vector3(boss ? 1.45f : 0.86f, 0.1f, 1f);
            SpriteRenderer healthBackRenderer = healthBack.AddComponent<SpriteRenderer>();
            healthBackRenderer.sprite = unitSprite;
            healthBackRenderer.color = new Color(0.12f, 0.08f, 0.08f, 0.95f);
            healthBackRenderer.sortingOrder = 7;

            GameObject healthFill = new GameObject("Health Fill");
            healthFill.transform.SetParent(healthBack.transform, false);
            healthFill.transform.localPosition = new Vector3(-0.48f, 0f, 0f);
            SpriteRenderer healthFillRenderer = healthFill.AddComponent<SpriteRenderer>();
            healthFillRenderer.sprite = unitSprite;
            healthFillRenderer.color = boss
                ? new Color(0.9f, 0.18f, 0.12f)
                : new Color(0.25f, 0.9f, 0.3f);
            healthFillRenderer.sortingOrder = 8;

            return new UnitView
            {
                Root = root,
                Body = body,
                Shadow = shadow,
                HealthFill = healthFill.transform,
                Label = label,
                LastHp = -1f,
                IsBoss = boss
            };
        }

        private void LateUpdate()
        {
            if (arena == null || !worldVisible) return;
            UpdatePlayerView();
            UpdateEnemyViews();
            UpdateCameraEffects();
        }

        private void UpdatePlayerView()
        {
            if (playerPositionField == null || playerView == null) return;
            Vector2 source = (Vector2)playerPositionField.GetValue(arena);
            Vector3 target = SourceToWorld(source);
            float speed = Vector3.Distance(previousPlayerPosition, target) / Mathf.Max(Time.deltaTime, 0.0001f);
            playerView.Root.transform.position = Vector3.Lerp(
                playerView.Root.transform.position,
                target,
                1f - Mathf.Exp(-26f * Time.deltaTime));
            playerView.Root.transform.localScale = Vector3.one * (0.95f + Mathf.Sin(Time.time * 8f) * 0.025f);
            playerView.Body.flipX = target.x < previousPlayerPosition.x;
            previousPlayerPosition = target;

            float hp = playerHpField != null ? Convert.ToSingle(playerHpField.GetValue(arena)) : 1f;
            float maxHp = playerMaxHpProperty != null ? Convert.ToSingle(playerMaxHpProperty.GetValue(arena)) : 1f;
            SetHealth(playerView, hp, maxHp);

            TrailRenderer trail = playerView.Root.GetComponent<TrailRenderer>();
            if (trail != null) trail.emitting = speed > 15f;
        }

        private void UpdateEnemyViews()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null) return;

            staleEnemies.Clear();
            foreach (object key in enemyViews.Keys) staleEnemies.Add(key);

            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                staleEnemies.Remove(enemy);

                Type enemyType = enemy.GetType();
                bool isBoss = ReadBool(enemyType, enemy, "IsBoss");
                if (!enemyViews.TryGetValue(enemy, out UnitView view))
                {
                    Color color = isBoss ? new Color(0.72f, 0.12f, 0.13f) : RandomEnemyColor(i);
                    view = CreateUnitView(
                        isBoss ? "Boss View" : "Monster View",
                        isBoss,
                        color,
                        isBoss ? "침공 보스" : MonsterName(i));
                    view.Root.transform.localScale = Vector3.one *
                        (isBoss ? 1.35f : UnityEngine.Random.Range(0.7f, 0.92f));
                    Rigidbody2D body = view.Root.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.gravityScale = 0f;
                    CircleCollider2D collider = view.Root.AddComponent<CircleCollider2D>();
                    collider.radius = isBoss ? 0.55f : 0.42f;
                    enemyViews.Add(enemy, view);
                }

                Vector2 sourcePosition = ReadVector2(enemyType, enemy, "Position");
                Vector3 targetPosition = SourceToWorld(sourcePosition);
                view.Root.transform.position = Vector3.Lerp(
                    view.Root.transform.position,
                    targetPosition,
                    1f - Mathf.Exp(-18f * Time.deltaTime));

                float hp = ReadFloat(enemyType, enemy, "Hp");
                float maxHp = ReadFloat(enemyType, enemy, "MaxHp");
                if (view.LastHp >= 0f && hp < view.LastHp - 0.01f)
                {
                    float damage = view.LastHp - hp;
                    SpawnDamageText(view.Root.transform.position, damage, damage > maxHp * 0.3f);
                    view.FlashTimer = 0.09f;
                    shakeTimer = isBoss ? 0.11f : 0.055f;
                    shakeStrength = isBoss ? 0.1f : 0.045f;
                }
                view.LastHp = hp;
                SetHealth(view, hp, maxHp);

                view.FlashTimer -= Time.deltaTime;
                if (view.FlashTimer > 0f)
                    view.Body.color = Color.white;
                else
                    view.Body.color = StatusColor(enemyType, enemy, view.IsBoss, i);

                float pulse = 1f + Mathf.Sin(Time.time * (isBoss ? 3.5f : 5.5f) + i) *
                    (isBoss ? 0.035f : 0.02f);
                float baseScale = isBoss ? 1.35f : 0.82f;
                view.Root.transform.localScale = Vector3.one * baseScale * pulse;
            }

            for (int i = 0; i < staleEnemies.Count; i++)
            {
                object key = staleEnemies[i];
                if (!enemyViews.TryGetValue(key, out UnitView view)) continue;
                SpawnDefeatEffect(view.Root.transform.position, view.Body.color, view.IsBoss);
                Destroy(view.Root);
                enemyViews.Remove(key);
            }
        }

        private void SetHealth(UnitView view, float hp, float maxHp)
        {
            if (view?.HealthFill == null) return;
            float ratio = maxHp <= 0f ? 0f : Mathf.Clamp01(hp / maxHp);
            view.HealthFill.localScale = new Vector3(ratio, 0.82f, 1f);
            view.HealthFill.localPosition = new Vector3(-0.5f + ratio * 0.5f, 0f, 0f);
        }

        private void SpawnDamageText(Vector3 position, float damage, bool critical)
        {
            GameObject textObject = new GameObject("Damage Number");
            textObject.transform.position = position +
                new Vector3(UnityEngine.Random.Range(-0.18f, 0.18f), 0.65f, 0f);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = critical
                ? $"치명! {Mathf.CeilToInt(damage)}"
                : Mathf.CeilToInt(damage).ToString();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = critical ? 54 : 42;
            text.characterSize = 0.085f;
            text.color = critical ? new Color(1f, 0.82f, 0.12f) : Color.white;
            text.GetComponent<MeshRenderer>().sortingOrder = 30;
            StartCoroutine(AnimateDamageText(textObject, text));
        }

        private IEnumerator AnimateDamageText(GameObject target, TextMesh text)
        {
            float elapsed = 0f;
            Vector3 origin = target.transform.position;
            while (elapsed < 0.55f && target != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.55f;
                target.transform.position = origin + new Vector3(0f, t * 0.85f, 0f);
                Color color = text.color;
                color.a = 1f - t;
                text.color = color;
                yield return null;
            }
            if (target != null) Destroy(target);
        }

        private void SpawnDefeatEffect(Vector3 position, Color color, bool boss)
        {
            int count = boss ? 14 : 6;
            for (int i = 0; i < count; i++)
            {
                GameObject particle = new GameObject("Defeat Particle");
                particle.transform.position = position;
                particle.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.08f, 0.18f);
                SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
                renderer.sprite = unitSprite;
                renderer.color = color;
                renderer.sortingOrder = 20;
                StartCoroutine(AnimateParticle(
                    particle,
                    UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(1.2f, 3.4f)));
            }
        }

        private IEnumerator AnimateParticle(GameObject particle, Vector2 velocity)
        {
            float elapsed = 0f;
            SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
            while (elapsed < 0.5f && particle != null)
            {
                elapsed += Time.deltaTime;
                velocity += Vector2.down * 2.8f * Time.deltaTime;
                particle.transform.position += (Vector3)(velocity * Time.deltaTime);
                Color color = renderer.color;
                color.a = 1f - elapsed / 0.5f;
                renderer.color = color;
                yield return null;
            }
            if (particle != null) Destroy(particle);
        }

        private void UpdateCameraEffects()
        {
            if (worldCamera == null) return;
            shakeTimer -= Time.deltaTime;
            if (shakeTimer > 0f)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * shakeStrength;
                worldCamera.transform.position = cameraBasePosition + new Vector3(offset.x, offset.y, 0f);
            }
            else
            {
                worldCamera.transform.position = Vector3.Lerp(
                    worldCamera.transform.position,
                    cameraBasePosition,
                    1f - Mathf.Exp(-20f * Time.deltaTime));
            }
        }

        private static Vector3 SourceToWorld(Vector2 source)
        {
            float x = Mathf.Lerp(
                -WorldHalfWidth,
                WorldHalfWidth,
                Mathf.InverseLerp(SourceLeft, SourceRight, source.x));
            float y = Mathf.Lerp(
                WorldHalfHeight,
                -WorldHalfHeight,
                Mathf.InverseLerp(SourceTop, SourceBottom, source.y));
            return new Vector3(x, y, 0f);
        }

        private static Vector2 ReadVector2(Type type, object instance, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, PublicInstance);
            return field == null ? Vector2.zero : (Vector2)field.GetValue(instance);
        }

        private static float ReadFloat(Type type, object instance, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, PublicInstance);
            return field == null ? 0f : Convert.ToSingle(field.GetValue(instance));
        }

        private static bool ReadBool(Type type, object instance, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, PublicInstance);
            return field != null && (bool)field.GetValue(instance);
        }

        private static Color StatusColor(Type type, object enemy, bool boss, int index)
        {
            if (ReadFloat(type, enemy, "BurnTimer") > 0f)
                return new Color(1f, 0.3f, 0.08f);
            if (ReadFloat(type, enemy, "FrostTimer") > 0f)
                return new Color(0.3f, 0.75f, 1f);
            return boss ? new Color(0.72f, 0.12f, 0.13f) : RandomEnemyColor(index);
        }

        private static Color RandomEnemyColor(int index)
        {
            Color[] palette =
            {
                new Color(0.38f, 0.7f, 0.24f),
                new Color(0.62f, 0.28f, 0.68f),
                new Color(0.76f, 0.5f, 0.18f),
                new Color(0.25f, 0.62f, 0.58f),
                new Color(0.68f, 0.24f, 0.32f)
            };
            return palette[Mathf.Abs(index) % palette.Length];
        }

        private static string MonsterName(int index)
        {
            string[] names = { "곰팡이", "바구미", "포식벌레", "균사체", "침공충" };
            return names[Mathf.Abs(index) % names.Length];
        }

        private void OnGUI()
        {
            // The old debug toggle overlapped the new player-status panel and could
            // invisibly receive clicks, hiding the entire battlefield. It is only
            // available when the consolidated mobile UI is not running.
            if (FindFirstObjectByType<MobileIdleUiPrototype>() != null) return;

            Rect toggle = new Rect(15f, 15f, 165f, 34f);
            if (GUI.Button(toggle, worldVisible ? "2D 월드 숨기기" : "2D 월드 표시"))
            {
                worldVisible = !worldVisible;
                if (worldRoot != null) worldRoot.SetActive(worldVisible);
            }
        }
    }
}
