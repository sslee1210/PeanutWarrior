using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Adds a lightweight schema version and backup snapshot around the existing
    /// PlayerPrefs prototype save. Invalid values are clamped before they can spread
    /// into combat or UI state on the next session.
    /// </summary>
    [DefaultExecutionOrder(23000)]
    public sealed class PrototypeSaveIntegrityGuard : MonoBehaviour
    {
        [Serializable]
        private sealed class Snapshot
        {
            public int schemaVersion;
            public int world;
            public int stage;
            public int kills;
            public long gold;
            public long fragments;
            public int diamonds;
            public int advancementTier;
            public long utcTicks;
        }

        private const int CurrentSchemaVersion = 2;
        private const string VersionKey = "PeanutWarrior.Save.SchemaVersion";
        private const string BackupKey = "PeanutWarrior.Save.BackupJson";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo diamondsField;
        private FieldInfo advancementTierField;
        private FieldInfo worldField;
        private FieldInfo stageField;
        private FieldInfo killsField;
        private float backupTimer;
        private string lastReport = "저장 무결성 검사 대기";

        public string LastReport => lastReport;
        public int SchemaVersion => CurrentSchemaVersion;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PrototypeSaveIntegrityGuard>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPrototypeSaveIntegrityGuard");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSaveIntegrityGuard>();
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
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            diamondsField = arenaType.GetField("diamonds", PrivateInstance);
            advancementTierField = arenaType.GetField("advancementTier", PrivateInstance);

            Type flowType = typeof(StageFlowController);
            worldField = flowType.GetField("world", PrivateInstance);
            stageField = flowType.GetField("stage", PrivateInstance);
            killsField = flowType.GetField("monsterKills", PrivateInstance);

            ValidateAndRepair();
            SaveBackup();
        }

        private void Update()
        {
            backupTimer += Time.unscaledDeltaTime;
            if (backupTimer < 30f) return;
            backupTimer = 0f;
            ValidateAndRepair();
            SaveBackup();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            ValidateAndRepair();
            SaveBackup();
        }

        private void OnApplicationQuit()
        {
            ValidateAndRepair();
            SaveBackup();
        }

        private void ValidateAndRepair()
        {
            bool repaired = false;
            int world = ReadInt(worldField, stageFlow, 1);
            int stage = ReadInt(stageField, stageFlow, 1);
            int kills = ReadInt(killsField, stageFlow, 0);
            long gold = ReadLong(goldField, arena, 0L);
            long fragments = ReadLong(fragmentsField, arena, 0L);
            int diamonds = ReadInt(diamondsField, arena, 0);
            int advancement = ReadInt(advancementTierField, arena, 0);

            repaired |= SetIfDifferent(worldField, stageFlow, Mathf.Max(1, world));
            repaired |= SetIfDifferent(stageField, stageFlow, Mathf.Clamp(stage, 1, StageFlowController.StagesPerWorld));
            repaired |= SetIfDifferent(killsField, stageFlow, Mathf.Clamp(kills, 0, StageFlowController.RequiredKills));
            repaired |= SetIfDifferent(goldField, arena, Math.Max(0L, gold));
            repaired |= SetIfDifferent(fragmentsField, arena, Math.Max(0L, fragments));
            repaired |= SetIfDifferent(diamondsField, arena, Mathf.Max(0, diamonds));
            repaired |= SetIfDifferent(advancementTierField, arena, Mathf.Clamp(advancement, 0, 2));

            int oldVersion = PlayerPrefs.GetInt(VersionKey, 0);
            if (oldVersion != CurrentSchemaVersion)
            {
                PlayerPrefs.SetInt(VersionKey, CurrentSchemaVersion);
                repaired = true;
            }

            if (repaired)
            {
                PlayerPrefs.Save();
                lastReport = $"저장 데이터 보정 완료 · 스키마 v{CurrentSchemaVersion}";
                Debug.LogWarning($"[PeanutWarrior] {lastReport}");
            }
            else
            {
                lastReport = $"저장 데이터 정상 · 스키마 v{CurrentSchemaVersion}";
            }
        }

        private void SaveBackup()
        {
            if (arena == null || stageFlow == null) return;
            var snapshot = new Snapshot
            {
                schemaVersion = CurrentSchemaVersion,
                world = Mathf.Max(1, stageFlow.World),
                stage = Mathf.Clamp(stageFlow.Stage, 1, StageFlowController.StagesPerWorld),
                kills = Mathf.Clamp(stageFlow.MonsterKills, 0, StageFlowController.RequiredKills),
                gold = Math.Max(0L, ReadLong(goldField, arena, 0L)),
                fragments = Math.Max(0L, ReadLong(fragmentsField, arena, 0L)),
                diamonds = Mathf.Max(0, ReadInt(diamondsField, arena, 0)),
                advancementTier = Mathf.Clamp(ReadInt(advancementTierField, arena, 0), 0, 2),
                utcTicks = DateTime.UtcNow.Ticks
            };
            PlayerPrefs.SetString(BackupKey, JsonUtility.ToJson(snapshot));
            PlayerPrefs.SetInt(VersionKey, CurrentSchemaVersion);
            PlayerPrefs.Save();
        }

        public bool TryRestoreBackup()
        {
            string json = PlayerPrefs.GetString(BackupKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                lastReport = "복원할 백업이 없습니다";
                return false;
            }

            Snapshot snapshot;
            try
            {
                snapshot = JsonUtility.FromJson<Snapshot>(json);
            }
            catch (Exception exception)
            {
                lastReport = $"백업 해석 실패 · {exception.Message}";
                return false;
            }

            if (snapshot == null || snapshot.schemaVersion <= 0)
            {
                lastReport = "백업 형식이 올바르지 않습니다";
                return false;
            }

            worldField?.SetValue(stageFlow, Mathf.Max(1, snapshot.world));
            stageField?.SetValue(stageFlow, Mathf.Clamp(snapshot.stage, 1, StageFlowController.StagesPerWorld));
            killsField?.SetValue(stageFlow, Mathf.Clamp(snapshot.kills, 0, StageFlowController.RequiredKills));
            goldField?.SetValue(arena, Math.Max(0L, snapshot.gold));
            fragmentsField?.SetValue(arena, Math.Max(0L, snapshot.fragments));
            diamondsField?.SetValue(arena, Mathf.Max(0, snapshot.diamonds));
            advancementTierField?.SetValue(arena, Mathf.Clamp(snapshot.advancementTier, 0, 2));
            lastReport = "최근 백업 복원 완료";
            PlayerPrefs.SetInt(VersionKey, CurrentSchemaVersion);
            PlayerPrefs.Save();
            return true;
        }

        private static int ReadInt(FieldInfo field, object target, int fallback)
        {
            return field == null || target == null ? fallback : Convert.ToInt32(field.GetValue(target));
        }

        private static long ReadLong(FieldInfo field, object target, long fallback)
        {
            return field == null || target == null ? fallback : Convert.ToInt64(field.GetValue(target));
        }

        private static bool SetIfDifferent(FieldInfo field, object target, int value)
        {
            if (field == null || target == null) return false;
            int current = Convert.ToInt32(field.GetValue(target));
            if (current == value) return false;
            field.SetValue(target, value);
            return true;
        }

        private static bool SetIfDifferent(FieldInfo field, object target, long value)
        {
            if (field == null || target == null) return false;
            long current = Convert.ToInt64(field.GetValue(target));
            if (current == value) return false;
            field.SetValue(target, value);
            return true;
        }
    }
}
