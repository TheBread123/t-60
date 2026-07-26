using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using T60.StateMachine;

namespace T60.UI
{
    [DisallowMultipleComponent]
    public class GameOverHUDHandler : MonoBehaviour
    {
        [Header("UI Canvas Group References")]
        [Tooltip("Main CanvasGroup for the Game Over HUD panel.")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Tooltip("List of DetailedCardViewHandler instances to fade out when Game Over activates.")]
        [SerializeField] private List<DetailedCardViewHandler> cardDetailHandlersToFade = new List<DetailedCardViewHandler>();

        [Tooltip("List of additional CanvasGroups (e.g., HUD panels, timers) to fade out during Game Over.")]
        [SerializeField] private List<CanvasGroup> canvasGroupsToFade = new List<CanvasGroup>();

        [Tooltip("Duration in seconds for fade transitions.")]
        [SerializeField] private float fadeDuration = 0.4f;

        [Header("Winner Display Settings")]
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private Color playerOneColor = new Color(0.23f, 0.51f, 0.96f, 1f); // Blue
        [SerializeField] private Color playerTwoColor = new Color(0.94f, 0.27f, 0.27f, 1f); // Red
        [SerializeField] private string playerOneName = "PLAYER 1";
        [SerializeField] private string playerTwoName = "PLAYER 2";

        [Header("Win Condition Settings")]
        [SerializeField] private TMP_Text conditionText;
        [SerializeField] private string timerExpiredText = "Timer Ran Out";
        [SerializeField] private string resourceExhaustionText = "0 Cards Remaining";
        [SerializeField] private string immediateVictoryText = "Instant Victory";
        [SerializeField] private string defaultWinText = "Match Victory";

        [Header("Restart Controls")]
        [SerializeField] private Button restartButton;

        private Coroutine activeFadeRoutine;
        private readonly List<Coroutine> activeGroupFadeRoutines = new List<Coroutine>();

        public CanvasGroup PanelCanvasGroup => panelCanvasGroup;
        public TMP_Text WinnerText => winnerText;
        public TMP_Text ConditionText => conditionText;
        public Button RestartButton => restartButton;

        public Color PlayerOneColor { get => playerOneColor; set => playerOneColor = value; }
        public Color PlayerTwoColor { get => playerTwoColor; set => playerTwoColor = value; }
        public string PlayerOneName { get => playerOneName; set => playerOneName = value; }
        public string PlayerTwoName { get => playerTwoName; set => playerTwoName = value; }

        public string TimerExpiredText { get => timerExpiredText; set => timerExpiredText = value; }
        public string ResourceExhaustionText { get => resourceExhaustionText; set => resourceExhaustionText = value; }
        public string ImmediateVictoryText { get => immediateVictoryText; set => immediateVictoryText = value; }
        public string DefaultWinText { get => defaultWinText; set => defaultWinText = value; }

        private void Awake()
        {
            EnsureCanvasGroup();
            AutoCollectReferences();
            SetupButtonListeners();
            SetPanelAlphaImmediate(0f);
        }

        private void OnEnable()
        {
            GameOverState.OnGameOverEntered += HandleGameOverEntered;
            GameOverState.OnGameOverExited += HandleGameOverExited;
        }

        private void OnDisable()
        {
            GameOverState.OnGameOverEntered -= HandleGameOverEntered;
            GameOverState.OnGameOverExited -= HandleGameOverExited;
        }

        public void EnsureCanvasGroup()
        {
            if (panelCanvasGroup == null)
            {
                if (!TryGetComponent<CanvasGroup>(out panelCanvasGroup))
                {
                    panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        public void AutoCollectReferences()
        {
            if (cardDetailHandlersToFade == null) cardDetailHandlersToFade = new List<DetailedCardViewHandler>();
            if (cardDetailHandlersToFade.Count == 0)
            {
#if UNITY_2023_1_OR_NEWER
                var views = FindObjectsByType<DetailedCardViewHandler>(FindObjectsSortMode.None);
#else
                var views = FindObjectsOfType<DetailedCardViewHandler>();
#endif
                cardDetailHandlersToFade.AddRange(views);
            }

            if (restartButton == null)
            {
                restartButton = GetComponentInChildren<Button>(true);
            }
        }

        private void SetupButtonListeners()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartButtonClicked);
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }
        }

        private void HandleGameOverEntered(MatchContext context)
        {
            ShowGameOverHUD(context);
        }

        private void HandleGameOverExited()
        {
            HideGameOverHUD();
        }

        public void ShowGameOverHUD(MatchContext context)
        {
            if (context == null) return;

            // 1. Update Winner Text & Color
            if (winnerText != null)
            {
                bool isP1Winner = (context.WinnerPlayerIndex == 0);
                winnerText.text = isP1Winner ? playerOneName : playerTwoName;
                winnerText.color = isP1Winner ? playerOneColor : playerTwoColor;
            }

            // 2. Update Win Condition Text
            if (conditionText != null)
            {
                switch (context.WinReason)
                {
                    case GameOverReason.TimerExpired:
                        conditionText.text = timerExpiredText;
                        break;
                    case GameOverReason.ResourceExhaustion:
                        conditionText.text = resourceExhaustionText;
                        break;
                    case GameOverReason.ImmediateVictory:
                        conditionText.text = immediateVictoryText;
                        break;
                    default:
                        conditionText.text = defaultWinText;
                        break;
                }
            }

            // 3. Fade in main Game Over HUD panel
            FadeCanvasGroup(panelCanvasGroup, 1f, fadeDuration, true);

            // 4. Fade out card detail handlers
            if (cardDetailHandlersToFade != null)
            {
                foreach (var handler in cardDetailHandlersToFade)
                {
                    if (handler != null)
                    {
                        handler.FadeTo(0f, fadeDuration);
                    }
                }
            }

            // 5. Fade out extra HUD canvas groups
            if (canvasGroupsToFade != null)
            {
                foreach (var cg in canvasGroupsToFade)
                {
                    if (cg != null && cg != panelCanvasGroup)
                    {
                        FadeCanvasGroup(cg, 0f, fadeDuration, false);
                    }
                }
            }
        }

        public void HideGameOverHUD()
        {
            // Fade out main panel
            FadeCanvasGroup(panelCanvasGroup, 0f, fadeDuration, false);

            // Fade back in card detail handlers
            if (cardDetailHandlersToFade != null)
            {
                foreach (var handler in cardDetailHandlersToFade)
                {
                    if (handler != null)
                    {
                        handler.FadeTo(1f, fadeDuration);
                    }
                }
            }

            // Fade back in extra HUD canvas groups
            if (canvasGroupsToFade != null)
            {
                foreach (var cg in canvasGroupsToFade)
                {
                    if (cg != null && cg != panelCanvasGroup)
                    {
                        FadeCanvasGroup(cg, 1f, fadeDuration, true);
                    }
                }
            }
        }

        public void OnRestartButtonClicked()
        {
            Debug.Log("[GameOverHUDHandler] Restart Game button clicked.");
            HideGameOverHUD();

            MatchStateMachineRunner runner = null;
            if (GameManager.Instance != null && GameManager.Instance.MatchRunner != null)
            {
                runner = GameManager.Instance.MatchRunner;
            }
            else
            {
#if UNITY_2023_1_OR_NEWER
                runner = FindFirstObjectByType<MatchStateMachineRunner>();
#else
                runner = FindObjectOfType<MatchStateMachineRunner>();
#endif
            }

            if (runner != null)
            {
                runner.RestartMatch();
            }
            else
            {
                Debug.LogWarning("[GameOverHUDHandler] MatchStateMachineRunner not found! Cannot restart match.");
            }
        }

        private readonly Dictionary<CanvasGroup, Coroutine> runningFadeRoutines = new Dictionary<CanvasGroup, Coroutine>();

        public void SetPanelAlphaImmediate(float alpha)
        {
            EnsureCanvasGroup();
            if (panelCanvasGroup != null)
            {
                if (!panelCanvasGroup.gameObject.activeSelf)
                {
                    panelCanvasGroup.gameObject.SetActive(true);
                }
                float clamped = Mathf.Clamp01(alpha);
                panelCanvasGroup.alpha = clamped;
                panelCanvasGroup.interactable = clamped > 0.99f;
                panelCanvasGroup.blocksRaycasts = clamped > 0.01f;
            }
        }

        public void FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration, bool enableRaycastsOnComplete)
        {
            if (cg == null) return;

            if (targetAlpha > 0f && !cg.gameObject.activeSelf)
            {
                cg.gameObject.SetActive(true);
            }

            if (runningFadeRoutines.TryGetValue(cg, out Coroutine active) && active != null)
            {
                StopCoroutine(active);
                runningFadeRoutines.Remove(cg);
            }

            if (duration <= 0f)
            {
                float clamped = Mathf.Clamp01(targetAlpha);
                cg.alpha = clamped;
                cg.blocksRaycasts = enableRaycastsOnComplete && clamped > 0.01f;
                cg.interactable = enableRaycastsOnComplete && clamped > 0.99f;
                return;
            }

            runningFadeRoutines[cg] = StartCoroutine(CanvasGroupFadeRoutine(cg, targetAlpha, duration, enableRaycastsOnComplete));
        }

        private IEnumerator CanvasGroupFadeRoutine(CanvasGroup cg, float targetAlpha, float duration, bool enableRaycastsOnComplete)
        {
            if (cg == null) yield break;

            float startAlpha = cg.alpha;
            float elapsed = 0f;
            float clampedTarget = Mathf.Clamp01(targetAlpha);

            while (elapsed < duration)
            {
                if (cg == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(startAlpha, clampedTarget, t);
                cg.blocksRaycasts = cg.alpha > 0.01f;
                yield return null;
            }

            if (cg != null)
            {
                cg.alpha = clampedTarget;
                cg.blocksRaycasts = enableRaycastsOnComplete && clampedTarget > 0.01f;
                cg.interactable = enableRaycastsOnComplete && clampedTarget > 0.99f;
            }

            if (cg != null)
            {
                runningFadeRoutines.Remove(cg);
            }
        }
    }
}
