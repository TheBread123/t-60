using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using T60.Audio;

namespace T60.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene To Load")]
        [SerializeField] private string mainSceneName = "MainScene";

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject cardsPanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button cardsButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button creditsButton;

        [Header("Back Buttons")]
        [SerializeField] private Button cardsBackButton;
        [SerializeField] private Button howToPlayBackButton;
        [SerializeField] private Button creditsBackButton;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (cardsButton != null) cardsButton.onClick.AddListener(() => { PlayClickSfx(); ShowPanel(cardsPanel); });
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(() => { PlayClickSfx(); ShowPanel(howToPlayPanel); });
            if (creditsButton != null) creditsButton.onClick.AddListener(() => { PlayClickSfx(); ShowPanel(creditsPanel); });

            if (cardsBackButton != null) cardsBackButton.onClick.AddListener(() => { PlayClickSfx(); ShowPanel(mainMenuPanel); });
            if (howToPlayBackButton != null) howToPlayBackButton.onClick.AddListener(() => { PlayClickSfx(); ShowPanel(mainMenuPanel); });
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(() => { PlayClickSfx(); ShowPanel(mainMenuPanel); });
        }

        private void Start()
        {
            ShowPanel(mainMenuPanel);
        }

        private void OnPlayClicked()
        {
            PlayClickSfx();
            SceneManager.LoadScene(mainSceneName);
        }

        private void ShowPanel(GameObject target)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenuPanel == target);
            if (cardsPanel != null) cardsPanel.SetActive(cardsPanel == target);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(howToPlayPanel == target);
            if (creditsPanel != null) creditsPanel.SetActive(creditsPanel == target);
        }

        private void PlayClickSfx()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayRandomSfx();
            }
        }
    }
}
