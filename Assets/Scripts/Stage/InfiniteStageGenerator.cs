using System;

namespace PeanutWarrior.Stage
{
    public static class InfiniteStageGenerator
    {
        private static readonly string[] BaseWorldNames =
        {
            "땅콩밭 침공", "곰팡이 창고", "포식자의 숲", "얼어붙은 저장고",
            "불타는 이세계", "차원 균열 중심부"
        };

        public static StageDefinition Generate(int globalStageIndex)
        {
            globalStageIndex = Math.Max(1, globalStageIndex);
            int zeroBased = globalStageIndex - 1;
            int worldSequence = zeroBased / 30;
            int stageNumber = zeroBased % 30 + 1;
            int baseWorldIndex = worldSequence % BaseWorldNames.Length;
            int cycle = worldSequence / BaseWorldNames.Length;
            int worldNumber = worldSequence + 1;
            string prefix = cycle == 0 ? string.Empty : $"강화된 {cycle}단계 ";
            string worldName = prefix + BaseWorldNames[baseWorldIndex];

            float growth = (float)Math.Pow(1.12, zeroBased);
            float bossGrowth = growth * (1.8f + stageNumber * 0.02f);

            return new StageDefinition(
                globalStageIndex,
                worldNumber,
                stageNumber,
                cycle,
                worldName,
                100,
                growth,
                growth * 0.85f,
                bossGrowth
            );
        }
    }
}
