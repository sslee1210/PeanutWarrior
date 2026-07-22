using System;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(7200)]
    public sealed class PetProgressionPrototype : MonoBehaviour
    {
        public enum PetElement
        {
            Fire = 0,
            Ice = 1,
            Lightning = 2
        }

        private const string Prefix = "PeanutWarrior.Pets.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int PetCount = 3;
        private const int MaxStars = 5;

        private readonly int[] levels = { 1, 1, 1 };
        private readonly int[] stars = { 1, 1, 1 };
        private readonly int[] duplicateShards = new int[PetCount];
        private readonly int[] lifetimeHatches = new int[PetCount];

        private IdleSystemsPrototype legacyPets;
        private AdvancementProgressionPrototype advancement;
        private FieldInfo legacyHatchedField;
        private FieldInfo legacyAttackField;
        private FieldInfo legacyCritField;
        private FieldInfo legacyCritDamageField;
        private FieldInfo legacyMessageField;
        private int observedHatches;
        private string lastMessage = "펫 성장 준비";

        public bool IsUnlocked => advancement != null ? advancement.PetsUnlocked : ReadLegacyUnlock();
        public int TotalHatches => lifetimeHatches[0] + lifetimeHatches[1] + lifetimeHatches[2];
        public int CollectionLevel => levels[0] + levels[1] + levels[2];
        public int CollectionStars => stars[0] + stars[1] + stars[2];
        public float AttackMultiplier => 1f + (CollectionLevel - PetCount) * 0.035f + (CollectionStars - PetCount) * 0.08f;
        public float CriticalChanceBonus => Mathf.Min(0.30f, (CollectionStars - PetCount) * 0.0125f);
        public float CriticalDamageBonus => (CollectionLevel - PetCount) * 0.025f;
        public string LastMessage => lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PetProgressionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPetProgression");
            DontDestroyOnLoad(root);
            root.AddComponent<PetProgressionPrototype>();
        }

        private void Start()
        {
            legacyPets = FindFirstObjectByType<IdleSystemsPrototype>();
            advancement = FindFirstObjectByType<AdvancementProgressionPrototype>();
            if (legacyPets == null)
            {
                enabled = false;
                return;
            }

            Type type = typeof(IdleSystemsPrototype);
            legacyHatchedField = type.GetField("hatchedMinis", PrivateInstance);
            legacyAttackField = type.GetField("miniAttackLevel", PrivateInstance);
            legacyCritField = type.GetField("miniCritLevel", PrivateInstance);
            legacyCritDamageField = type.GetField("miniCritDamageLevel", PrivateInstance);
            legacyMessageField = type.GetField("systemMessage", PrivateInstance);
            Load();
            observedHatches = Mathf.Max(TotalHatches, ReadLegacyHatches());
            SyncLegacyCombatLevels();
        }

        private void Update()
        {
            int current = ReadLegacyHatches();
            if (current <= observedHatches) return;
            int gained = current - observedHatches;
            observedHatches = current;
            for (int i = 0; i < gained; i++) RegisterHatch();
            Save();
        }

        public int GetLevel(PetElement element)
        {
            return levels[(int)element];
        }

        public int GetStars(PetElement element)
        {
            return stars[(int)element];
        }

        public int GetDuplicateShards(PetElement element)
        {
            return duplicateShards[(int)element];
        }

        public int GetRequiredShards(PetElement element)
        {
            int star = GetStars(element);
            return star >= MaxStars ? 0 : star * 3;
        }

        public int GetLifetimeHatches(PetElement element)
        {
            return lifetimeHatches[(int)element];
        }

        public int[] GetLevelsCopy()
        {
            return (int[])levels.Clone();
        }

        public int[] GetStarsCopy()
        {
            return (int[])stars.Clone();
        }

        public int[] GetDuplicateShardsCopy()
        {
            return (int[])duplicateShards.Clone();
        }

        public int[] GetLifetimeHatchesCopy()
        {
            return (int[])lifetimeHatches.Clone();
        }

        public string GetDisplayName(PetElement element)
        {
            switch (element)
            {
                case PetElement.Fire: return "불씨 땅콩";
                case PetElement.Ice: return "서리 땅콩";
                case PetElement.Lightning: return "번개 땅콩";
                default: return "펫";
            }
        }

        public string GetPassiveDescription(PetElement element)
        {
            int level = GetLevel(element);
            int star = GetStars(element);
            switch (element)
            {
                case PetElement.Fire:
                    return $"공격 보조 · Lv.{level} · {star}성";
                case PetElement.Ice:
                    return $"치명타 보조 · Lv.{level} · {star}성";
                case PetElement.Lightning:
                    return $"치명타 피해 보조 · Lv.{level} · {star}성";
                default:
                    return string.Empty;
            }
        }

        public bool SpendGoldToTrain(PetElement element, long availableGold, out long cost)
        {
            int index = (int)element;
            cost = GetTrainingCost(element);
            if (availableGold < cost)
            {
                lastMessage = $"골드 부족 · {cost:N0}G 필요";
                return false;
            }
            levels[index]++;
            SyncLegacyCombatLevels();
            Save();
            lastMessage = $"{GetDisplayName(element)} Lv.{levels[index]} 성장";
            return true;
        }

        public long GetTrainingCost(PetElement element)
        {
            int index = (int)element;
            return 100L * Math.Max(1, levels[index]) * Math.Max(1, stars[index]);
        }

        public void RestoreState(int[] restoredLevels, int[] restoredStars, int[] restoredShards, int[] restoredHatches)
        {
            for (int i = 0; i < PetCount; i++)
            {
                levels[i] = ReadArrayValue(restoredLevels, i, 1, 1, int.MaxValue);
                stars[i] = ReadArrayValue(restoredStars, i, 1, 1, MaxStars);
                duplicateShards[i] = ReadArrayValue(restoredShards, i, 0, 0, int.MaxValue);
                lifetimeHatches[i] = ReadArrayValue(restoredHatches, i, 0, 0, int.MaxValue);
            }
            observedHatches = Mathf.Max(ReadLegacyHatches(), TotalHatches);
            SyncLegacyCombatLevels();
            Save();
            lastMessage = "펫 데이터 복구 완료";
        }

        public void SaveNow()
        {
            Save();
        }

        private void RegisterHatch()
        {
            int index = UnityEngine.Random.Range(0, PetCount);
            lifetimeHatches[index]++;
            duplicateShards[index]++;

            int required = stars[index] * 3;
            if (stars[index] < MaxStars && duplicateShards[index] >= required)
            {
                duplicateShards[index] -= required;
                stars[index]++;
                levels[index] += 2;
                lastMessage = $"{GetDisplayName((PetElement)index)} {stars[index]}성 승급";
            }
            else
            {
                levels[index]++;
                lastMessage = $"{GetDisplayName((PetElement)index)} 부화 · Lv.{levels[index]}";
            }

            SyncLegacyCombatLevels();
            if (legacyMessageField != null) legacyMessageField.SetValue(legacyPets, lastMessage);
        }

        private void SyncLegacyCombatLevels()
        {
            if (legacyPets == null) return;
            int attack = Mathf.Max(1, Mathf.RoundToInt((levels[0] + levels[1] + levels[2]) / 3f));
            int crit = Mathf.Max(1, 1 + CollectionStars - PetCount);
            int critDamage = Mathf.Max(1, 1 + Mathf.FloorToInt((CollectionLevel - PetCount) / 4f));
            legacyAttackField?.SetValue(legacyPets, attack);
            legacyCritField?.SetValue(legacyPets, crit);
            legacyCritDamageField?.SetValue(legacyPets, critDamage);
        }

        private int ReadLegacyHatches()
        {
            return legacyHatchedField == null || legacyPets == null
                ? 0
                : Mathf.Max(0, Convert.ToInt32(legacyHatchedField.GetValue(legacyPets)));
        }

        private bool ReadLegacyUnlock()
        {
            CombatPrototypeArena arena = FindFirstObjectByType<CombatPrototypeArena>();
            if (arena == null) return false;
            FieldInfo field = typeof(CombatPrototypeArena).GetField("miniSlotsUnlocked", PrivateInstance);
            return field != null && Convert.ToBoolean(field.GetValue(arena));
        }

        private void Save()
        {
            for (int i = 0; i < PetCount; i++)
            {
                PlayerPrefs.SetInt(Prefix + "Level." + i, levels[i]);
                PlayerPrefs.SetInt(Prefix + "Stars." + i, stars[i]);
                PlayerPrefs.SetInt(Prefix + "Shards." + i, duplicateShards[i]);
                PlayerPrefs.SetInt(Prefix + "Hatches." + i, lifetimeHatches[i]);
            }
            PlayerPrefs.SetInt(Prefix + "ObservedHatches", observedHatches);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            for (int i = 0; i < PetCount; i++)
            {
                levels[i] = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "Level." + i, 1));
                stars[i] = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Stars." + i, 1), 1, MaxStars);
                duplicateShards[i] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Shards." + i, 0));
                lifetimeHatches[i] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Hatches." + i, 0));
            }
            observedHatches = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "ObservedHatches", TotalHatches));
        }

        private static int ReadArrayValue(int[] values, int index, int fallback, int min, int max)
        {
            if (values == null || index < 0 || index >= values.Length) return fallback;
            return Mathf.Clamp(values[index], min, max);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
