using System;

namespace PeanutWarrior.Data
{
    [Serializable]
    public class PlayerStats
    {
        public int attackLevel = 1;
        public int hpLevel = 1;
        public int criticalChanceLevel = 1;
        public int criticalDamageLevel = 1;
        public int goldGainLevel = 1;
        public int hpRegenLevel = 1;
        public int maxMpLevel = 1;
        public int mpRegenLevel = 1;

        public float CriticalChance => Math.Clamp(criticalChanceLevel * 0.005f, 0f, 1f);
        public float Attack => 10f * attackLevel;
        public float MaxHp => 100f + 25f * hpLevel;
        public float CriticalDamage => 1.5f + 0.05f * criticalDamageLevel;
        public float GoldMultiplier => 1f + 0.03f * goldGainLevel;
        public float HpRegen => 0.25f * hpRegenLevel;
        public float MaxMp => 100f + 10f * maxMpLevel;
        public float MpRegen => 0.5f * mpRegenLevel;
    }
}
