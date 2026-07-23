using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Displays the eight illustrated skill effects supplied by
    /// ProceduralBattleArtPrototype. Pooled SpriteRenderers are reused to avoid
    /// allocations during automatic combat.
    /// </summary>
    [DefaultExecutionOrder(26000)]
    public sealed class CombatEffectWorldViewPrototype : MonoBehaviour
    {
        private sealed class EffectView
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public bool IsRingPool;
        }

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float SourceLeft = 55f;
        private const float SourceRight = 705f;
        private const float SourceTop = 155f;
        private const float SourceBottom = 425f;
        private const float WorldHalfWidth = 8.2f;
        private const float WorldHalfHeight = 4.2f;
        private const int RingPrewarmCount = 10;
        private const int SlashPrewarmCount = 20;

        private readonly Queue<EffectView> ringPool = new Queue<EffectView>();
        private readonly Queue<EffectView> slashPool = new Queue<EffectView>();
        private readonly float[] previousSkillCooldowns = new float[8];

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private RuntimeWorldViewPrototype worldView;
        private ProceduralBattleArtPrototype rasterArt;
        private FieldInfo playerPositionField;
        private FieldInfo attackCooldownField;
        private FieldInfo skillCooldownsField;
        private FieldInfo worldRootField;
        private Transform effectRoot;
        private float previousAttackCooldown;
        private int activeEffectCount;

        public int ActiveEffectCount => activeEffectCount;
        public int AvailableRingCount => ringPool.Count;
        public int AvailableSlashCount => slashPool.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<CombatEffectWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("Peanut Illustrated Combat Effects");
            DontDestroyOnLoad(root);
            root.AddComponent<CombatEffectWorldViewPrototype>();
        }

        private IEnumerator Start()
        {
            yield return null;
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            rasterArt = FindFirstObjectByType<ProceduralBattleArtPrototype>();

            if (arena == null || stageFlow == null || worldView == null || rasterArt == null)
            {
                Debug.LogError("[PeanutEffects] Required combat or raster-art system is missing.");
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
                Debug.LogError("[PeanutEffects] Raster effect sprites were not initialized.");
                enabled = false;
                yield break;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            attackCooldownField = arenaType.GetField("playerAttackCooldown", PrivateInstance);
            skillCooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject rootObject = new GameObject("Illustrated Combat Effects");
            rootObject.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            effectRoot = rootObject.transform;
            PrewarmPools();

            previousAttackCooldown = ReadAttackCooldown();
            float[] cooldowns = ReadCooldowns();
            if (cooldowns != null)
                Array.Copy(cooldowns, previousSkillCooldowns, Mathf.Min(cooldowns.Length, previousSkillCooldowns.Length));

            Debug.Log("[PeanutEffects] Eight illustrated combat effects connected.");
        }

        private void PrewarmPools()
        {
            for (int index = 0; index < RingPrewarmCount; index++)
                ringPool.Enqueue(CreateEffectView(true));
            for (int index = 0; index < SlashPrewarmCount; index++)
                slashPool.Enqueue(CreateEffectView(false));
        }

        private EffectView CreateEffectView(bool ringPoolView)
        {
            GameObject effect = new GameObject(ringPoolView ? "Pooled Illustrated Burst" : "Pooled Illustrated Slash");
            effect.transform.SetParent(effectRoot, false);
            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = rasterArt.GetEffectSprite(ringPoolView ? 4 : 0);
            renderer.color = Color.white;
            renderer.sortingOrder = ringPoolView ? 17 : 18;
            effect.SetActive(false);
            return new EffectView { Root = effect, Renderer = renderer, IsRingPool = ringPoolView };
        }

        private EffectView Acquire(bool ringPoolView, int effectIndex)
        {
            Queue<EffectView> pool = ringPoolView ? ringPool : slashPool;
            EffectView view = pool.Count > 0 ? pool.Dequeue() : CreateEffectView(ringPoolView);
            view.Renderer.sprite = rasterArt.GetEffectSprite(effectIndex);
            view.Renderer.color = Color.white;
            view.Root.SetActive(true);
            activeEffectCount++;
            return view;
        }

        private void Release(EffectView view)
        {
            if (view == null || view.Root == null) return;
            view.Root.SetActive(false);
            view.Root.transform.SetParent(effectRoot, false);
            view.Root.transform.localPosition = Vector3.zero;
            view.Root.transform.localRotation = Quaternion.identity;
            view.Root.transform.localScale = Vector3.one;
            activeEffectCount = Mathf.Max(0, activeEffectCount - 1);
            if (view.IsRingPool) ringPool.Enqueue(view);
            else slashPool.Enqueue(view);
        }

        private void Update()
        {
            if (effectRoot == null || rasterArt == null || !rasterArt.ArtReady) return;
            DetectBasicAttack();
            DetectSkills();
        }

        private void DetectBasicAttack()
        {
            float current = ReadAttackCooldown();
            bool started = current > previousAttackCooldown + 0.18f;
            previousAttackCooldown = current;
            if (!started) return;

            Spawn(
                false,
                0,
                PlayerWorldPosition + new Vector3(0.45f, 0.08f, 0f),
                0.8f,
                2.0f,
                0.24f,
                UnityEngine.Random.Range(-24f, 24f),
                22f);
        }

        private void DetectSkills()
        {
            float[] cooldowns = ReadCooldowns();
            if (cooldowns == null || cooldowns.Length < 8) return;
            int activeStart = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            for (int index = 0; index < cooldowns.Length && index < previousSkillCooldowns.Length; index++)
            {
                bool cast = index >= activeStart && index < activeStart + 4 &&
                    cooldowns[index] > previousSkillCooldowns[index] + 1f;
                previousSkillCooldowns[index] = cooldowns[index];
                if (cast) SpawnSkillEffect(index);
            }
        }

        private void SpawnSkillEffect(int skillIndex)
        {
            Vector3 origin = PlayerWorldPosition;
            switch (skillIndex)
            {
                case 0:
                    Spawn(false, 0, origin + Vector3.right * 0.5f, 1.1f, 3.3f, 0.42f, -12f, 120f);
                    break;
                case 1:
                    for (int index = 0; index < 4; index++)
                    {
                        Vector3 offset = new Vector3((index - 1.5f) * 0.65f, Mathf.Abs(index - 1.5f) * 0.18f, 0f);
                        Spawn(false, 1, origin + offset, 0.8f, 2.1f, 0.34f + index * 0.025f, -42f + index * 28f, 35f);
                    }
                    break;
                case 2:
                    Spawn(true, 2, origin, 0.9f, 4.2f, 0.5f, 0f, 8f);
                    break;
                case 3:
                    for (int index = 0; index < 7; index++)
                    {
                        Vector3 rainPosition = origin + new Vector3((index - 3f) * 0.65f, 1.5f - Mathf.Abs(index - 3f) * 0.12f, 0f);
                        Spawn(false, 3, rainPosition, 0.7f, 1.75f, 0.46f, -10f + index * 3f, 0f);
                    }
                    break;
                case 4:
                    Spawn(true, 4, origin, 1.0f, 5.2f, 0.62f, 0f, 80f);
                    break;
                case 5:
                    for (int index = 0; index < 6; index++)
                    {
                        Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * 1.3f);
                        Spawn(false, 5, origin + offset, 0.85f, 2.35f, 0.38f, index * 31f, 80f);
                    }
                    break;
                case 6:
                    Spawn(true, 6, origin, 0.8f, 4.4f, 0.68f, 0f, -35f);
                    StartCoroutine(SpawnEchoes(6, origin));
                    break;
                default:
                    Spawn(true, 7, Vector3.zero, 1.7f, 12.5f, 0.86f, 0f, 130f);
                    break;
            }
        }

        private IEnumerator SpawnEchoes(int effectIndex, Vector3 origin)
        {
            for (int index = 0; index < 3; index++)
            {
                yield return new WaitForSeconds(0.09f);
                Spawn(true, effectIndex, origin, 1.1f + index * 0.45f, 3.2f + index * 0.5f, 0.44f, 0f, index % 2 == 0 ? 45f : -45f);
            }
        }

        private void Spawn(
            bool ringPoolView,
            int effectIndex,
            Vector3 position,
            float startScale,
            float endScale,
            float duration,
            float angle,
            float rotationSpeed)
        {
            EffectView view = Acquire(ringPoolView, effectIndex);
            view.Root.transform.position = position;
            view.Root.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            StartCoroutine(AnimateEffect(view, startScale, endScale, duration, rotationSpeed));
        }

        private IEnumerator AnimateEffect(
            EffectView view,
            float from,
            float to,
            float duration,
            float rotationSpeed)
        {
            float elapsed = 0f;
            while (elapsed < duration && view != null && view.Root != null && view.Root.activeSelf)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                view.Root.transform.localScale = Vector3.one * Mathf.Lerp(from, to, eased);
                view.Root.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
                Color color = Color.white;
                color.a = Mathf.Clamp01(1f - t * t);
                view.Renderer.color = color;
                yield return null;
            }
            Release(view);
        }

        private float ReadAttackCooldown()
        {
            return attackCooldownField == null ? 0f : Convert.ToSingle(attackCooldownField.GetValue(arena));
        }

        private float[] ReadCooldowns()
        {
            return skillCooldownsField?.GetValue(arena) as float[];
        }

        private Vector3 PlayerWorldPosition
        {
            get
            {
                Vector2 source = playerPositionField == null
                    ? new Vector2(380f, 280f)
                    : (Vector2)playerPositionField.GetValue(arena);
                float x = Mathf.Lerp(-WorldHalfWidth, WorldHalfWidth, Mathf.InverseLerp(SourceLeft, SourceRight, source.x));
                float y = Mathf.Lerp(WorldHalfHeight, -WorldHalfHeight, Mathf.InverseLerp(SourceTop, SourceBottom, source.y));
                return new Vector3(x, y, 0f);
            }
        }
    }
}
