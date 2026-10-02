using UnityEngine;
using UnityEngine.UI;

namespace SpaceAttack
{
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private FeedbackController feedback;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button defaultsButton;
        [SerializeField] private Text title;
        [SerializeField] private Slider[] sliders;
        [SerializeField] private Text[] values;
        private GameController game;

        public void Bind(GameController owner)
        {
            game = owner;
            openButton.onClick.AddListener(game.ToggleSettings);
            closeButton.onClick.AddListener(game.ToggleSettings);
            defaultsButton.onClick.AddListener(feedback.ResetPreferences);
            for (int i = 0; i < sliders.Length; i++)
            {
                FeedbackSetting setting = (FeedbackSetting)i;
                sliders[i].onValueChanged.AddListener(value => feedback.SetPreference(setting, value));
            }
            feedback.PreferencesChanged += Refresh;
            panel.SetActive(false);
        }

        public void Show(bool open)
        {
            panel.SetActive(open);
            if (!open) return;
            title.text = game.State == GameState.Playing ? "SETTINGS  /  PAUSED" : "SETTINGS";
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < sliders.Length; i++)
            {
                float value = feedback.GetPreference((FeedbackSetting)i);
                sliders[i].SetValueWithoutNotify(value);
                values[i].text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        private void OnDestroy()
        {
            if (feedback != null) feedback.PreferencesChanged -= Refresh;
        }
    }
}
