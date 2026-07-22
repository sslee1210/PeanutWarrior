using System.Collections;
using PeanutWarrior.Core;
using PeanutWarrior.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    public sealed class PrototypeGame : MonoBehaviour
    {
        [Header("Prototype balance")]
        [SerializeField] private float playerMaxHp = 125f;
        [SerializeField] private float playerAttack = 20f;
        [SerializeField] private float attackInterval = 0.35f;
        [SerializeField] private float spawnInterval = 0.25f;

        private StageManager stage;
        private PrototypeEnemy enemy;
        private float playerHp;
        private float attackTimer;
        private bool spawning;

        private Text statusText;
        private Text hpText;
        private Text killText;
        private Button bossButton;
        private Toggle autoToggle;

        public bool IsPlayerDead => playerHp <= 0f;

        private IEnumerator Start()
        {
            yield return null;
            stage = StageManager.Instance;
            playerHp = playerMaxHp;
            BuildUi();
            StartCoroutine(SpawnLoop());
        }

        private void Update()
        {
            if (stage == null || IsPlayerDead) return;
            attackTimer -= Time.deltaTime;
            if (enemy == null || attackTimer > 0f) return;
            attackTimer = attackInterval;
            enemy.TakeDamage(playerAttack);
            RefreshUi();
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                if (stage == null || IsPlayerDead || enemy != null) continue;
                if (stage.Phase == PeanutWarrior.Data.BattlePhase.Boss)
                {
                    SpawnEnemy(true);
                    continue;
                }
                SpawnEnemy(false);
            }
        }

        private void SpawnEnemy(bool boss)
        {
            var data = stage.CurrentStage;
            GameObject go = new GameObject(boss ? "Prototype Boss" : "Prototype Monster");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeVisuals.WhiteSprite;
            go.transform.position = new Vector3(2.4f, 0.2f, 0f);
            PrototypeEnemy created = go.AddComponent<PrototypeEnemy>();
            float hp = boss ? 250f * data.bossHpMultiplier : 18f * data.enemyHpMultiplier;
            float atk = boss ? 9f * data.enemyAttackMultiplier : 2.5f * data.enemyAttackMultiplier;
            created.Initialize(this, boss, hp, atk);
            enemy = created;
            RefreshUi();
        }

        public void DamagePlayer(float amount)
        {
            playerHp = Mathf.Max(0f, playerHp - Mathf.Max(0f, amount));
            if (playerHp <= 0f)
            {
                bool wasBoss = stage.Phase == PeanutWarrior.Data.BattlePhase.Boss;
                stage.HandlePlayerDeath();
                playerHp = playerMaxHp;
                if (enemy != null) Destroy(enemy.gameObject);
                enemy = null;
                statusText.text = wasBoss
                    ? "보스전 패배: 현재 스테이지에서 0/100부터 다시 사냥"
                    : "사냥 중 패배: 이전 스테이지로 이동";
            }
            RefreshUi();
        }

        public void OnEnemyDefeated(PrototypeEnemy defeated)
        {
            enemy = null;
            if (defeated.IsBoss)
            {
                stage.DefeatBoss(100, 5, 1);
                playerHp = playerMaxHp;
                statusText.text = "보스 처치!";
            }
            else
            {
                stage.RegisterMonsterKill(2, 1);
            }
            RefreshUi();
        }

        private void EnterBoss()
        {
            if (!stage.BossReady) return;
            if (enemy != null) Destroy(enemy.gameObject);
            enemy = null;
            playerHp = playerMaxHp;
            stage.StartBossBattle();
            statusText.text = "보스전 시작: HP/MP 완전 회복";
            RefreshUi();
        }

        private void BuildUi()
        {
            Canvas canvas = new GameObject("Prototype Canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText = CreateText(canvas.transform, font, new Vector2(0f, 260f), new Vector2(900f, 60f), 28);
            hpText = CreateText(canvas.transform, font, new Vector2(-310f, 210f), new Vector2(280f, 50f), 24);
            killText = CreateText(canvas.transform, font, new Vector2(0f, 210f), new Vector2(360f, 50f), 24);

            bossButton = CreateButton(canvas.transform, font, "보스 도전", new Vector2(0f, -240f));
            bossButton.onClick.AddListener(EnterBoss);

            autoToggle = CreateToggle(canvas.transform, font, new Vector2(250f, -240f));
            autoToggle.isOn = GameManager.Instance.State.autoChallenge;
            autoToggle.onValueChanged.AddListener(stage.SetAutoChallenge);

            RefreshUi();
        }

        private void RefreshUi()
        {
            if (stage == null || statusText == null) return;
            var data = stage.CurrentStage;
            hpText.text = $"HP {playerHp:0}/{playerMaxHp:0}";
            killText.text = $"{data.worldName}  {data.worldNumber}-{data.stageNumber}   몬스터 {stage.CurrentKills}/{data.requiredKills}";
            bossButton.interactable = stage.BossReady && stage.Phase == PeanutWarrior.Data.BattlePhase.Hunting;
            autoToggle.SetIsOnWithoutNotify(GameManager.Instance.State.autoChallenge);
            if (string.IsNullOrWhiteSpace(statusText.text)) statusText.text = "자동 사냥 중";
        }

        private static Text CreateText(Transform parent, Font font, Vector2 pos, Vector2 size, int fontSize)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(Transform parent, Font font, string label, Vector2 pos)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220f, 70f);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.7f, 0.42f, 0.12f);
            Button button = go.AddComponent<Button>();
            Text text = CreateText(go.transform, font, Vector2.zero, rect.sizeDelta, 25);
            text.text = label;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, Font font, Vector2 pos)
        {
            GameObject root = new GameObject("AutoChallengeToggle");
            root.transform.SetParent(parent, false);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(260f, 70f);
            Toggle toggle = root.AddComponent<Toggle>();

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchoredPosition = new Vector2(-95f, 0f);
            bgRect.sizeDelta = new Vector2(42f, 42f);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = Color.white;

            GameObject check = new GameObject("Checkmark");
            check.transform.SetParent(bg.transform, false);
            RectTransform checkRect = check.AddComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(28f, 28f);
            Image checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(0.15f, 0.65f, 0.2f);
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;

            Text label = CreateText(root.transform, font, new Vector2(35f, 0f), new Vector2(190f, 60f), 24);
            label.text = "자동도전";
            return toggle;
        }
    }
}
