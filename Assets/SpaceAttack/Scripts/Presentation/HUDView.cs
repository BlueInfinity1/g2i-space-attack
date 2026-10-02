using UnityEngine;
using UnityEngine.UI;

namespace SpaceAttack
{
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private Text bestText;
        [SerializeField] private Text stageText;
        [SerializeField] private Text healthText;
        [SerializeField] private RectTransform energyFill;
        [SerializeField] private SettingsView settingsView;
        [SerializeField] private Text statusText;
        [SerializeField] private Text resultsText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;
        private GameController game;
        private float announcementRemaining;

        public void Bind(GameController owner)
        {
            game = owner;
            // Unity's bundled font; no imported artwork or font dependency.
            foreach (Text text in GetComponentsInChildren<Text>(true))
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            startButton.onClick.AddListener(game.StartRun);
            restartButton.onClick.AddListener(game.StartRun);
            settingsView.Bind(game);
        }

        public void Refresh(GameController owner)
        {
            scoreText.text = owner.Score.ToString("D6");
            bestText.text = owner.BestScore.ToString("D6");
            stageText.text = $"STAGE  {Mathf.Max(1, owner.Waves.Stage):00}";
            healthText.text = $"ENERGY   {owner.Player.Health.Current} / {owner.Player.Health.Maximum}";
            healthText.color = owner.Player.Health.Current <= 1 ? new Color(1f, 0.35f, 0.4f) : new Color(0.3f, 0.95f, 0.95f);
            energyFill.anchorMax = new Vector2(Mathf.Clamp01(owner.Player.Health.Current / (float)owner.Player.Health.Maximum), 1f);
            energyFill.GetComponent<Image>().color = healthText.color;
        }

        public void ShowStart()
        {
            startPanel.SetActive(true);
            gameOverPanel.SetActive(false);
            ClearAnnouncement();
        }

        public void ShowRun()
        {
            startPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            ClearAnnouncement();
        }

        public void ShowGameOver(GameController owner)
        {
            startPanel.SetActive(false);
            gameOverPanel.SetActive(true);
            finalScoreText.text = owner.Score.ToString("D6");
            resultsText.text = $"STAGE {owner.Waves.Stage:00} REACHED    /    {owner.Kills} DESTROYED\nSESSION BEST  {owner.BestScore:D6}";
            ClearAnnouncement();
        }

        public void Announce(string message, float duration)
        {
            statusText.text = message;
            announcementRemaining = duration;
        }

        public void ClearAnnouncement()
        {
            statusText.text = "";
            announcementRemaining = 0f;
        }

        public void ShowSettings(bool open) => settingsView.Show(open);

        private void Update()
        {
            if (game == null || game.State != GameState.Playing || announcementRemaining <= 0f) return;
            announcementRemaining -= Time.deltaTime;
            if (announcementRemaining <= 0f) ClearAnnouncement();
        }

        private void OnDestroy()
        {
            if (game == null) return;
            startButton.onClick.RemoveListener(game.StartRun);
            restartButton.onClick.RemoveListener(game.StartRun);
        }
    }
}
