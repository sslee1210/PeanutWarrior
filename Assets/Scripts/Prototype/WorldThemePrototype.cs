using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Applies a distinct visual identity to each authored world without requiring
    /// external art assets. It updates the runtime field, monster labels and boss
    /// names while preserving the existing combat model.
    /// </summary>
    [DefaultExecutionOrder(17000)]
    public sealed class WorldThemePrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private static readonly string[][] MonsterNames =
        {
            new[] { "싹곰팡이", "땅콩바구미", "껍질포식충", "잡초균" },
            new[] { "창고곰팡이", "포자주머니", "균사덩어리", "부패벌레" },
            new[] { "숲쥐", "껍질사냥꾼", "덩굴포식충", "송곳니버섯" },
            new[] { "서리바구미", "얼음균사", "냉기포자", "빙결포식충" },
            new[] { "화염나방", "불씨곰팡이", "용암껍질충", "재포자" },
            new[] { "차원기생체", "균열감시자", "공허포자", "이세계포식자" }
        };

        private static readonly string[] BossNames =
        {
            "땅콩밭 보스", "창고 포자군주", "포식자의 우두머리",
            "빙결 저장고 군주", "화염 차원의 침략자", "균열 중심체"
        };

        private static readonly Color[] FloorColors =
        {
            new Color(0.50f, 0.68f, 0.34f),
            new Color(0.56f, 0.49f, 0.30f),
            new Color(0.29f, 0.52f, 0.28f),
            new Color(0.40f, 0.66f, 0.72f),
            new Color(0.68f, 0.34f, 0.20f),
            new Color(0.35f, 0.27f, 0.55f)
        };

        private static readonly Color[] PatchColors =
        {
            new Color(0.72f, 0.72f, 0.30f, 0.62f),
            new Color(0.36f, 0.31f, 0.18f, 0.62f),
            new Color(0.18f, 0.40f, 0.20f, 0.62f),
            new Color(0.64f, 0.88f, 0.94f, 0.62f),
            new Color(0.96f, 0.54f, 0.18f, 0.62f),
            new Color(0.64f, 0.35f, 0.82f, 0.62f)
        };

        private StageFlowController stageFlow;
        private RuntimeWorldViewPrototype worldView;
        private FieldInfo worldRootField;
        private FieldInfo enemyViewsField;
        private FieldInfo cameraField;
        private int appliedWorld = -1;
        private float refreshTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<WorldThemePrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorWorldThemePrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<WorldThemePrototype>();
        }

        private IEnumerator Start()
        {
            yield return null;
            stageFlow = FindFirstObjectByType<StageFlowController>();
            worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (stageFlow == null || worldView == null)
            {
                enabled = false;
                yield break;
            }

            Type viewType = typeof(RuntimeWorldViewPrototype);
            worldRootField = viewType.GetField("worldRoot", PrivateInstance);
            enemyViewsField = viewType.GetField("enemyViews", PrivateInstance);
            cameraField = viewType.GetField("worldCamera", PrivateInstance);
            stageFlow.StateChanged += ApplyTheme;
            ApplyTheme();
        }

        private void OnDestroy()
        {
            if (stageFlow != null) stageFlow.StateChanged -= ApplyTheme;
        }

        private void Update()
        {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.4f;
            ApplyUnitNames();
        }

        private void ApplyTheme()
        {
            if (stageFlow == null || worldView == null) return;
            int theme = ThemeIndex;
            if (appliedWorld != stageFlow.World)
            {
                appliedWorld = stageFlow.World;
                GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
                if (worldRoot != null)
                {
                    Transform floor = FindDeepChild(worldRoot.transform, "Peanut Field");
                    SpriteRenderer floorRenderer = floor != null ? floor.GetComponent<SpriteRenderer>() : null;
                    if (floorRenderer != null) floorRenderer.color = FloorColors[theme];

                    SpriteRenderer[] renderers = worldRoot.GetComponentsInChildren<SpriteRenderer>(true);
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i].gameObject.name == "Field Patch")
                            renderers[i].color = PatchColors[theme];
                    }
                }

                Camera camera = cameraField?.GetValue(worldView) as Camera;
                if (camera != null) camera.backgroundColor = Color.Lerp(FloorColors[theme], Color.black, 0.28f);
            }
            ApplyUnitNames();
        }

        private void ApplyUnitNames()
        {
            IDictionary views = enemyViewsField?.GetValue(worldView) as IDictionary;
            if (views == null) return;
            int theme = ThemeIndex;
            foreach (DictionaryEntry entry in views)
            {
                object unitView = entry.Value;
                if (unitView == null) continue;
                Type viewType = unitView.GetType();
                TextMesh label = viewType.GetField("Label", PublicInstance)?.GetValue(unitView) as TextMesh;
                SpriteRenderer body = viewType.GetField("Body", PublicInstance)?.GetValue(unitView) as SpriteRenderer;
                bool isBoss = Convert.ToBoolean(viewType.GetField("IsBoss", PublicInstance)?.GetValue(unitView) ?? false);
                if (label != null)
                {
                    int nameIndex = Mathf.Abs(entry.Key.GetHashCode()) % MonsterNames[theme].Length;
                    label.text = isBoss ? BossNames[theme] : MonsterNames[theme][nameIndex];
                }
                if (body != null && isBoss)
                    body.color = Color.Lerp(FloorColors[theme], new Color(0.75f, 0.10f, 0.12f), 0.72f);
            }
        }

        private int ThemeIndex => Mathf.Abs(stageFlow.World - 1) % FloorColors.Length;

        public string CurrentThemeDescription()
        {
            int theme = ThemeIndex;
            return $"{stageFlow.GetWorldDisplayName()} · 보스 {BossNames[theme]}";
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
