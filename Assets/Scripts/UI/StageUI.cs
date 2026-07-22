using PeanutWarrior.Core;
using PeanutWarrior.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.UI
{
    public class StageUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text worldText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text killText;
        [SerializeField] private Button bossButton;
        [SerializeField] private Toggle autoChallengeToggle;

        private void Start()
        {
            autoChallengeToggle.isOn = GameManager.Instance.State.autoChallenge;
            autoChallengeToggle.onValueChanged.AddListener(StageManager.Instance.SetAutoChallenge);
            bossButton.onClick.AddListener(StageManager.Instance.StartBossBattle);
        }

        private void Update()
        {
            var stage = StageManager.Instance.CurrentStage;
            worldText.text = stage.worldName;
            stageText.text = $"지역 {stage.worldNumber} · 스테이지 {stage.stageNumber}/30";
            killText.text = $"몬스터 {StageManager.Instance.CurrentKills}/{stage.requiredKills}";
            bossButton.interactable = StageManager.Instance.BossReady &&
                                      StageManager.Instance.Phase == PeanutWarrior.Data.BattlePhase.Hunting;
            autoChallengeToggle.SetIsOnWithoutNotify(GameManager.Instance.State.autoChallenge);
        }
    }
}
