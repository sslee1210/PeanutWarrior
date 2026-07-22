using System;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Persists the current prototype's core resources, upgrades, advancement,
    /// equipment selections, skills, and stage position through PlayerPrefs.
    /// </summary>
    public sealed class PrototypeSaveBridge : MonoBehaviour
    {
        private const string Prefix = "PeanutWarrior.CoreSave.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private float saveTimer;
        private string saveMessage = "저장 준비";

        private static readonly string[] ArenaIntFields =
        {
            "attackLevel", "hpLevel", "maxMpLevel", "mpRegenLevel",
            "basicAttackLevel", "diamonds", "lifetimeKills",
            "lastDiamondMilestone", "advancementTier"
        };

        private static readonly string[] ArenaLongFields =
        {
            "gold", "fragments"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateBridge()
        {
            if (FindFirstObjectByType<PrototypeSaveBridge>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPrototypeSaveBridge");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSaveBridge>();
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
            Load();
        }

        private void Update()
        {
            saveTimer += Time.deltaTime;
            if (saveTimer < 10f) return;
            saveTimer = 0f;
            Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private void Save()
        {
            if (arena == null || stageFlow == null) return;

            Type arenaType = typeof(CombatPrototypeArena);
            foreach (string fieldName in ArenaIntFields)
            {
                FieldInfo field = arenaType.GetField(fieldName, PrivateInstance);
                if (field != null) PlayerPrefs.SetInt(Prefix + fieldName, (int)field.GetValue(arena));
            }
            foreach (string fieldName in ArenaLongFields)
            {
                FieldInfo field = arenaType.GetField(fieldName, PrivateInstance);
                if (field != null) PlayerPrefs.SetString(Prefix + fieldName, ((long)field.GetValue(arena)).ToString());
            }

            SaveEnumField(arenaType, "huntingElement");
            SaveEnumField(arenaType, "bossElement");
            SaveIntArray(arenaType, "skillLevels");

            Type flowType = typeof(StageFlowController);
            SaveFlowInt(flowType, "world");
            SaveFlowInt(flowType, "stage");
            SaveFlowInt(flowType, "monsterKills");
            FieldInfo autoField = flowType.GetField("autoChallenge", PrivateInstance);
            if (autoField != null) PlayerPrefs.SetInt(Prefix + "autoChallenge", (bool)autoField.GetValue(stageFlow) ? 1 : 0);

            PlayerPrefs.SetString(Prefix + "SavedUtc", DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
            saveMessage = "자동 저장 완료";
        }

        private void Load()
        {
            Type arenaType = typeof(CombatPrototypeArena);
            foreach (string fieldName in ArenaIntFields)
            {
                string key = Prefix + fieldName;
                FieldInfo field = arenaType.GetField(fieldName, PrivateInstance);
                if (field != null && PlayerPrefs.HasKey(key)) field.SetValue(arena, PlayerPrefs.GetInt(key));
            }
            foreach (string fieldName in ArenaLongFields)
            {
                string key = Prefix + fieldName;
                FieldInfo field = arenaType.GetField(fieldName, PrivateInstance);
                if (field == null || !PlayerPrefs.HasKey(key)) continue;
                if (long.TryParse(PlayerPrefs.GetString(key), out long value)) field.SetValue(arena, value);
            }

            LoadEnumField(arenaType, "huntingElement");
            LoadEnumField(arenaType, "bossElement");
            LoadIntArray(arenaType, "skillLevels");

            Type flowType = typeof(StageFlowController);
            LoadFlowInt(flowType, "world", 1, int.MaxValue);
            LoadFlowInt(flowType, "stage", 1, StageFlowController.StagesPerWorld);
            LoadFlowInt(flowType, "monsterKills", 0, StageFlowController.RequiredKills);

            FieldInfo killsField = flowType.GetField("monsterKills", PrivateInstance);
            FieldInfo phaseField = flowType.GetField("phase", PrivateInstance);
            if (killsField != null && phaseField != null)
            {
                int kills = (int)killsField.GetValue(stageFlow);
                object phase = Enum.Parse(phaseField.FieldType,
                    kills >= StageFlowController.RequiredKills ? "BossReady" : "Hunting");
                phaseField.SetValue(stageFlow, phase);
            }

            FieldInfo autoField = flowType.GetField("autoChallenge", PrivateInstance);
            if (autoField != null) autoField.SetValue(stageFlow, PlayerPrefs.GetInt(Prefix + "autoChallenge", 0) == 1);

            FieldInfo miniField = arenaType.GetField("miniSlotsUnlocked", PrivateInstance);
            FieldInfo advancementField = arenaType.GetField("advancementTier", PrivateInstance);
            if (miniField != null && advancementField != null)
                miniField.SetValue(arena, (int)advancementField.GetValue(arena) >= 2);

            MethodInfo restore = arenaType.GetMethod("FullRestore", PrivateInstance);
            restore?.Invoke(arena, null);
            saveMessage = PlayerPrefs.HasKey(Prefix + "SavedUtc") ? "저장 데이터 불러옴" : "새 게임 데이터";
        }

        private void SaveEnumField(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            if (field != null) PlayerPrefs.SetInt(Prefix + name, Convert.ToInt32(field.GetValue(arena)));
        }

        private void LoadEnumField(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            if (field == null || !PlayerPrefs.HasKey(Prefix + name)) return;
            field.SetValue(arena, Enum.ToObject(field.FieldType, PlayerPrefs.GetInt(Prefix + name)));
        }

        private void SaveIntArray(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            int[] values = field?.GetValue(arena) as int[];
            if (values == null) return;
            for (int i = 0; i < values.Length; i++) PlayerPrefs.SetInt(Prefix + name + i, values[i]);
        }

        private void LoadIntArray(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            int[] values = field?.GetValue(arena) as int[];
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
            {
                string key = Prefix + name + i;
                if (PlayerPrefs.HasKey(key)) values[i] = Mathf.Max(1, PlayerPrefs.GetInt(key));
            }
        }

        private void SaveFlowInt(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            if (field != null) PlayerPrefs.SetInt(Prefix + name, (int)field.GetValue(stageFlow));
        }

        private void LoadFlowInt(Type type, string name, int min, int max)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            if (field == null || !PlayerPrefs.HasKey(Prefix + name)) return;
            field.SetValue(stageFlow, Mathf.Clamp(PlayerPrefs.GetInt(Prefix + name), min, max));
        }

        private void ResetSave()
        {
            foreach (string fieldName in ArenaIntFields) PlayerPrefs.DeleteKey(Prefix + fieldName);
            foreach (string fieldName in ArenaLongFields) PlayerPrefs.DeleteKey(Prefix + fieldName);
            PlayerPrefs.DeleteKey(Prefix + "huntingElement");
            PlayerPrefs.DeleteKey(Prefix + "bossElement");
            for (int i = 0; i < 8; i++) PlayerPrefs.DeleteKey(Prefix + "skillLevels" + i);
            PlayerPrefs.DeleteKey(Prefix + "world");
            PlayerPrefs.DeleteKey(Prefix + "stage");
            PlayerPrefs.DeleteKey(Prefix + "monsterKills");
            PlayerPrefs.DeleteKey(Prefix + "autoChallenge");
            PlayerPrefs.DeleteKey(Prefix + "SavedUtc");
            PlayerPrefs.Save();
            saveMessage = "핵심 저장 데이터 삭제됨 · 재실행 시 초기화";
        }

        private void OnGUI()
        {
            Rect panel = new Rect(Screen.width - 210f, Screen.height - 82f, 195f, 66f);
            GUI.Box(panel, saveMessage);
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 30f, 82f, 28f), "즉시 저장")) Save();
            if (GUI.Button(new Rect(panel.x + 98f, panel.y + 30f, 88f, 28f), "저장 삭제")) ResetSave();
        }
    }
}
