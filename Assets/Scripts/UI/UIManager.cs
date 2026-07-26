using System;
using System.Collections;
using UnityEngine;
using TMPro;
using T60.StateMachine;

namespace T60.UI
{
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Text References")]
        [SerializeField] private TMP_Text mainTimerText;
        [SerializeField] private TMP_Text mainTimerValueChangeText;
        [SerializeField] private TMP_Text announcementText;
        [SerializeField] private TMP_Text countdownText;

        [Header("Alpha Group Faders")]
        [SerializeField] private AlphaGroupFader mainTimerFader;
        [SerializeField] private AlphaGroupFader mainTimerValueChangeFader;
        [SerializeField] private AlphaGroupFader announcementFader;
        [SerializeField] private AlphaGroupFader countdownFader;

        [Header("Timer Value Change Settings")]
        [SerializeField] private Color timerValueAdditionColor = Color.green;
        [SerializeField] private Color timerValueSubtractionColor = Color.red;
        [SerializeField] private float timerValueChangeDisplayDuration = 1.0f;

        [Header("Fade Settings")]
        [SerializeField] private float announcementFadeInDuration = 0.2f;
        [SerializeField] private float announcementFadeOutDuration = 0.35f;
        [SerializeField] private float timerFadeDuration = 0.3f;

        [Header("Announcement Settings")]
        [SerializeField] private string getReadyText = "GET READY!";
        [SerializeField] private string goText = "GO!";
        [SerializeField] private bool hideAnnouncementOnPrepEnd = true;
        [SerializeField] private string hiddenTimerPlaceholder = "??:??";

        [Header("Optional References")]
        [SerializeField] private GameObject timerJammedVisual;
        [SerializeField] private MatchStateMachineRunner matchRunner;
        [SerializeField] private DetailedCardViewHandler p1DetailedCardView;
        [SerializeField] private DetailedCardViewHandler p2DetailedCardView;
        [SerializeField] private GameOverHUDHandler gameOverHUDHandler;
        [SerializeField] private TurnIndicatorPanelHandler turnIndicatorPanel;
        [SerializeField] private ActionLogHUDHandler actionLogHUDHandler;

        public DetailedCardViewHandler P1DetailedCardView => p1DetailedCardView;
        public DetailedCardViewHandler P2DetailedCardView => p2DetailedCardView;
        public GameOverHUDHandler GameOverHUDHandler => gameOverHUDHandler;
        public TurnIndicatorPanelHandler TurnIndicatorPanel => turnIndicatorPanel;
        public ActionLogHUDHandler ActionLogHUDHandler => actionLogHUDHandler;

        private Coroutine activeAnnouncementCoroutine;
        private Coroutine activeCountdownCoroutine;
        private Coroutine activeTimerValueChangeCoroutine;

        public TMP_Text MainTimerText => mainTimerText;
        public TMP_Text MainTimerValueChangeText => mainTimerValueChangeText;
        public TMP_Text AnnouncementText => announcementText;
        public TMP_Text CountdownText => countdownText;

        public AlphaGroupFader MainTimerFader => mainTimerFader;
        public AlphaGroupFader MainTimerValueChangeFader => mainTimerValueChangeFader;
        public AlphaGroupFader AnnouncementFader => announcementFader;
        public AlphaGroupFader CountdownFader => countdownFader;

        public Color TimerValueAdditionColor { get => timerValueAdditionColor; set => timerValueAdditionColor = value; }
        public Color TimerValueSubtractionColor { get => timerValueSubtractionColor; set => timerValueSubtractionColor = value; }
        public float TimerValueChangeDisplayDuration { get => timerValueChangeDisplayDuration; set => timerValueChangeDisplayDuration = value; }

        public float AnnouncementFadeInDuration { get => announcementFadeInDuration; set => announcementFadeInDuration = value; }
        public float AnnouncementFadeOutDuration { get => announcementFadeOutDuration; set => announcementFadeOutDuration = value; }
        public float TimerFadeDuration { get => timerFadeDuration; set => timerFadeDuration = value; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureFadersInitialized();
            HideInitialTimerValueChangeText();
            ResolveTurnIndicatorPanel();
        }

        private void ResolveTurnIndicatorPanel()
        {
            if (turnIndicatorPanel != null) return;
#if UNITY_2023_1_OR_NEWER
            turnIndicatorPanel = GetComponentInChildren<TurnIndicatorPanelHandler>(true);
            if (turnIndicatorPanel == null) turnIndicatorPanel = FindFirstObjectByType<TurnIndicatorPanelHandler>();
#else
            turnIndicatorPanel = GetComponentInChildren<TurnIndicatorPanelHandler>(true);
            if (turnIndicatorPanel == null) turnIndicatorPanel = FindObjectOfType<TurnIndicatorPanelHandler>();
#endif
        }

        public void SetTurnIndicator(int activePlayerIndex, bool immediate = false)
        {
            if (turnIndicatorPanel != null)
            {
                turnIndicatorPanel.SetTurn(activePlayerIndex, immediate);
            }
        }

        private void HideInitialTimerValueChangeText()
        {
            if (mainTimerValueChangeFader != null)
            {
                mainTimerValueChangeFader.SetAlpha(0f);
            }
            if (mainTimerValueChangeText != null)
            {
                mainTimerValueChangeText.gameObject.SetActive(false);
            }
        }

        private void EnsureFadersInitialized()
        {
            EnsureFaderInitialized(ref mainTimerFader, mainTimerText);
            EnsureFaderInitialized(ref mainTimerValueChangeFader, mainTimerValueChangeText);
            EnsureFaderInitialized(ref announcementFader, announcementText);
            EnsureFaderInitialized(ref countdownFader, countdownText);
        }

        private void EnsureFaderInitialized(ref AlphaGroupFader fader, Component target)
        {
            if (fader != null || target == null) return;

            if (!target.TryGetComponent<AlphaGroupFader>(out fader))
            {
                fader = target.gameObject.AddComponent<AlphaGroupFader>();
            }

            fader.CollectComponents();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
        }

        private void OnEnable()
        {
            DealCardsState.OnDealCardsStarted += HandleDealCardsStarted;
            DealCardsState.OnEnterDelayFinished += HandleDealCardsDelayFinished;

            MatchPreparationState.OnPreparationStarted += HandlePreparationStarted;
            MatchPreparationState.OnCountdownTick += HandleCountdownTick;
            MatchPreparationState.OnCountdownGo += HandleCountdownGo;
            MatchPreparationState.OnPreparationEnded += HandlePreparationEnded;
        }

        private void OnDisable()
        {
            DealCardsState.OnDealCardsStarted -= HandleDealCardsStarted;
            DealCardsState.OnEnterDelayFinished -= HandleDealCardsDelayFinished;

            MatchPreparationState.OnPreparationStarted -= HandlePreparationStarted;
            MatchPreparationState.OnCountdownTick -= HandleCountdownTick;
            MatchPreparationState.OnCountdownGo -= HandleCountdownGo;
            MatchPreparationState.OnPreparationEnded -= HandlePreparationEnded;
        }

        private void Update()
        {
            UpdateMainTimerDisplay();
        }

        #region Timer Updates

        private void UpdateMainTimerDisplay()
        {
            if (mainTimerText == null) return;

            if (matchRunner == null)
            {
                ResolveMatchRunner();
            }

            if (matchRunner == null || matchRunner.Context == null) return;

            var context = matchRunner.Context;

            if (!mainTimerText.gameObject.activeSelf)
            {
                mainTimerText.gameObject.SetActive(true);
            }

            if (context.IsTimerHidden)
            {
                mainTimerText.text = hiddenTimerPlaceholder;
                if (timerJammedVisual != null && !timerJammedVisual.activeSelf)
                {
                    timerJammedVisual.SetActive(true);
                }
                return;
            }

            if (timerJammedVisual != null && timerJammedVisual.activeSelf)
            {
                timerJammedVisual.SetActive(false);
            }

            float rawSeconds = context.MainClockSeconds;
            mainTimerText.text = FormatTimeSeconds(rawSeconds);
        }

        private void ResolveMatchRunner()
        {
            if (GameManager.Instance != null && GameManager.Instance.MatchRunner != null)
            {
                matchRunner = GameManager.Instance.MatchRunner;
                return;
            }

#if UNITY_2023_1_OR_NEWER
            matchRunner = FindFirstObjectByType<MatchStateMachineRunner>();
#else
            matchRunner = FindObjectOfType<MatchStateMachineRunner>();
#endif
        }

        public string FormatTimeSeconds(float totalSeconds)
        {
            float clampedSeconds = Mathf.Max(0f, totalSeconds);
            return Mathf.CeilToInt(clampedSeconds).ToString();
        }

        public void SetMainTimerText(string text)
        {
            if (mainTimerText == null) return;
            mainTimerText.text = text;
        }

        public void FadeMainTimer(float targetAlpha, float duration = -1f, Action onComplete = null)
        {
            EnsureFadersInitialized();
            float dur = duration >= 0f ? duration : timerFadeDuration;
            FadeFader(mainTimerFader, targetAlpha, dur, onComplete);
        }

        public void SetMainTimerAlpha(float alpha)
        {
            EnsureFadersInitialized();
            if (mainTimerFader == null) return;
            mainTimerFader.SetAlpha(alpha);
        }

        public void ShowMainTimerValueChange(float deltaAmount, float displayDuration = -1f, float fadeInDuration = -1f, float fadeOutDuration = -1f)
        {
            if (activeTimerValueChangeCoroutine != null)
            {
                StopCoroutine(activeTimerValueChangeCoroutine);
                activeTimerValueChangeCoroutine = null;
            }

            EnsureFadersInitialized();

            if (mainTimerValueChangeText == null) return;

            string sign = deltaAmount > 0f ? "+" : "";
            float rounded = Mathf.Round(deltaAmount);
            string formattedText = Mathf.Approximately(deltaAmount, rounded)
                ? $"{sign}{(int)rounded}"
                : $"{sign}{deltaAmount:0.#}";

            mainTimerValueChangeText.text = formattedText;
            mainTimerValueChangeText.color = deltaAmount >= 0f ? timerValueAdditionColor : timerValueSubtractionColor;

            if (!mainTimerValueChangeText.gameObject.activeSelf)
            {
                mainTimerValueChangeText.gameObject.SetActive(true);
            }

            float inDur = fadeInDuration >= 0f ? fadeInDuration : announcementFadeInDuration;
            float outDur = fadeOutDuration >= 0f ? fadeOutDuration : announcementFadeOutDuration;
            float dispDur = displayDuration >= 0f ? displayDuration : timerValueChangeDisplayDuration;

            FadeFader(mainTimerValueChangeFader, 1f, inDur);

            if (dispDur <= 0f) return;
            activeTimerValueChangeCoroutine = StartCoroutine(HideTimerValueChangeAfterDelay(dispDur, outDur));
        }

        public void FadeOutMainTimerValueChange(float fadeDuration = -1f, Action onComplete = null)
        {
            if (activeTimerValueChangeCoroutine != null)
            {
                StopCoroutine(activeTimerValueChangeCoroutine);
                activeTimerValueChangeCoroutine = null;
            }

            EnsureFadersInitialized();
            float dur = fadeDuration >= 0f ? fadeDuration : announcementFadeOutDuration;

            if (mainTimerValueChangeFader == null || mainTimerValueChangeText == null || !mainTimerValueChangeText.gameObject.activeInHierarchy || dur <= 0f)
            {
                if (mainTimerValueChangeText != null)
                {
                    mainTimerValueChangeText.gameObject.SetActive(false);
                }
                onComplete?.Invoke();
                return;
            }

            mainTimerValueChangeFader.FadeTo(0f, dur, () =>
            {
                if (mainTimerValueChangeText != null)
                {
                    mainTimerValueChangeText.gameObject.SetActive(false);
                }
                onComplete?.Invoke();
            });
        }

        private IEnumerator HideTimerValueChangeAfterDelay(float delay, float fadeOutDuration)
        {
            yield return new WaitForSecondsRealtime(delay);
            FadeOutMainTimerValueChange(fadeOutDuration);
        }

        #endregion

        #region Generic Fading

        public void FadeFader(AlphaGroupFader fader, float targetAlpha, float duration = -1f, Action onComplete = null)
        {
            if (fader == null)
            {
                onComplete?.Invoke();
                return;
            }

            float dur = duration >= 0f ? duration : announcementFadeInDuration;
            if (dur <= 0f)
            {
                fader.SetAlpha(targetAlpha);
                onComplete?.Invoke();
                return;
            }

            fader.FadeTo(targetAlpha, dur, onComplete);
        }

        #endregion

        #region Announcements and Countdown

        public void SetAnnouncementText(string text)
        {
            if (announcementText == null) return;
            announcementText.text = text;
        }

        public void SetCountdownText(string text)
        {
            if (countdownText == null) return;
            countdownText.text = text;
        }

        public void SetAnnouncementActive(bool active)
        {
            if (announcementText == null) return;
            announcementText.gameObject.SetActive(active);
            EnsureFadersInitialized();
            if (announcementFader == null) return;
            announcementFader.SetAlpha(active ? 1f : 0f);
        }

        public void SetCountdownActive(bool active)
        {
            if (countdownText == null) return;
            countdownText.gameObject.SetActive(active);
            EnsureFadersInitialized();
            if (countdownFader == null) return;
            countdownFader.SetAlpha(active ? 1f : 0f);
        }

        public void ShowAnnouncement(string text, float displayDuration = -1f, float fadeInDuration = -1f, float fadeOutDuration = -1f)
        {
            if (activeAnnouncementCoroutine != null)
            {
                StopCoroutine(activeAnnouncementCoroutine);
                activeAnnouncementCoroutine = null;
            }

            if (countdownText != null && countdownText != announcementText)
            {
                countdownText.gameObject.SetActive(false);
            }

            EnsureFadersInitialized();
            SetAnnouncementText(text);

            if (announcementText == null) return;

            if (!announcementText.gameObject.activeSelf)
            {
                announcementText.gameObject.SetActive(true);
            }

            float inDur = fadeInDuration >= 0f ? fadeInDuration : announcementFadeInDuration;
            float outDur = fadeOutDuration >= 0f ? fadeOutDuration : announcementFadeOutDuration;

            FadeFader(announcementFader, 1f, inDur);

            if (displayDuration <= 0f) return;
            activeAnnouncementCoroutine = StartCoroutine(HideAnnouncementAfterDelay(displayDuration, outDur));
        }

        public void ShowCountdown(string text, float displayDuration = -1f, float fadeInDuration = -1f, float fadeOutDuration = -1f)
        {
            if (activeCountdownCoroutine != null)
            {
                StopCoroutine(activeCountdownCoroutine);
                activeCountdownCoroutine = null;
            }

            EnsureFadersInitialized();

            TMP_Text targetText = countdownText != null ? countdownText : announcementText;
            AlphaGroupFader targetFader = countdownText != null ? countdownFader : announcementFader;

            if (targetText == null) return;

            if (announcementText != null && announcementText != targetText)
            {
                announcementText.gameObject.SetActive(false);
            }

            targetText.text = text;

            if (!targetText.gameObject.activeSelf)
            {
                targetText.gameObject.SetActive(true);
            }

            float inDur = fadeInDuration >= 0f ? fadeInDuration : announcementFadeInDuration;
            float outDur = fadeOutDuration >= 0f ? fadeOutDuration : announcementFadeOutDuration;

            FadeFader(targetFader, 1f, inDur);

            if (displayDuration <= 0f) return;
            activeCountdownCoroutine = StartCoroutine(HideCountdownAfterDelay(displayDuration, outDur));
        }

        public void FadeOutAnnouncement(float fadeDuration = -1f, Action onComplete = null)
        {
            if (activeAnnouncementCoroutine != null)
            {
                StopCoroutine(activeAnnouncementCoroutine);
                activeAnnouncementCoroutine = null;
            }

            EnsureFadersInitialized();
            float dur = fadeDuration >= 0f ? fadeDuration : announcementFadeOutDuration;

            if (announcementFader == null || announcementText == null || !announcementText.gameObject.activeInHierarchy || dur <= 0f)
            {
                SetAnnouncementActive(false);
                onComplete?.Invoke();
                return;
            }

            announcementFader.FadeTo(0f, dur, () =>
            {
                if (announcementText != null)
                {
                    announcementText.gameObject.SetActive(false);
                }
                onComplete?.Invoke();
            });
        }

        public void FadeOutCountdown(float fadeDuration = -1f, Action onComplete = null)
        {
            if (activeCountdownCoroutine != null)
            {
                StopCoroutine(activeCountdownCoroutine);
                activeCountdownCoroutine = null;
            }

            EnsureFadersInitialized();

            TMP_Text targetText = countdownText != null ? countdownText : announcementText;
            AlphaGroupFader targetFader = countdownText != null ? countdownFader : announcementFader;

            float dur = fadeDuration >= 0f ? fadeDuration : announcementFadeOutDuration;

            if (targetFader == null || targetText == null || !targetText.gameObject.activeInHierarchy || dur <= 0f)
            {
                if (targetText == countdownText)
                {
                    SetCountdownActive(false);
                }
                else
                {
                    SetAnnouncementActive(false);
                }
                onComplete?.Invoke();
                return;
            }

            targetFader.FadeTo(0f, dur, () =>
            {
                if (targetText != null)
                {
                    targetText.gameObject.SetActive(false);
                }
                onComplete?.Invoke();
            });
        }

        public void FadeOutAllAnnouncements(float fadeDuration = -1f, Action onComplete = null)
        {
            FadeOutAnnouncement(fadeDuration);
            FadeOutCountdown(fadeDuration, onComplete);
        }

        private IEnumerator HideAnnouncementAfterDelay(float delay, float fadeOutDuration)
        {
            yield return new WaitForSecondsRealtime(delay);
            FadeOutAnnouncement(fadeOutDuration);
        }

        private IEnumerator HideCountdownAfterDelay(float delay, float fadeOutDuration)
        {
            yield return new WaitForSecondsRealtime(delay);
            FadeOutCountdown(fadeOutDuration);
        }

        #endregion

        #region Event Handlers

        private void HandleDealCardsStarted()
        {
            ShowAnnouncement(getReadyText, 1f);
        }

        private void HandleDealCardsDelayFinished()
        {
        }

        private void HandlePreparationStarted()
        {
            SetAnnouncementActive(true);
        }

        private void HandleCountdownTick(int count)
        {
            ShowCountdown(count.ToString(), 0.25f);
        }

        private void HandleCountdownGo()
        {
            ShowCountdown(goText, 1f);
        }

        private void HandlePreparationEnded()
        {
            if (!hideAnnouncementOnPrepEnd) return;
            FadeOutAllAnnouncements(announcementFadeOutDuration);
        }

        #endregion
    }
}
