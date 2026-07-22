using System;
using System.Collections.Generic;

namespace PeanutWarrior.Core
{
    [Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public string title;
        public long current;
        public long target;
        public long diamondReward;
        public bool claimed;
    }

    public class AchievementSystem
    {
        public readonly List<AchievementProgress> Achievements = new();

        public bool Claim(string achievementId)
        {
            AchievementProgress achievement = Achievements.Find(x => x.achievementId == achievementId);
            if (achievement == null || achievement.claimed || achievement.current < achievement.target) return false;
            achievement.claimed = true;
            GameManager.Instance.AddDiamonds(achievement.diamondReward);
            return true;
        }
    }
}
