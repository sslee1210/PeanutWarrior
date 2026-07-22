using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Temporary zero-setup prototype. Enter Play Mode in any scene and this
    /// component creates a small debug panel for validating the full stage loop.
    /// Remove this file after the production battle UI is ready.
    /// </summary>
    public sealed class StageFlowPrototype : MonoBehaviour
    {
        private StageFlowController stageFlow;
        private Vector2 scroll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (FindFirstObjectByType<StageFlowPrototype>() != null)
            {
                return;
            }

            GameObject root = new GameObject("PeanutWarriorPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<StageFlowController>();
            root.AddComponent<StageFlowPrototype>();
        }

        private void Awake()
        {
            stageFlow = GetComponent<StageFlowController>();
            stageFlow.BossBattleStarted += OnBossBattleStarted;
            stageFlow.BossBattleFailed += OnBossBattleFailed;
            stageFlow.BossDefeated += OnBossDefeated;
            stageFlow.HuntingDeath += OnHuntingDeath;
        }

        private void OnDestroy()
        {
            if (stageFlow == null)
            {
                return;
            }

            stageFlow.BossBattleStarted -= OnBossBattleStarted;
            stageFlow.BossBattleFailed -= OnBossBattleFailed;
            stageFlow.BossDefeated -= OnBossDefeated;
            stageFlow.HuntingDeath -= OnHuntingDeath;
        }

        private void OnGUI()
        {
            const float width = 420f;
            const float height = 560f;
            Rect panel = new Rect(20f, 20f, width, Mathf.Min(height, Screen.height - 40f));

            GUILayout.BeginArea(panel, GUI.skin.window);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("땅콩전사 키우기 · 스테이지 프로토타입", HeaderStyle());
            GUILayout.Space(8f);
            GUILayout.Label($"지역: {stageFlow.GetWorldDisplayName()}");
            GUILayout.Label($"스테이지: {stageFlow.World}-{stageFlow.Stage}");
            GUILayout.Label($"진행: {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");
            GUILayout.Label($"상태: {GetPhaseLabel(stageFlow.Phase)}");
            GUILayout.Label($"자동도전: {(stageFlow.AutoChallenge ? "체크" : "미체크")}");

            GUILayout.Space(12f);
            bool nextAuto = GUILayout.Toggle(stageFlow.AutoChallenge, "자동도전");
            if (nextAuto != stageFlow.AutoChallenge)
            {
                stageFlow.SetAutoChallenge(nextAuto);
            }

            GUILayout.Space(8f);
            GUILayout.Label("사냥 테스트");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 처치"))
            {
                stageFlow.RegisterMonsterKill();
            }

            if (GUILayout.Button("+10 처치"))
            {
                stageFlow.RegisterMonsterKill(10);
            }

            if (GUILayout.Button("100마리 채우기"))
            {
                stageFlow.RegisterMonsterKill(StageFlowController.RequiredKills);
            }
            GUILayout.EndHorizontal();

            GUI.enabled = stageFlow.CanChallengeBoss;
            if (GUILayout.Button("보스 도전", GUILayout.Height(42f)))
            {
                stageFlow.StartBossBattle();
            }
            GUI.enabled = true;

            GUILayout.Space(8f);
            GUILayout.Label("사망·클리어 규칙 테스트");

            GUI.enabled = stageFlow.Phase != BattlePhase.BossBattle;
            if (GUILayout.Button("사냥 중 사망"))
            {
                stageFlow.HandleHuntingDeath();
            }
            GUI.enabled = true;

            GUI.enabled = stageFlow.Phase == BattlePhase.BossBattle;
            if (GUILayout.Button("보스전 사망"))
            {
                stageFlow.HandleBossBattleDeath();
            }

            if (GUILayout.Button("보스 처치"))
            {
                stageFlow.HandleBossDefeated();
            }
            GUI.enabled = true;

            GUILayout.Space(16f);
            GUILayout.Label("확정 규칙", HeaderStyle());
            GUILayout.Label("• 일반 몬스터 100마리 처치 후 보스 도전 가능");
            GUILayout.Label("• 자동도전 체크 시 100마리 달성 즉시 보스전 진입");
            GUILayout.Label("• 보스전 입장 시 실제 전투 시스템에서 HP·MP 완전 회복");
            GUILayout.Label("• 사냥 중 사망하면 이전 스테이지로 이동");
            GUILayout.Label("• 보스전 사망하면 현재 스테이지에서 0/100으로 재시작");
            GUILayout.Label("• 보스전 사망 시 자동도전 체크 해제");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string GetPhaseLabel(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.Hunting => "일반 사냥",
                BattlePhase.BossReady => "보스 도전 가능",
                BattlePhase.BossBattle => "보스전",
                _ => phase.ToString()
            };
        }

        private static GUIStyle HeaderStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            return style;
        }

        private static void OnBossBattleStarted()
        {
            Debug.Log("[PeanutWarrior] 보스용 세팅 적용 및 HP·MP 완전 회복 후 보스전 시작");
        }

        private static void OnBossBattleFailed()
        {
            Debug.Log("[PeanutWarrior] 보스전 사망: 현재 스테이지 유지, 자동도전 해제, 0/100 재시작");
        }

        private static void OnBossDefeated()
        {
            Debug.Log("[PeanutWarrior] 보스 처치");
        }

        private static void OnHuntingDeath()
        {
            Debug.Log("[PeanutWarrior] 사냥 중 사망: 자동도전 해제 후 이전 스테이지 이동");
        }
    }
}
