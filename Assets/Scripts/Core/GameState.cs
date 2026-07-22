using System;
using System.Collections.Generic;
using PeanutWarrior.Data;

namespace PeanutWarrior.Core
{
    [Serializable]
    public class GameState
    {
        public long gold;
        public long diamonds;
        public long skillFragments;
        public int currentStageIndex = 1;
        public int highestUnlockedStageIndex = 1;
        public int jobTier;
        public bool autoChallenge;
        public PlayerStats playerStats = new();
        public WeaponDefinition huntingWeapon;
        public WeaponDefinition bossWeapon;
        public List<SkillDefinition> huntingSkills = new();
        public List<SkillDefinition> bossSkills = new();
        public List<CompanionDefinition> companions = new();
        public string[] huntingCompanionSlots = new string[3];
        public string[] bossCompanionSlots = new string[3];
    }
}
