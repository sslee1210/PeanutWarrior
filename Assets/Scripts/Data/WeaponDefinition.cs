using System;
using UnityEngine;

namespace PeanutWarrior.Data
{
    [Serializable]
    public class WeaponDefinition
    {
        public string weaponId;
        public string displayName;
        public ElementType element;
        public float criticalDamageBonus;
        [Range(0f, 1f)] public float effectChance = 0.15f;
        public float effectPower = 1f;
        public float effectDuration = 3f;
        public int maxStacks = 5;

        public StatusEffectType StatusEffect => element switch
        {
            ElementType.Fire => StatusEffectType.Burn,
            ElementType.Ice => StatusEffectType.Frostbite,
            ElementType.Lightning => StatusEffectType.Shock,
            ElementType.Wind => StatusEffectType.Bleed,
            ElementType.Poison => StatusEffectType.Poison,
            ElementType.Light => StatusEffectType.HolyMark,
            ElementType.Dark => StatusEffectType.Curse,
            _ => StatusEffectType.Rupture
        };
    }
}
