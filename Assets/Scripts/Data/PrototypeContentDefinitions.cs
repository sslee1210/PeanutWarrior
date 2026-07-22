using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeanutWarrior.Data
{
    public enum PrototypeElement
    {
        Neutral,
        Fire,
        Ice,
        Lightning,
        Wind,
        Poison,
        Light,
        Dark
    }

    [Serializable]
    public sealed class PrototypeMonsterDefinition
    {
        public string id = "monster";
        public string displayName = "몬스터";
        public float baseHp = 30f;
        public float attack = 4f;
        public float moveSpeed = 2f;
        public float attackRange = 0.8f;
        public float aggroRange = 3f;
        public int goldReward = 2;
        public bool boss;
    }

    [Serializable]
    public sealed class PrototypeSkillDefinition
    {
        public string id = "skill";
        public string displayName = "스킬";
        public float damageMultiplier = 1.5f;
        public float cooldown = 5f;
        public float mpCost = 20f;
        public int hitCount = 1;
        public float range = 2f;
        public bool bossSkill;
    }

    [Serializable]
    public sealed class PrototypeSwordDefinition
    {
        public string id = "sword";
        public string displayName = "검";
        public PrototypeElement element;
        public int rarity = 1;
        public float attackMultiplier = 1f;
        public float statusPower = 1f;
    }

    [Serializable]
    public sealed class PrototypeAdvancementDefinition
    {
        public int tier;
        public string displayName = "전직";
        public int requiredGlobalStage;
        public int requiredCombatPower;
        public long requiredGold;
        public int requiredDiamonds;
        public float statMultiplier = 1f;
        public int basicAttackHits = 1;
        public bool unlockMinis;
    }

    [CreateAssetMenu(fileName = "PrototypeContentDatabase", menuName = "Peanut Warrior/Prototype Content Database")]
    public sealed class PrototypeContentDatabase : ScriptableObject
    {
        public List<PrototypeMonsterDefinition> monsters = new List<PrototypeMonsterDefinition>();
        public List<PrototypeSkillDefinition> skills = new List<PrototypeSkillDefinition>();
        public List<PrototypeSwordDefinition> swords = new List<PrototypeSwordDefinition>();
        public List<PrototypeAdvancementDefinition> advancements = new List<PrototypeAdvancementDefinition>();

        public PrototypeMonsterDefinition FindMonster(string id)
        {
            return monsters.Find(item => string.Equals(item.id, id, StringComparison.Ordinal));
        }

        public PrototypeSkillDefinition FindSkill(string id)
        {
            return skills.Find(item => string.Equals(item.id, id, StringComparison.Ordinal));
        }

        public PrototypeSwordDefinition FindSword(string id)
        {
            return swords.Find(item => string.Equals(item.id, id, StringComparison.Ordinal));
        }
    }
}
