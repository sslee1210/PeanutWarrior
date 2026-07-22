using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(20000)]
    public sealed class PeanutSaveGameService : MonoBehaviour
    {
        public const int CurrentSchemaVersion = 3;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";
        private const string FirstClearPrefix = "PeanutWarrior.FirstClear.Stage.";

        [Serializable]
        private sealed class SaveData
        {
            public int schemaVersion = CurrentSchemaVersion;
            public long savedUtcTicks;
            public CoreData core = new CoreData();
            public StageData stage = new StageData();
            public GrowthData growth = new GrowthData();
            public PetData pets = new PetData();
            public ShopData shop = new ShopData();
            public SettingsData settings = new SettingsData();
            public ClearData clears = new ClearData();
        }

        [Serializable]
        private sealed class CoreData
        {
            public long gold;
            public long fragments;
            public int diamonds;
            public int lifetimeKills;
            public int attackLevel = 1;
            public int hpLevel = 1;
            public int maxMpLevel = 1;
            public int mpRegenLevel = 1;
            public int basicAttackLevel = 1;
            public int advancementTier;
            public bool petSlotsUnlocked;
            public int huntingElement;
            public int bossElement;
        }

        [Serializable]
        private sealed class StageData
        {
            public int world = 1;
            public int stage = 1;
            public int monsterKills;
            public bool autoChallenge = true;
            public int highestGlobalStage = 1;
        }

        [Serializable]
        private sealed class GrowthData
        {
            public int critChanceLevel = 1;
            public int critDamageLevel = 1;
            public int goldGainLevel = 1;
            public int hpRegenLevel = 1;
            public int expGainLevel = 1;
            public int equipmentGainLevel = 1;
            public int playerLevel = 1;
            public long currentExperience;
            public int equipmentMaterials;
            public float equipmentMaterialProgress;
        }

        [Serializable]
        private sealed class PetData
        {
            public int[] levels = { 1, 1, 1 };
            public int[] stars = { 1, 1, 1 };
            public int[] shards = { 0, 0, 0 };
            public int[] lifetimeHatches = { 0, 0, 0 };
            public int eggs;
            public int hatchedPets;
            public bool incubating;
            public float incubationRemaining;
        }

        [Serializable]
        private sealed class ShopData
        {
            public int dailyStreak;
            public string lastClaimDate = string.Empty;
        }

        [Serializable]
        private sealed class SettingsData
        {
            public float bgmVolume = 0.8f;
            public float sfxVolume = 0.9f;
            public bool vibration = true;
            public int frameRate = 60;
        }

        [Serializable]
        private sealed class ClearData
        {
            public int bossKills;
            public int uniqueClears;
            public List<int> clearedGlobalStages = new List<int>();
        }

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private AdvancementProgressionPrototype advancement;
        private PetProgressionPrototype petProgression;
        private IdleSystemsPrototype legacyPets;
        private PrototypeShopAndDaily shop;
        private FirstClearRewardPrototype firstClear;
        private GameSettingsPrototype settings;

        private float saveTimer;
        private float backupTimer;
        private bool initialized;
        private string lastMessage = "저장 서비스 준비";

        public int SchemaVersion => CurrentSchemaVersion;
        public string LastMessage => lastMessage;
        public string MainSavePath => Path.Combine(Application.persistentDataPath, "peanut-warrior-save.json");
        public string BackupSavePath => Path.Combine(Application.persistentDataPath, "peanut-warrior-save.backup.json");
        public bool HasMainSave => File.Exists(MainSavePath);
        public bool HasBackupSave => File.Exists(BackupSavePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutSaveGameService>() != null) return;
            GameObject root = new GameObject("PeanutWarriorSaveGameService");
            DontDestroyOnLoad(root);
            root.AddComponent<PeanutSaveGameService>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 4; i++) yield return null;
            BindSystems();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                yield break;
            }

            if (HasMainSave)
            {
                if (!TryLoadFile(MainSavePath, false) && HasBackupSave)
                    TryLoadFile(BackupSavePath, true);
            }
            else
            {
                SaveNow(true);
                lastMessage = "기존 PlayerPrefs 진행 데이터를 JSON 저장으로 마이그레이션";
            }

            initialized = true;
        }

        private void BindSystems()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            advancement = FindFirstObjectByType<AdvancementProgressionPrototype>();
            petProgression = FindFirstObjectByType<PetProgressionPrototype>();
            legacyPets = FindFirstObjectByType<IdleSystemsPrototype>();
            shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            firstClear = FindFirstObjectByType<FirstClearRewardPrototype>();
            settings = FindFirstObjectByType<GameSettingsPrototype>();
        }

        private void Update()
        {
            if (!initialized) return;
            saveTimer += Time.unscaledDeltaTime;
            backupTimer += Time.unscaledDeltaTime;
            if (saveTimer < PeanutGameRules.AutoSaveIntervalSeconds) return;
            saveTimer = 0f;
            bool makeBackup = backupTimer >= PeanutGameRules.BackupSaveIntervalSeconds;
            if (makeBackup) backupTimer = 0f;
            SaveNow(makeBackup);
        }

        public void SaveNow(bool createBackup = false)
        {
            if (arena == null || stageFlow == null) return;
            try
            {
                SaveData data = Capture();
                string json = JsonUtility.ToJson(data, true);
                WriteAtomic(MainSavePath, BackupSavePath, json, createBackup);
                lastMessage = createBackup ? "자동 저장 및 백업 완료" : "자동 저장 완료";
            }
            catch (Exception exception)
            {
                lastMessage = "저장 실패 · " + exception.Message;
                Debug.LogException(exception, this);
            }
        }

        public bool TryRestoreBackup()
        {
            if (!HasBackupSave)
            {
                lastMessage = "복구할 백업 파일이 없습니다";
                return false;
            }
            return TryLoadFile(BackupSavePath, true);
        }

        private bool TryLoadFile(string path, bool fromBackup)
        {
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.schemaVersion <= 0)
                    throw new InvalidDataException("저장 데이터 형식이 올바르지 않습니다");
                Validate(data);
                Apply(data);
                lastMessage = fromBackup ? "백업 저장 복구 완료" : "JSON 저장 데이터 불러옴";
                return true;
            }
            catch (Exception exception)
            {
                lastMessage = "저장 불러오기 실패 · " + exception.Message;
                Debug.LogWarning("[PeanutWarrior] " + lastMessage);
                return false;
            }
        }

        private SaveData Capture()
        {
            SaveData data = new SaveData
            {
                schemaVersion = CurrentSchemaVersion,
                savedUtcTicks = DateTime.UtcNow.Ticks
            };

            Type arenaType = typeof(CombatPrototypeArena);
            data.core.gold = ReadLong(arenaType, arena, "gold");
            data.core.fragments = ReadLong(arenaType, arena, "fragments");
            data.core.diamonds = ReadInt(arenaType, arena, "diamonds");
            data.core.lifetimeKills = ReadInt(arenaType, arena, "lifetimeKills");
            data.core.attackLevel = ReadInt(arenaType, arena, "attackLevel", 1);
            data.core.hpLevel = ReadInt(arenaType, arena, "hpLevel", 1);
            data.core.maxMpLevel = ReadInt(arenaType, arena, "maxMpLevel", 1);
            data.core.mpRegenLevel = ReadInt(arenaType, arena, "mpRegenLevel", 1);
            data.core.basicAttackLevel = ReadInt(arenaType, arena, "basicAttackLevel", 1);
            data.core.advancementTier = advancement == null ? ReadInt(arenaType, arena, "advancementTier") : advancement.Tier;
            data.core.petSlotsUnlocked = ReadBool(arenaType, arena, "miniSlotsUnlocked");
            data.core.huntingElement = ReadEnumInt(arenaType, arena, "huntingElement");
            data.core.bossElement = ReadEnumInt(arenaType, arena, "bossElement");

            data.stage.world = stageFlow.World;
            data.stage.stage = stageFlow.Stage;
            data.stage.monsterKills = stageFlow.MonsterKills;
            data.stage.autoChallenge = stageFlow.AutoChallenge;
            data.stage.highestGlobalStage = Mathf.Max(
                PeanutGameRules.ToGlobalStage(stageFlow.World, stageFlow.Stage),
                PlayerPrefs.GetInt(HighestStageKey, 1));

            if (growth != null)
            {
                Type growthType = typeof(GrowthExpansionPrototype);
                data.growth.critChanceLevel = ReadInt(growthType, growth, "critChanceLevel", 1);
                data.growth.critDamageLevel = ReadInt(growthType, growth, "critDamageLevel", 1);
                data.growth.goldGainLevel = ReadInt(growthType, growth, "goldGainLevel", 1);
                data.growth.hpRegenLevel = ReadInt(growthType, growth, "hpRegenLevel", 1);
                data.growth.expGainLevel = ReadInt(growthType, growth, "expGainLevel", 1);
                data.growth.equipmentGainLevel = ReadInt(growthType, growth, "equipmentGainLevel", 1);
                data.growth.playerLevel = growth.PlayerLevel;
                data.growth.currentExperience = growth.CurrentExperience;
                data.growth.equipmentMaterials = growth.EquipmentEnhancementMaterials;
                data.growth.equipmentMaterialProgress = ReadFloat(growthType, growth, "equipmentMaterialProgress");
            }

            if (petProgression != null)
            {
                data.pets.levels = petProgression.GetLevelsCopy();
                data.pets.stars = petProgression.GetStarsCopy();
                data.pets.shards = petProgression.GetDuplicateShardsCopy();
                data.pets.lifetimeHatches = petProgression.GetLifetimeHatchesCopy();
            }
            if (legacyPets != null)
            {
                Type petType = typeof(IdleSystemsPrototype);
                data.pets.eggs = ReadInt(petType, legacyPets, "eggs");
                data.pets.hatchedPets = ReadInt(petType, legacyPets, "hatchedMinis");
                data.pets.incubating = ReadBool(petType, legacyPets, "incubating");
                data.pets.incubationRemaining = ReadFloat(petType, legacyPets, "incubationRemaining");
            }

            if (shop != null)
            {
                Type shopType = typeof(PrototypeShopAndDaily);
                data.shop.dailyStreak = ReadInt(shopType, shop, "dailyStreak");
                data.shop.lastClaimDate = ReadString(shopType, shop, "lastClaimDate");
            }

            if (settings != null)
            {
                data.settings.bgmVolume = settings.BgmVolume;
                data.settings.sfxVolume = settings.SfxVolume;
                data.settings.vibration = settings.VibrationEnabled;
                data.settings.frameRate = settings.TargetFrameRate;
            }

            if (firstClear != null)
            {
                data.clears.bossKills = firstClear.BossKills;
                data.clears.uniqueClears = firstClear.UniqueClears;
            }
            for (int global = 1; global <= data.stage.highestGlobalStage; global++)
                if (PlayerPrefs.GetInt(FirstClearPrefix + global, 0) == 1)
                    data.clears.clearedGlobalStages.Add(global);

            return data;
        }

        private void Validate(SaveData data)
        {
            data.schemaVersion = CurrentSchemaVersion;
            data.core.gold = Math.Max(0L, data.core.gold);
            data.core.fragments = Math.Max(0L, data.core.fragments);
            data.core.diamonds = Mathf.Max(0, data.core.diamonds);
            data.core.lifetimeKills = Mathf.Max(0, data.core.lifetimeKills);
            data.core.attackLevel = Mathf.Max(1, data.core.attackLevel);
            data.core.hpLevel = Mathf.Max(1, data.core.hpLevel);
            data.core.maxMpLevel = Mathf.Max(1, data.core.maxMpLevel);
            data.core.mpRegenLevel = Mathf.Max(1, data.core.mpRegenLevel);
            data.core.basicAttackLevel = Mathf.Max(1, data.core.basicAttackLevel);
            data.core.advancementTier = Mathf.Clamp(data.core.advancementTier, 0, PeanutGameRules.AdvancementCount - 1);
            data.core.huntingElement = Mathf.Clamp(data.core.huntingElement, 0, 3);
            data.core.bossElement = Mathf.Clamp(data.core.bossElement, 0, 3);

            data.stage.world = Mathf.Max(1, data.stage.world);
            data.stage.stage = Mathf.Clamp(data.stage.stage, 1, PeanutGameRules.StagesPerWorld);
            data.stage.monsterKills = Mathf.Clamp(data.stage.monsterKills, 0, PeanutGameRules.RequiredKillsPerStage);
            data.stage.highestGlobalStage = Mathf.Max(
                PeanutGameRules.ToGlobalStage(data.stage.world, data.stage.stage),
                data.stage.highestGlobalStage);

            data.growth.critChanceLevel = Mathf.Clamp(data.growth.critChanceLevel, 1, 49);
            data.growth.critDamageLevel = Mathf.Max(1, data.growth.critDamageLevel);
            data.growth.goldGainLevel = Mathf.Max(1, data.growth.goldGainLevel);
            data.growth.hpRegenLevel = Mathf.Max(1, data.growth.hpRegenLevel);
            data.growth.expGainLevel = Mathf.Max(1, data.growth.expGainLevel);
            data.growth.equipmentGainLevel = Mathf.Max(1, data.growth.equipmentGainLevel);
            data.growth.playerLevel = Mathf.Max(1, data.growth.playerLevel);
            data.growth.currentExperience = Math.Max(0L, data.growth.currentExperience);
            data.growth.equipmentMaterials = Mathf.Max(0, data.growth.equipmentMaterials);
            data.growth.equipmentMaterialProgress = Mathf.Max(0f, data.growth.equipmentMaterialProgress);

            NormalizeArray(ref data.pets.levels, 1, 1, int.MaxValue);
            NormalizeArray(ref data.pets.stars, 1, 1, 5);
            NormalizeArray(ref data.pets.shards, 0, 0, int.MaxValue);
            NormalizeArray(ref data.pets.lifetimeHatches, 0, 0, int.MaxValue);
            data.pets.eggs = Mathf.Max(0, data.pets.eggs);
            data.pets.hatchedPets = Mathf.Max(0, data.pets.hatchedPets);
            data.pets.incubationRemaining = Mathf.Max(0f, data.pets.incubationRemaining);
            data.shop.dailyStreak = Mathf.Clamp(data.shop.dailyStreak, 0, 7);
            data.settings.bgmVolume = Mathf.Clamp01(data.settings.bgmVolume);
            data.settings.sfxVolume = Mathf.Clamp01(data.settings.sfxVolume);
            data.settings.frameRate = data.settings.frameRate <= 30 ? 30 : 60;
            data.clears.bossKills = Mathf.Max(0, data.clears.bossKills);
            data.clears.uniqueClears = Mathf.Max(0, data.clears.uniqueClears);
            if (data.clears.clearedGlobalStages == null) data.clears.clearedGlobalStages = new List<int>();
        }

        private void Apply(SaveData data)
        {
            Type arenaType = typeof(CombatPrototypeArena);
            SetField(arenaType, arena, "gold", data.core.gold);
            SetField(arenaType, arena, "fragments", data.core.fragments);
            SetField(arenaType, arena, "diamonds", data.core.diamonds);
            SetField(arenaType, arena, "lifetimeKills", data.core.lifetimeKills);
            SetField(arenaType, arena, "attackLevel", data.core.attackLevel);
            SetField(arenaType, arena, "hpLevel", data.core.hpLevel);
            SetField(arenaType, arena, "maxMpLevel", data.core.maxMpLevel);
            SetField(arenaType, arena, "mpRegenLevel", data.core.mpRegenLevel);
            SetField(arenaType, arena, "basicAttackLevel", data.core.basicAttackLevel);
            SetEnumField(arenaType, arena, "huntingElement", data.core.huntingElement);
            SetEnumField(arenaType, arena, "bossElement", data.core.bossElement);

            Type flowType = typeof(StageFlowController);
            SetField(flowType, stageFlow, "world", data.stage.world);
            SetField(flowType, stageFlow, "stage", data.stage.stage);
            SetField(flowType, stageFlow, "monsterKills", data.stage.monsterKills);
            SetField(flowType, stageFlow, "autoChallenge", data.stage.autoChallenge);
            FieldInfo phaseField = flowType.GetField("phase", PrivateInstance);
            if (phaseField != null)
            {
                string phaseName = data.stage.monsterKills >= PeanutGameRules.RequiredKillsPerStage ? "BossReady" : "Hunting";
                phaseField.SetValue(stageFlow, Enum.Parse(phaseField.FieldType, phaseName));
            }
            PlayerPrefs.SetInt(HighestStageKey, data.stage.highestGlobalStage);

            if (growth != null)
            {
                Type growthType = typeof(GrowthExpansionPrototype);
                SetField(growthType, growth, "critChanceLevel", data.growth.critChanceLevel);
                SetField(growthType, growth, "critDamageLevel", data.growth.critDamageLevel);
                SetField(growthType, growth, "goldGainLevel", data.growth.goldGainLevel);
                SetField(growthType, growth, "hpRegenLevel", data.growth.hpRegenLevel);
                SetField(growthType, growth, "expGainLevel", data.growth.expGainLevel);
                SetField(growthType, growth, "equipmentGainLevel", data.growth.equipmentGainLevel);
                SetField(growthType, growth, "playerLevel", data.growth.playerLevel);
                SetField(growthType, growth, "currentExperience", data.growth.currentExperience);
                SetField(growthType, growth, "equipmentEnhancementMaterials", data.growth.equipmentMaterials);
                SetField(growthType, growth, "equipmentMaterialProgress", data.growth.equipmentMaterialProgress);
                growth.SaveNow();
            }

            advancement?.RestoreTier(data.core.advancementTier, false);
            if (advancement == null)
            {
                SetField(arenaType, arena, "advancementTier", data.core.advancementTier);
                SetField(arenaType, arena, "miniSlotsUnlocked", data.core.petSlotsUnlocked || data.core.advancementTier >= 2);
            }

            petProgression?.RestoreState(data.pets.levels, data.pets.stars, data.pets.shards, data.pets.lifetimeHatches);
            if (legacyPets != null)
            {
                Type petType = typeof(IdleSystemsPrototype);
                SetField(petType, legacyPets, "eggs", data.pets.eggs);
                SetField(petType, legacyPets, "hatchedMinis", data.pets.hatchedPets);
                SetField(petType, legacyPets, "incubating", data.pets.incubating);
                SetField(petType, legacyPets, "incubationRemaining", data.pets.incubationRemaining);
            }

            if (shop != null)
            {
                Type shopType = typeof(PrototypeShopAndDaily);
                SetField(shopType, shop, "dailyStreak", data.shop.dailyStreak);
                SetField(shopType, shop, "lastClaimDate", data.shop.lastClaimDate ?? string.Empty);
            }

            settings?.SetBgmVolume(data.settings.bgmVolume);
            settings?.SetSfxVolume(data.settings.sfxVolume);
            settings?.SetVibration(data.settings.vibration);
            settings?.SetFrameRate(data.settings.frameRate);

            if (firstClear != null)
            {
                Type firstType = typeof(FirstClearRewardPrototype);
                SetField(firstType, firstClear, "bossKills", data.clears.bossKills);
                SetField(firstType, firstClear, "uniqueClears", data.clears.uniqueClears);
            }
            for (int global = 1; global <= data.stage.highestGlobalStage; global++)
                PlayerPrefs.SetInt(FirstClearPrefix + global, 0);
            for (int i = 0; i < data.clears.clearedGlobalStages.Count; i++)
            {
                int global = data.clears.clearedGlobalStages[i];
                if (global > 0 && global <= data.stage.highestGlobalStage)
                    PlayerPrefs.SetInt(FirstClearPrefix + global, 1);
            }

            MethodInfo restore = arenaType.GetMethod("FullRestore", PrivateInstance);
            restore?.Invoke(arena, null);
            PlayerPrefs.Save();
        }

        private static void WriteAtomic(string mainPath, string backupPath, string content, bool createBackup)
        {
            string directory = Path.GetDirectoryName(mainPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string tempPath = mainPath + ".tmp";
            File.WriteAllText(tempPath, content);
            if (createBackup && File.Exists(mainPath)) File.Copy(mainPath, backupPath, true);
            File.Copy(tempPath, mainPath, true);
            File.Delete(tempPath);
        }

        private static void NormalizeArray(ref int[] values, int fallback, int min, int max)
        {
            int[] normalized = new int[3];
            for (int i = 0; i < normalized.Length; i++)
            {
                int value = values != null && i < values.Length ? values[i] : fallback;
                normalized[i] = Mathf.Clamp(value, min, max);
            }
            values = normalized;
        }

        private static FieldInfo GetField(Type type, string name)
        {
            return type.GetField(name, PrivateInstance);
        }

        private static void SetField(Type type, object target, string name, object value)
        {
            FieldInfo field = GetField(type, name);
            if (field != null && target != null) field.SetValue(target, value);
        }

        private static void SetEnumField(Type type, object target, string name, int value)
        {
            FieldInfo field = GetField(type, name);
            if (field != null && target != null) field.SetValue(target, Enum.ToObject(field.FieldType, value));
        }

        private static int ReadInt(Type type, object target, string name, int fallback = 0)
        {
            FieldInfo field = GetField(type, name);
            return field == null || target == null ? fallback : Convert.ToInt32(field.GetValue(target));
        }

        private static long ReadLong(Type type, object target, string name)
        {
            FieldInfo field = GetField(type, name);
            return field == null || target == null ? 0L : Convert.ToInt64(field.GetValue(target));
        }

        private static float ReadFloat(Type type, object target, string name)
        {
            FieldInfo field = GetField(type, name);
            return field == null || target == null ? 0f : Convert.ToSingle(field.GetValue(target));
        }

        private static bool ReadBool(Type type, object target, string name)
        {
            FieldInfo field = GetField(type, name);
            return field != null && target != null && Convert.ToBoolean(field.GetValue(target));
        }

        private static int ReadEnumInt(Type type, object target, string name)
        {
            FieldInfo field = GetField(type, name);
            return field == null || target == null ? 0 : Convert.ToInt32(field.GetValue(target));
        }

        private static string ReadString(Type type, object target, string name)
        {
            FieldInfo field = GetField(type, name);
            return field == null || target == null ? string.Empty : field.GetValue(target) as string ?? string.Empty;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && initialized) SaveNow(true);
        }

        private void OnApplicationQuit()
        {
            if (initialized) SaveNow(true);
        }
    }
}
