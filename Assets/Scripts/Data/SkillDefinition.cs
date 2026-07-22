using System;
using UnityEngine;

namespace PeanutWarrior.Data
{
    [Serializable]
    public class SkillDefinition
    {
        public string skillId;
        public string displayName;
        public SkillLoadoutType loadoutType;
        public int upgradeLevel = 1;
        public int evolutionLevel = 0;
        public float mpCost = 10f;
        public float cooldown = 3f;
        public float baseDamageMultiplier = 1f;
        public int baseHitCount = 1;
        public int baseTargetCount = 1;
        public float baseRange = 1f;

        public int HitCount => baseHitCount + evolutionLevel;
        public int TargetCount => baseTargetCount + evolutionLevel * 2;
        public float Range => baseRange + evolutionLevel * 0.15f;
        public float DamageMultiplier => baseDamageMultiplier * (1f + upgradeLevel * 0.1f + evolutionLevel * 0.25f);
    }
}
