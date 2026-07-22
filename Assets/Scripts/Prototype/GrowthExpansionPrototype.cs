using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Completes the prototype gold-growth list with critical chance, critical
    /// damage, gold gain, HP regeneration, and basic stage navigation controls.
    /// </summary>
    public sealed class GrowthExpansionPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo goldField;
        private FieldInfo playerHpField;
        private PropertyInfo maxHpProperty;
        private FieldInfo attackLevelField;
        private FieldInfo lifetimeKillsField;

        private int critChanceLevel = 1;
        private int critDamageLevel = 1;
        private int goldGainLevel = 1;
        private int hpRegenLevel = 1;
        private float regenTimer;
        private int observedKills;
        private string message = "추가 성장 준비";
        private bool panelOpen;

        private float CritChance => Mathf.Min(1f, 0.05f + (critChanceLevel - 1) * 0.02f);
        private float CritDamage => 1.5f + (critDamageLevel - 1) * 0.12f;
        private float GoldGainMultiplier => 1f + (goldGainLevel - 1) * 0.08f;
        private float HpRegenPerSecond => 1.5f + (hpRegenLevel - 1) * 1.1f;
        private long CritChanceCost => 45L * critChanceLevel;
        private long CritDamageCost => 55L * critDamageLevel;
        private long GoldGainCost => 65L * goldGainLevel;
        private long HpRegenCost => 40L * hpRegenLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<GrowthExpansionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorGrowthExpansionPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<GrowthExpansionPrototype>();
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

            Type type = typeof(CombatPrototypeArena);
            goldField = type.GetField("gold", PrivateInstance);
            playerHpField = type.GetField("playerHp", PrivateInstance);
            maxHpProperty = type.GetProperty("PlayerMaxHp", PrivateInstance);
            attackLevelField = type.GetField("attackLevel", PrivateInstance);
            lifetimeKillsField = type.GetField("lifetimeKills", PrivateInstance);
            observedKills = LifetimeKills;
            Load();
        }

        private long Gold => goldField == null ? 0L : (long)goldField.GetValue(arena);
        private int LifetimeKills => lifetimeKillsField == null ? 0 : (int)lifetimeKillsField.GetValue(arena);

        private bool SpendGold(long amount)
        {
            if (Gold < amount || goldField == null) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private void Update()
        {
            if (arena == null || stageFlow == null) return;

            regenTimer += Time.deltaTime;
            if (regenTimer >= 0.25f)
            {
                float elapsed = regenTimer;
                regenTimer = 0f;
                RegenerateHp(elapsed);
            }

            int kills = LifetimeKills;
            if (kills > observedKills)
            {
                int gainedKills = kills - observedKills;
                observedKills = kills;
                long bonus = Mathf.RoundToInt(gainedKills * Mathf.Max(0f, GoldGainMultiplier - 1f) * (stageFlow.Stage + 2));
                if (bonus > 0 && goldField != null) goldField.SetValue(arena, Gold + bonus);
            }
        }

        private void RegenerateHp(float elapsed)
        {
            if (playerHpField == null || maxHpProperty == null) return;
            float hp = Convert.ToSingle(playerHpField.GetValue(arena));
            float maxHp = Convert.ToSingle(maxHpProperty.GetValue(arena));
            if (hp <= 0f || hp >= maxHp) return;
            playerHpField.SetValue(arena, Mathf.Min(maxHp, hp + HpRegenPerSecond * elapsed));
        }

        private void Upgrade(ref int level, long cost, string label)
        {
            if (!SpendGold(cost))
            {
                message = $"{label} 강화 골드 부족";
                return;
            }
            level++;
            message = $"{label} Lv.{level} 강화 완료";
            Save();
        }

        private void MovePreviousStage()
        {
            int world = stageFlow.World;
            int stage = stageFlow.Stage - 1;
            if (stage < 1)
            {
                if (world <= 1)
                {
                    message = "현재 최초 스테이지";
                    return;
                }
                world--;
                stage = StageFlowController.StagesPerWorld;
            }
            stageFlow.SelectStage(world, stage);
            message = $"{world}-{stage} 스테이지로 이동";
        }

        private void MoveNextStageForTesting()
        {
            int world = stageFlow.World;
            int stage = stageFlow.Stage + 1;
            if (stage > StageFlowController.StagesPerWorld)
            {
                world++;
                stage = 1;
            }
            stageFlow.SelectStage(world, stage);
            message = $"테스트 이동: {world}-{stage}";
        }

        private void Save()
        {
            PlayerPrefs.SetInt("PeanutWarrior.Growth.CritChance", critChanceLevel);
            PlayerPrefs.SetInt("PeanutWarrior.Growth.CritDamage", critDamageLevel);
            PlayerPrefs.SetInt("PeanutWarrior.Growth.GoldGain", goldGainLevel);
            PlayerPrefs.SetInt("PeanutWarrior.Growth.HpRegen", hpRegenLevel);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            critChanceLevel = Mathf.Max(1, PlayerPrefs.GetInt("PeanutWarrior.Growth.CritChance", 1));
            critDamageLevel = Mathf.Max(1, PlayerPrefs.GetInt("PeanutWarrior.Growth.CritDamage", 1));
            goldGainLevel = Mathf.Max(1, PlayerPrefs.GetInt("PeanutWarrior.Growth.GoldGain", 1));
            hpRegenLevel = Mathf.Max(1, PlayerPrefs.GetInt("PeanutWarrior.Growth.HpRegen", 1));
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }

        private void OnApplicationQuit() => Save();

        private void OnGUI()
        {
            if (GUI.Button(new Rect(150f, 88f, 126f, 38f), panelOpen ? "성장 닫기" : "추가 성장")) panelOpen = !panelOpen;
            if (!panelOpen || arena == null || stageFlow == null) return;

            Rect panel = new Rect(455f, 132f, 360f, 305f);
            GUI.Box(panel, "추가 성장·스테이지 이동");
            GUI.Label(new Rect(panel.x + 12f, panel.y + 28f, 335f, 22f),
                $"골드 {Gold} · {stageFlow.World}-{stageFlow.Stage} · {message}");

            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 58f, 160f, 46f),
                    $"치명타 확률 Lv.{critChanceLevel}\n{CritChance * 100f:0}% · {CritChanceCost}G"))
                Upgrade(ref critChanceLevel, CritChanceCost, "치명타 확률");

            if (GUI.Button(new Rect(panel.x + 184f, panel.y + 58f, 160f, 46f),
                    $"치명타 피해 Lv.{critDamageLevel}\n{CritDamage * 100f:0}% · {CritDamageCost}G"))
                Upgrade(ref critDamageLevel, CritDamageCost, "치명타 피해");

            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 112f, 160f, 46f),
                    $"골드 획득 Lv.{goldGainLevel}\n×{GoldGainMultiplier:0.00} · {GoldGainCost}G"))
                Upgrade(ref goldGainLevel, GoldGainCost, "골드 획득");

            if (GUI.Button(new Rect(panel.x + 184f, panel.y + 112f, 160f, 46f),
                    $"HP 회복 Lv.{hpRegenLevel}\n초당 {HpRegenPerSecond:0.0} · {HpRegenCost}G"))
                Upgrade(ref hpRegenLevel, HpRegenCost, "HP 회복");

            GUI.Label(new Rect(panel.x + 12f, panel.y + 170f, 335f, 42f),
                "치명타 확률 상한 100%\n골드 획득 보너스와 HP 자동 회복은 실시간 적용");

            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 218f, 160f, 36f), "이전 스테이지")) MovePreviousStage();
            if (GUI.Button(new Rect(panel.x + 184f, panel.y + 218f, 160f, 36f), "다음 스테이지 테스트")) MoveNextStageForTesting();
            GUI.Label(new Rect(panel.x + 12f, panel.y + 261f, 335f, 32f),
                "다음 스테이지 버튼은 개발 테스트용이며\n정식 버전에서는 클리어한 스테이지만 선택 가능하게 제한 예정");
        }
    }
}
