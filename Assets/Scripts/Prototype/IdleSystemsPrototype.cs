using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Prototype-only meta systems layered on top of CombatPrototypeArena.
    /// Includes mini peanuts, formations, egg incubation, missions, achievements,
    /// PlayerPrefs save/load, and capped offline rewards.
    /// </summary>
    public sealed class IdleSystemsPrototype : MonoBehaviour
    {
        private enum MiniElement { Fire, Ice, Lightning }

        private sealed class MiniUnit
        {
            public Vector2 Position;
            public Vector2 TargetPosition;
            public float MoveTimer;
            public float AttackTimer;
            public MiniElement Element;
        }

        private const string SavePrefix = "PeanutWarrior.Prototype.";
        private const float ArenaWidth = 760f;
        private const float ArenaHeight = 470f;
        private const float MapLeft = 55f;
        private const float MapRight = ArenaWidth - 55f;
        private const float MapTop = 155f;
        private const float MapBottom = ArenaHeight - 45f;

        private readonly List<MiniUnit> minis = new List<MiniUnit>();
        private readonly MiniElement[] huntingFormation =
            { MiniElement.Fire, MiniElement.Ice, MiniElement.Lightning };
        private readonly MiniElement[] bossFormation =
            { MiniElement.Lightning, MiniElement.Fire, MiniElement.Ice };

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private Type arenaType;
        private FieldInfo enemiesField;
        private FieldInfo playerPositionField;
        private FieldInfo miniUnlockedField;
        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo diamondsField;
        private FieldInfo lifetimeKillsField;
        private FieldInfo advancementTierField;
        private MethodInfo dealDamageMethod;

        private int miniAttackLevel = 1;
        private int miniCritLevel = 1;
        private int miniCritDamageLevel = 1;
        private int eggs;
        private int hatchedMinis;
        private bool incubating;
        private float incubationRemaining;
        private int claimedKillMissionTier;
        private int claimedStageMissionTier;
        private int claimedAchievementTier;
        private float saveTimer;
        private string systemMessage = "미니 시스템 준비";

        private long MiniAttackCost => 80L * miniAttackLevel;
        private long MiniCritCost => 100L * miniCritLevel;
        private long MiniCritDamageCost => 120L * miniCritDamageLevel;
        private float MiniDamage => 8f + (miniAttackLevel - 1) * 3f;
        private float MiniCritChance => Mathf.Min(1f, 0.05f + (miniCritLevel - 1) * 0.025f);
        private float MiniCritMultiplier => 1.5f + (miniCritDamageLevel - 1) * 0.15f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateSystem()
        {
            if (FindFirstObjectByType<IdleSystemsPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorIdleSystemsPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<IdleSystemsPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (arena == null)
            {
                enabled = false;
                return;
            }

            arenaType = typeof(CombatPrototypeArena);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            enemiesField = arenaType.GetField("enemies", flags);
            playerPositionField = arenaType.GetField("playerPosition", flags);
            miniUnlockedField = arenaType.GetField("miniSlotsUnlocked", flags);
            goldField = arenaType.GetField("gold", flags);
            fragmentsField = arenaType.GetField("fragments", flags);
            diamondsField = arenaType.GetField("diamonds", flags);
            lifetimeKillsField = arenaType.GetField("lifetimeKills", flags);
            advancementTierField = arenaType.GetField("advancementTier", flags);
            dealDamageMethod = arenaType.GetMethod("DealDamage", flags);

            CreateMinis();
            LoadProgress();
            GrantOfflineRewards();
        }

        private void Update()
        {
            if (arena == null || stageFlow == null) return;

            if (incubating)
            {
                incubationRemaining -= Time.deltaTime;
                if (incubationRemaining <= 0f)
                {
                    incubating = false;
                    eggs = Mathf.Max(0, eggs - 1);
                    hatchedMinis++;
                    AddDiamonds(1);
                    systemMessage = $"알 부화 완료 · 미니 도감 {hatchedMinis} · 다이아 +1";
                }
            }

            if (IsMiniUnlocked()) UpdateMinis();

            saveTimer += Time.deltaTime;
            if (saveTimer >= 10f)
            {
                saveTimer = 0f;
                SaveProgress();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveProgress();
        }

        private void OnApplicationQuit() => SaveProgress();

        private void CreateMinis()
        {
            minis.Clear();
            for (int i = 0; i < 3; i++)
            {
                minis.Add(new MiniUnit
                {
                    Position = new Vector2(300f + i * 35f, 260f),
                    TargetPosition = RandomMapPoint(),
                    MoveTimer = UnityEngine.Random.Range(0.5f, 2f),
                    AttackTimer = UnityEngine.Random.Range(0.1f, 0.8f),
                    Element = huntingFormation[i]
                });
            }
        }

        private void UpdateMinis()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            Vector2 playerPosition = playerPositionField != null
                ? (Vector2)playerPositionField.GetValue(arena)
                : new Vector2(380f, 280f);

            for (int i = 0; i < minis.Count; i++)
            {
                MiniUnit mini = minis[i];
                mini.Element = stageFlow.Phase == StageFlowPhase.BossBattle
                    ? bossFormation[i]
                    : huntingFormation[i];

                object target = FindClosestEnemy(enemies, mini.Position, out Vector2 enemyPosition);
                if (target != null && Vector2.Distance(mini.Position, enemyPosition) <= 210f)
                {
                    if (Vector2.Distance(mini.Position, enemyPosition) > 78f)
                    {
                        mini.Position = Vector2.MoveTowards(mini.Position, enemyPosition, 78f * Time.deltaTime);
                    }

                    mini.AttackTimer -= Time.deltaTime;
                    if (mini.AttackTimer <= 0f && Vector2.Distance(mini.Position, enemyPosition) <= 95f)
                    {
                        mini.AttackTimer = 0.9f - Mathf.Min(0.3f, miniAttackLevel * 0.01f);
                        float damage = MiniDamage;
                        bool critical = UnityEngine.Random.value < MiniCritChance;
                        if (critical) damage *= MiniCritMultiplier;
                        damage *= ElementMultiplier(mini.Element, target);
                        dealDamageMethod?.Invoke(arena, new[] { target, (object)damage, false });
                        systemMessage = $"미니 {ElementName(mini.Element)} 공격{(critical ? " · 치명타" : string.Empty)}";
                    }
                }
                else
                {
                    mini.MoveTimer -= Time.deltaTime;
                    if (mini.MoveTimer <= 0f || Vector2.Distance(mini.Position, mini.TargetPosition) < 8f)
                    {
                        Vector2 orbit = playerPosition + UnityEngine.Random.insideUnitCircle * 115f;
                        mini.TargetPosition = ClampToMap(orbit);
                        mini.MoveTimer = UnityEngine.Random.Range(1f, 2.8f);
                    }
                    mini.Position = Vector2.MoveTowards(mini.Position, mini.TargetPosition, 62f * Time.deltaTime);
                }
                mini.Position = ClampToMap(mini.Position);
            }
        }

        private object FindClosestEnemy(IList enemies, Vector2 origin, out Vector2 position)
        {
            position = Vector2.zero;
            if (enemies == null || enemies.Count == 0) return null;

            object closest = null;
            float best = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                FieldInfo positionInfo = enemy.GetType().GetField("Position", BindingFlags.Instance | BindingFlags.Public);
                if (positionInfo == null) continue;
                Vector2 enemyPosition = (Vector2)positionInfo.GetValue(enemy);
                float distance = Vector2.SqrMagnitude(enemyPosition - origin);
                if (distance >= best) continue;
                best = distance;
                closest = enemy;
                position = enemyPosition;
            }
            return closest;
        }

        private static float ElementMultiplier(MiniElement element, object target)
        {
            // No enemy weakness system: each element keeps a neutral damage budget.
            return element switch
            {
                MiniElement.Fire => 1.04f,
                MiniElement.Ice => 0.96f,
                MiniElement.Lightning => 1f,
                _ => 1f
            };
        }

        private bool IsMiniUnlocked()
        {
            return miniUnlockedField != null && (bool)miniUnlockedField.GetValue(arena);
        }

        private long Gold => goldField == null ? 0L : (long)goldField.GetValue(arena);
        private int Diamonds => diamondsField == null ? 0 : (int)diamondsField.GetValue(arena);
        private int LifetimeKills => lifetimeKillsField == null ? 0 : (int)lifetimeKillsField.GetValue(arena);
        private int AdvancementTier => advancementTierField == null ? 0 : (int)advancementTierField.GetValue(arena);

        private void AddGold(long amount)
        {
            if (goldField != null) goldField.SetValue(arena, Gold + amount);
        }

        private bool SpendGold(long amount)
        {
            if (Gold < amount) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private void AddFragments(long amount)
        {
            if (fragmentsField == null) return;
            long current = (long)fragmentsField.GetValue(arena);
            fragmentsField.SetValue(arena, current + amount);
        }

        private void AddDiamonds(int amount)
        {
            if (diamondsField != null) diamondsField.SetValue(arena, Diamonds + amount);
        }

        private void CycleFormation(int slot, bool boss)
        {
            MiniElement[] formation = boss ? bossFormation : huntingFormation;
            formation[slot] = (MiniElement)(((int)formation[slot] + 1) % 3);
        }

        private void BuyEgg()
        {
            if (Diamonds < 3)
            {
                systemMessage = "알 구매에 다이아 3개 필요";
                return;
            }
            diamondsField.SetValue(arena, Diamonds - 3);
            eggs++;
            systemMessage = "미니 알 구매 완료";
        }

        private void StartIncubation()
        {
            if (incubating || eggs <= 0)
            {
                systemMessage = incubating ? "이미 부화 중" : "보유한 알이 없음";
                return;
            }
            incubating = true;
            incubationRemaining = 60f;
            systemMessage = "알 부화 시작 · 프로토타입 60초";
        }

        private void ClaimKillMission()
        {
            int tier = LifetimeKills / 50;
            if (tier <= claimedKillMissionTier)
            {
                systemMessage = "처치 미션 보상 조건 미달";
                return;
            }
            int rewards = tier - claimedKillMissionTier;
            claimedKillMissionTier = tier;
            AddDiamonds(rewards * 2);
            systemMessage = $"처치 미션 보상 · 다이아 +{rewards * 2}";
        }

        private void ClaimStageMission()
        {
            int globalStage = (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;
            int tier = globalStage / 2;
            if (tier <= claimedStageMissionTier)
            {
                systemMessage = "스테이지 미션 보상 조건 미달";
                return;
            }
            int rewards = tier - claimedStageMissionTier;
            claimedStageMissionTier = tier;
            AddDiamonds(rewards * 3);
            systemMessage = $"스테이지 미션 보상 · 다이아 +{rewards * 3}";
        }

        private void ClaimGrowthAchievement()
        {
            int tier = (miniAttackLevel + miniCritLevel + miniCritDamageLevel + AdvancementTier * 5) / 5;
            if (tier <= claimedAchievementTier)
            {
                systemMessage = "성장 업적 보상 조건 미달";
                return;
            }
            int rewards = tier - claimedAchievementTier;
            claimedAchievementTier = tier;
            AddFragments(rewards * 3L);
            AddDiamonds(rewards);
            systemMessage = $"성장 업적 · 조각 +{rewards * 3}, 다이아 +{rewards}";
        }

        private void GrantOfflineRewards()
        {
            string key = SavePrefix + "LastUtcTicks";
            long previousTicks;
            if (!long.TryParse(PlayerPrefs.GetString(key, "0"), out previousTicks) || previousTicks <= 0)
            {
                PlayerPrefs.SetString(key, DateTime.UtcNow.Ticks.ToString());
                return;
            }

            double seconds = new TimeSpan(DateTime.UtcNow.Ticks - previousTicks).TotalSeconds;
            seconds = Math.Max(0d, Math.Min(seconds, 8d * 60d * 60d));
            if (seconds < 30d) return;

            int simulatedKills = Mathf.FloorToInt((float)seconds / 6f);
            long offlineGold = simulatedKills * Math.Max(2, stageFlow.Stage + 2);
            long offlineFragments = simulatedKills / 20;
            AddGold(offlineGold);
            AddFragments(offlineFragments);
            systemMessage = $"오프라인 {Mathf.FloorToInt((float)seconds / 60f)}분 · {offlineGold}G, 조각 {offlineFragments}";
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(SavePrefix + "MiniAttack", miniAttackLevel);
            PlayerPrefs.SetInt(SavePrefix + "MiniCrit", miniCritLevel);
            PlayerPrefs.SetInt(SavePrefix + "MiniCritDamage", miniCritDamageLevel);
            PlayerPrefs.SetInt(SavePrefix + "Eggs", eggs);
            PlayerPrefs.SetInt(SavePrefix + "Hatched", hatchedMinis);
            PlayerPrefs.SetInt(SavePrefix + "KillMission", claimedKillMissionTier);
            PlayerPrefs.SetInt(SavePrefix + "StageMission", claimedStageMissionTier);
            PlayerPrefs.SetInt(SavePrefix + "Achievement", claimedAchievementTier);
            PlayerPrefs.SetInt(SavePrefix + "Incubating", incubating ? 1 : 0);
            PlayerPrefs.SetFloat(SavePrefix + "Incubation", incubationRemaining);
            for (int i = 0; i < 3; i++)
            {
                PlayerPrefs.SetInt(SavePrefix + $"HuntFormation{i}", (int)huntingFormation[i]);
                PlayerPrefs.SetInt(SavePrefix + $"BossFormation{i}", (int)bossFormation[i]);
            }
            PlayerPrefs.SetString(SavePrefix + "LastUtcTicks", DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            miniAttackLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "MiniAttack", 1));
            miniCritLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "MiniCrit", 1));
            miniCritDamageLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "MiniCritDamage", 1));
            eggs = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "Eggs", 0));
            hatchedMinis = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "Hatched", 0));
            claimedKillMissionTier = PlayerPrefs.GetInt(SavePrefix + "KillMission", 0);
            claimedStageMissionTier = PlayerPrefs.GetInt(SavePrefix + "StageMission", 0);
            claimedAchievementTier = PlayerPrefs.GetInt(SavePrefix + "Achievement", 0);
            incubating = PlayerPrefs.GetInt(SavePrefix + "Incubating", 0) == 1;
            incubationRemaining = PlayerPrefs.GetFloat(SavePrefix + "Incubation", 0f);
            for (int i = 0; i < 3; i++)
            {
                huntingFormation[i] = (MiniElement)PlayerPrefs.GetInt(SavePrefix + $"HuntFormation{i}", i);
                bossFormation[i] = (MiniElement)PlayerPrefs.GetInt(SavePrefix + $"BossFormation{i}", 2 - i);
            }
        }

        private void OnGUI()
        {
            if (arena == null || stageFlow == null) return;
            float left = Mathf.Max(20f, (Screen.width - ArenaWidth) * 0.5f);
            float top = Mathf.Max(20f, Screen.height - ArenaHeight - 35f);

            if (IsMiniUnlocked())
            {
                for (int i = 0; i < minis.Count; i++)
                {
                    MiniUnit mini = minis[i];
                    Rect rect = new Rect(left + mini.Position.x - 18f, top + mini.Position.y - 18f, 36f, 36f);
                    GUI.Box(rect, $"미니\n{ElementName(mini.Element)}");
                }
            }

            Rect panel = new Rect(left - 250f, top, 240f, 470f);
            GUI.Box(panel, "미니·방치 시스템");
            GUI.Label(new Rect(panel.x + 10f, panel.y + 28f, 220f, 38f), systemMessage);
            GUI.Label(new Rect(panel.x + 10f, panel.y + 68f, 220f, 42f),
                IsMiniUnlocked() ? $"미니 3/3 활동 · 외형 전직 {Mathf.Max(0, AdvancementTier - 1)}단계" : "2차 전직 후 미니 3슬롯 해금");

            if (GUI.Button(new Rect(panel.x + 10f, panel.y + 112f, 105f, 42f), $"미니 공격 Lv.{miniAttackLevel}\n{MiniAttackCost}G") && SpendGold(MiniAttackCost)) miniAttackLevel++;
            if (GUI.Button(new Rect(panel.x + 122f, panel.y + 112f, 105f, 42f), $"치명타 Lv.{miniCritLevel}\n{MiniCritCost}G") && SpendGold(MiniCritCost)) miniCritLevel++;
            if (GUI.Button(new Rect(panel.x + 10f, panel.y + 160f, 217f, 42f), $"치명타 피해 Lv.{miniCritDamageLevel} · {MiniCritDamageCost}G") && SpendGold(MiniCritDamageCost)) miniCritDamageLevel++;

            GUI.Label(new Rect(panel.x + 10f, panel.y + 208f, 220f, 22f), "사냥 편성 (클릭해 속성 변경)");
            for (int i = 0; i < 3; i++)
            {
                if (GUI.Button(new Rect(panel.x + 10f + i * 73f, panel.y + 232f, 68f, 34f), ElementName(huntingFormation[i]))) CycleFormation(i, false);
            }
            GUI.Label(new Rect(panel.x + 10f, panel.y + 270f, 220f, 22f), "보스 편성");
            for (int i = 0; i < 3; i++)
            {
                if (GUI.Button(new Rect(panel.x + 10f + i * 73f, panel.y + 294f, 68f, 34f), ElementName(bossFormation[i]))) CycleFormation(i, true);
            }

            GUI.Label(new Rect(panel.x + 10f, panel.y + 334f, 220f, 22f), $"알 {eggs} · 부화 도감 {hatchedMinis}");
            if (GUI.Button(new Rect(panel.x + 10f, panel.y + 358f, 105f, 34f), "알 구매 3◆")) BuyEgg();
            if (GUI.Button(new Rect(panel.x + 122f, panel.y + 358f, 105f, 34f), incubating ? $"부화 {Mathf.CeilToInt(incubationRemaining)}초" : "부화 시작")) StartIncubation();

            if (GUI.Button(new Rect(panel.x + 10f, panel.y + 398f, 68f, 48f), "처치\n미션")) ClaimKillMission();
            if (GUI.Button(new Rect(panel.x + 84f, panel.y + 398f, 68f, 48f), "스테이지\n미션")) ClaimStageMission();
            if (GUI.Button(new Rect(panel.x + 158f, panel.y + 398f, 68f, 48f), "성장\n업적")) ClaimGrowthAchievement();
        }

        private static Vector2 RandomMapPoint()
        {
            return new Vector2(UnityEngine.Random.Range(MapLeft, MapRight), UnityEngine.Random.Range(MapTop, MapBottom));
        }

        private static Vector2 ClampToMap(Vector2 point)
        {
            point.x = Mathf.Clamp(point.x, MapLeft, MapRight);
            point.y = Mathf.Clamp(point.y, MapTop, MapBottom);
            return point;
        }

        private static string ElementName(MiniElement element)
        {
            return element switch
            {
                MiniElement.Fire => "화염",
                MiniElement.Ice => "냉기",
                MiniElement.Lightning => "번개",
                _ => element.ToString()
            };
        }
    }
}
