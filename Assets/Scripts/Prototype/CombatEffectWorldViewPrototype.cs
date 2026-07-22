using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Procedural placeholder effects for basic attacks and the eight skills.
    /// These effects can later be replaced by authored sprites and particles.
    /// </summary>
    [DefaultExecutionOrder(17500)]
    public sealed class CombatEffectWorldViewPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
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
        private FieldInfo attackCooldownField;
        private FieldInfo skillCooldownsField;
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private FieldInfo worldRootField;
        private float previousAttackCooldown;
        private readonly float[] previousSkillCooldowns = new float[8];
        private Sprite circleSprite;
        private Sprite slashSprite;
        private Transform effectRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<CombatEffectWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorCombatEffectWorldViewPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<CombatEffectWorldViewPrototype>();
        }

        private IEnumerator Start()
        {
            yield return null;
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (arena == null || stageFlow == null || worldView == null)
            {
                enabled = false;
                yield break;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            attackCooldownField = arenaType.GetField("playerAttackCooldown", PrivateInstance);
            skillCooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            huntingElementField = arenaType.GetField("huntingElement", PrivateInstance);
            bossElementField = arenaType.GetField("bossElement", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            CreateSprites();
            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject rootObject = new GameObject("Combat Effects");
            rootObject.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            effectRoot = rootObject.transform;
            previousAttackCooldown = ReadAttackCooldown();
            float[] cooldowns = ReadCooldowns();
            if (cooldowns != null)
                Array.Copy(cooldowns, previousSkillCooldowns, Mathf.Min(cooldowns.Length, previousSkillCooldowns.Length));
        }

        private void CreateSprites()
        {
            circleSprite = CreateCircleSprite(96);
            Texture2D slash = new Texture2D(96, 24, TextureFormat.RGBA32, false);
            slash.name = "ProceduralSlash";
            slash.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < slash.height; y++)
            {
                for (int x = 0; x < slash.width; x++)
                {
                    float vertical = 1f - Mathf.Abs((y / (slash.height - 1f)) * 2f - 1f);
                    float horizontal = Mathf.Sin((x / (slash.width - 1f)) * Mathf.PI);
                    float alpha = Mathf.Pow(Mathf.Clamp01(vertical * horizontal), 1.8f);
                    slash.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            slash.Apply();
            slashSprite = Sprite.Create(slash, new Rect(0f, 0f, slash.width, slash.height), new Vector2(0.5f, 0.5f), 48f);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ProceduralSkillCircle";
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.47f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalized = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = normalized <= 1f ? Mathf.Clamp01(1f - normalized) * 0.28f : 0f;
                    if (normalized > 0.78f && normalized <= 1f) alpha = Mathf.Max(alpha, 0.72f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void Update()
        {
            if (effectRoot == null) return;
            DetectBasicAttack();
            DetectSkills();
        }

        private void DetectBasicAttack()
        {
            float current = ReadAttackCooldown();
            bool started = current > previousAttackCooldown + 0.18f;
            previousAttackCooldown = current;
            if (!started) return;
            SpawnSlash(PlayerWorldPosition, ElementColor(ActiveElement), 1.2f, UnityEngine.Random.Range(-24f, 24f));
        }

        private void DetectSkills()
        {
            float[] cooldowns = ReadCooldowns();
            if (cooldowns == null || cooldowns.Length < 8) return;
            int activeStart = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            for (int i = 0; i < cooldowns.Length && i < previousSkillCooldowns.Length; i++)
            {
                bool cast = i >= activeStart && i < activeStart + 4 && cooldowns[i] > previousSkillCooldowns[i] + 1f;
                previousSkillCooldowns[i] = cooldowns[i];
                if (cast) SpawnSkillEffect(i % 4);
            }
        }

        private void SpawnSkillEffect(int localIndex)
        {
            Color color = ElementColor(ActiveElement);
            Vector3 position = PlayerWorldPosition;
            switch (localIndex)
            {
                case 0:
                    SpawnRing(position, color, 0.7f, 4.2f, 0.48f);
                    break;
                case 1:
                    for (int i = 0; i < 5; i++)
                        SpawnSlash(position + (Vector3)(UnityEngine.Random.insideUnitCircle * 1.1f), color, 1.3f, i * 34f - 68f);
                    break;
                case 2:
                    SpawnRing(position, color, 0.4f, 2.6f, 0.38f);
                    for (int i = 0; i < 3; i++)
                        SpawnSlash(position + new Vector3(i - 1f, 0.25f * i, 0f), color, 1.0f, 25f - i * 25f);
                    break;
                default:
                    SpawnRing(Vector3.zero, color, 1f, 12f, 0.65f);
                    break;
            }
        }

        private void SpawnRing(Vector3 position, Color color, float startScale, float endScale, float duration)
        {
            GameObject go = new GameObject("Skill Ring");
            go.transform.SetParent(effectRoot, false);
            go.transform.position = position;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.color = new Color(color.r, color.g, color.b, 0.78f);
            renderer.sortingOrder = 16;
            StartCoroutine(AnimateEffect(go, renderer, startScale, endScale, duration, 0f));
        }

        private void SpawnSlash(Vector3 position, Color color, float scale, float angle)
        {
            GameObject go = new GameObject("Skill Slash");
            go.transform.SetParent(effectRoot, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = slashSprite;
            renderer.color = new Color(color.r, color.g, color.b, 0.92f);
            renderer.sortingOrder = 17;
            StartCoroutine(AnimateEffect(go, renderer, scale * 0.45f, scale * 1.45f, 0.26f, 18f));
        }

        private IEnumerator AnimateEffect(GameObject target, SpriteRenderer renderer, float from, float to, float duration, float rotationSpeed)
        {
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(from, to, 1f - Mathf.Pow(1f - t, 2f));
                target.transform.localScale = Vector3.one * scale;
                target.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
                Color current = renderer.color;
                current.a = 1f - t;
                renderer.color = current;
                yield return null;
            }
            if (target != null) Destroy(target);
        }

        private float ReadAttackCooldown()
        {
            return attackCooldownField == null ? 0f : Convert.ToSingle(attackCooldownField.GetValue(arena));
        }

        private float[] ReadCooldowns()
        {
            return skillCooldownsField?.GetValue(arena) as float[];
        }

        private int ActiveElement
        {
            get
            {
                FieldInfo field = stageFlow.Phase == StageFlowPhase.BossBattle ? bossElementField : huntingElementField;
                return field == null ? 0 : Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
            }
        }

        private Vector3 PlayerWorldPosition
        {
            get
            {
                Vector2 source = playerPositionField == null ? new Vector2(380f, 280f) : (Vector2)playerPositionField.GetValue(arena);
                float x = Mathf.Lerp(-WorldHalfWidth, WorldHalfWidth, Mathf.InverseLerp(SourceLeft, SourceRight, source.x));
                float y = Mathf.Lerp(WorldHalfHeight, -WorldHalfHeight, Mathf.InverseLerp(SourceTop, SourceBottom, source.y));
                return new Vector3(x, y, 0f);
            }
        }

        private static Color ElementColor(int element)
        {
            return element switch
            {
                1 => new Color(1f, 0.26f, 0.08f),
                2 => new Color(0.30f, 0.82f, 1f),
                3 => new Color(0.76f, 0.46f, 1f),
                _ => new Color(1f, 0.92f, 0.45f)
            };
        }
    }
}
