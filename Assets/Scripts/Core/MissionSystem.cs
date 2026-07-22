using System;
using System.Collections.Generic;

namespace PeanutWarrior.Core
{
    [Serializable]
    public class MissionProgress
    {
        public string missionId;
        public string description;
        public long current;
        public long target;
        public long diamondReward;
        public bool claimed;
    }

    public class MissionSystem
    {
        public readonly List<MissionProgress> Missions = new();

        public bool Claim(string missionId)
        {
            MissionProgress mission = Missions.Find(x => x.missionId == missionId);
            if (mission == null || mission.claimed || mission.current < mission.target) return false;
            mission.claimed = true;
            GameManager.Instance.AddDiamonds(mission.diamondReward);
            return true;
        }
    }
}
