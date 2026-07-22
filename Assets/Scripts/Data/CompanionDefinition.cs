using System;

namespace PeanutWarrior.Data
{
    [Serializable]
    public class CompanionDefinition
    {
        public string companionId;
        public string displayName;
        public ElementType element;
        public int attackLevel = 1;
        public int criticalChanceLevel = 1;
        public int criticalDamageLevel = 1;
        public int hatchSeconds = 3600;
        public bool isHatched;

        public float Attack => 5f * attackLevel;
        public float CriticalChance => Math.Clamp(criticalChanceLevel * 0.005f, 0f, 1f);
        public float CriticalDamage => 1.5f + criticalDamageLevel * 0.05f;
    }
}
