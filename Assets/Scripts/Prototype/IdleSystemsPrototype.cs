using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Core idle systems for three immortal pets. Each pet receives a separate target
    /// whenever possible and keeps a minimum distance from the other pets.
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
            public int AssignedEnemyIndex = -1;
        }

        private const string SavePrefix = "PeanutWarrior.Prototype.";
        private const float ArenaWidth = 760f;
        private const float ArenaHeight = 470f;
        private const float MapLeft = 55f;
        private const float MapRight = ArenaWidth - 55f;
        private const float MapTop = 155f;
        private const float MapBottom = ArenaHeight - 45f;
        private const float MinimumPetSpacing = 92f;
        private const float PetMoveSpeed = 88f;
        private const float PetAttackRange = 104f;

        private static readonly Vector2[] HuntingApproachOffsets =
        {
            new Vector2(-74f, 38f),
            new Vector2(0f, -76f),
            new Vector2(74f, 38f)
        };

        private static readonly Vector2[] BossApproachOffsets =
        {
            new Vector2(-96f, 42f),
            new Vector2(0f, -92f),
            new Vector2(96f, 42f)
        };

        private static readonly Vector2[] IdleFormationOffsets =
        {
            new Vector2(-126f, 44f),
            new Vector2(0f, -112f),
            new Vector2(126f, 44f)
        };

        private readonly List<MiniUnit> minis = new List<MiniUnit>();
        private readonly MiniElement[] huntingFormation =
            { MiniElement.Fire, MiniElement.Ice, MiniElement.Lightning };
        private readonly MiniElement[] bossFormation =
            { MiniElement.Lightning, MiniElement.Fire, MiniElement.Ice };
        private readonly HashSet<object> claimedTargets = new HashSet<object>();

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
        private string systemMessage = "펫 시스템 준비";

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
            if (arena == null || stageFlow == null)
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
                    systemMessage = $"알 부화 완료 · 펫 도감 {hatchedMinis} · 다이아 +1";
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
            Vector2 center = new Vector2(380f, 280f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 start = ClampToMap(center + IdleFormationOffsets[i]);
                minis.Add(new MiniUnit
                {
                    Position = start,
                    TargetPosition = start,
                    MoveTimer = UnityEngine.Random.Range(0.5f, 1.4f),
                    AttackTimer = UnityEngine.Random.Range(0.1f, 0.8f),
                    Element = huntingFormation[i],
                    AssignedEnemyIndex = -1
                });
            }
        }

        private void UpdateMinis()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            Vector2 playerPosition = playerPositionField != null
                ? (Vector2)playerPositionField.GetValue(arena)
                : new Vector2(380f, 280f);

            claimedTargets.Clear();
            bool bossBattle = stageFlow.Phase == StageFlowPhase.BossBattle;

            for (int i = 0; i < minis.Count; i++)
            {
                MiniUnit mini = minis[i];
                mini.Element = bossBattle ? bossFormation[i] : huntingFormation[i];

                object target = bossBattle
                    ? FindBoss(enemies, out Vector2 targetPosition, out int targetIndex)
                    : FindClosestUnclaimedEnemy(enemies, mini.Position, out targetPosition, out targetIndex);

                if (target != null)
                {
                    if (!bossBattle) claimedTargets.Add(target);
                    mini.AssignedEnemyIndex = targetIndex;
                    Vector2 offset = bossBattle
                        ? BossApproachOffsets[i % BossApproachOffsets.Length]
                        : HuntingApproachOffsets[i % HuntingApproachOffsets.Length];
                    Vector2 desired = ClampToMap(targetPosition + offset);
                    mini.TargetPosition = desired;
                    mini.Position = Vector2.MoveTowards(mini.Position, desired, PetMoveSpeed * Time.deltaTime);

                    mini.AttackTimer -= Time.deltaTime;
                    float distanceToEnemy = Vector2.Distance(mini.Position, targetPosition);
                    if (mini.AttackTimer <= 0f && distanceToEnemy <= PetAttackRange)
                    {
                        mini.AttackTimer = 0.9f - Mathf.Min(0.3f, miniAttackLevel * 0.01f);
                        float damage = MiniDamage;
                        bool critical = UnityEngine.Random.value < MiniCritChance;
                        if (critical) damage *= MiniCritMultiplier;
                        damage *= ElementMultiplier(mini.Element, target);
                        dealDamageMethod?.Invoke(arena, new[] { target, (object)damage, false });
                        systemMessage = $"{ElementName(mini.Element)} 펫 개별 공격{(critical ? " · 치명타" : string.Empty)}";
                    }
                }
                else
                {
                    mini.AssignedEnemyIndex = -1;
                    mini.MoveTimer -= Time.deltaTime;
                    Vector2 desired = ClampToMap(playerPosition + IdleFormationOffsets[i % IdleFormationOffsets.Length]);
                    if (mini.MoveTimer <= 0f || Vector2.Distance(mini.Position, mini.TargetPosition) < 8f)
                    {
                        mini.TargetPosition = desired + UnityEngine.Random.insideUnitCircle * 18f;
                        mini.TargetPosition = ClampToMap(mini.TargetPosition);
                        mini.MoveTimer = UnityEngine.Random.Range(1f, 2.2f);
                    }
                    mini.Position = Vector2.MoveTowards(mini.Position, mini.TargetPosition, 68f * Time.deltaTime);
                }

                mini.Position = ClampToMap(mini.Position);
            }

            SeparatePets();
        }

        private object FindClosestUnclaimedEnemy(IList enemies, Vector2 origin, out Vector2 position, out int index)
        {
            position = Vector2.zero;
            index = -1;
            if (enemies == null || enemies.Count == 0) return null;

            object closest = null;
            float best = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null || claimedTargets.Contains(enemy)) continue;
                FieldInfo positionInfo = enemy.GetType().GetField("Position", BindingFlags.Instance | BindingFlags.Public);
                if (positionInfo == null) continue;
                Vector2 enemyPosition = (Vector2)positionInfo.GetValue(enemy);
                float distance = Vector2.SqrMagnitude(enemyPosition - origin);
                if (distance >= best) continue;
                best = distance;
                closest = enemy;
                position = enemyPosition;
                index = i;
            }

            if (closest != null) return closest;

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
                index = i;
            }
            return closest;
        }

        private static object FindBoss(IList enemies, out Vector2 position, out int index)
        {
            position = Vector2.zero;
            index = -1;
            if (enemies == null || enemies.Count == 0) return null;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                Type type = enemy.GetType();
                FieldInfo positionInfo = type.GetField("Position", BindingFlags.Instance | BindingFlags.Public);
                if (positionInfo == null) continue;
                FieldInfo bossInfo = type.GetField("IsBoss", BindingFlags.Instance | BindingFlags.Public);
                if (bossInfo == null || Convert.ToBoolean(bossInfo.GetValue(enemy)))
                {
                    position = (Vector2)positionInfo.GetValue(enemy);
                    index = i;
                    return enemy;
                }
            }
            return null;
        }

        private void SeparatePets()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < minis.Count; i++)
                {
                    for (int j = i + 1; j < minis.Count; j++)
                    {
                        Vector2 delta = minis[j].Position - minis[i].Position;
                        float distance = delta.magnitude;
                        if (distance >= MinimumPetSpacing) continue;
                        Vector2 direction = distance > 0.01f
                            ? delta / distance
                            : Quaternion.Euler(0f, 0f, 120f * (j + 1)) * Vector2.right;
                        float correction = (MinimumPetSpacing - distance) * 0.5f;
                        minis[i].Position = ClampToMap(minis[i].Position - direction * correction);
                        minis[j].Position = ClampToMap(minis[j].Position + direction * correction);
                    }
                }
            }
        }

        private static float ElementMultiplier(MiniElement element, object target)
        {
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
            systemMessage = "펫 알 구매 완료";
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
            systemMessage = "알 부화 시작 · 60초";
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
            if (!long.TryParse(PlayerPrefs.GetString(key, "0"), out long previousTicks) || previousTicks <= 0)
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
            // Legacy debug GUI intentionally disabled. The mobile Canvas owns the UI.
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
