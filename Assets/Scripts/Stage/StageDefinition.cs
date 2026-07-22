using System;

namespace PeanutWarrior.Stage
{
    [Serializable]
    public readonly struct StageDefinition
    {
        public readonly int globalIndex;
        public readonly int worldNumber;
        public readonly int stageNumber;
        public readonly int cycle;
        public readonly string worldName;
        public readonly int requiredKills;
        public readonly float enemyHpMultiplier;
        public readonly float enemyAttackMultiplier;
        public readonly float bossHpMultiplier;

        public StageDefinition(int globalIndex, int worldNumber, int stageNumber, int cycle, string worldName,
            int requiredKills, float enemyHpMultiplier, float enemyAttackMultiplier, float bossHpMultiplier)
        {
            this.globalIndex = globalIndex;
            this.worldNumber = worldNumber;
            this.stageNumber = stageNumber;
            this.cycle = cycle;
            this.worldName = worldName;
            this.requiredKills = requiredKills;
            this.enemyHpMultiplier = enemyHpMultiplier;
            this.enemyAttackMultiplier = enemyAttackMultiplier;
            this.bossHpMultiplier = bossHpMultiplier;
        }
    }
}
