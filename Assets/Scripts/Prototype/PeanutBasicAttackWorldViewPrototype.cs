using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(17550)]
    public sealed class PeanutBasicAttackWorldViewPrototype : MonoBehaviour
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
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private FieldInfo worldRootField;
        private Transform effectRoot;
        private Sprite slashSprite;
        private float previousCooldown;

        public bool PreservesBasicAttackEffects => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutBasicAttackWorldViewPrototype>() != null) return;
            GameObject go = new GameObject("PeanutWarriorBasicAttackWorldView");
            DontDestroyOnLoad(go);
            go.AddComponent<PeanutBasicAttackWorldViewPrototype>();
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

            Type arenaType = typeof(CombatPrototypeArena);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            attackCooldownField = arenaType.GetField("playerAttackCooldown", PrivateInstance);
            huntingElementField = arenaType.GetField("huntingElement", PrivateInstance);
            bossElementField = arenaType.GetField("bossElement", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            GameObject root = new GameObject("Basic Attack Effects");
            root.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            effectRoot = root.transform;
            slashSprite = CreateSlashSprite();
            previousCooldown = ReadCooldown();
        }

        private void Update()
        {
            if (effectRoot == null) return;
            float current = ReadCooldown();
            bool attackStarted = current > previousCooldown + 0.18f;
            previousCooldown = current;
            if (!attackStarted) return;

            GameObject go = new GameObject("Basic Sword Slash");
            go.transform.SetParent(effectRoot, false);
            go.transform.position = ToWorld(ReadPlayerPosition());
            go.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-28f, 28f));
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = slashSprite;
            renderer.color = ElementColor(ReadActiveElement());
            renderer.sortingOrder = 24;
            StartCoroutine(Animate(renderer));
        }

        private IEnumerator Animate(SpriteRenderer renderer)
        {
            float elapsed = 0f;
            const float duration = 0.24f;
            while (renderer != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                renderer.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.9f, t);
                Color color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
                yield return null;
            }
            if (renderer != null) Destroy(renderer.gameObject);
        }

        private float ReadCooldown()
        {
            return attackCooldownField == null ? 0f : Convert.ToSingle(attackCooldownField.GetValue(arena));
        }

        private Vector2 ReadPlayerPosition()
        {
            return playerPositionField == null ? new Vector2(380f, 280f) : (Vector2)playerPositionField.GetValue(arena);
        }

        private int ReadActiveElement()
        {
            FieldInfo field = stageFlow.Phase == StageFlowPhase.BossBattle ? bossElementField : huntingElementField;
            return field == null ? 0 : Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
        }

        private static Vector3 ToWorld(Vector2 source)
        {
            float x = Mathf.Lerp(-WorldHalfWidth, WorldHalfWidth, Mathf.InverseLerp(SourceLeft, SourceRight, source.x));
            float y = Mathf.Lerp(WorldHalfHeight, -WorldHalfHeight, Mathf.InverseLerp(SourceTop, SourceBottom, source.y));
            return new Vector3(x, y, 0f);
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

        private static Sprite CreateSlashSprite()
        {
            const int width = 128;
            const int height = 28;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "BasicPeanutSlash";
            texture.filterMode = FilterMode.Bilinear;
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
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 48f);
        }
    }
}
