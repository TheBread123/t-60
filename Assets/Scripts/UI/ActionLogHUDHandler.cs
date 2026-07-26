using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace T60.UI
{
    [DisallowMultipleComponent]
    public class ActionLogHUDHandler : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text logTextContainer;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Log Settings")]
        [SerializeField] private int maxLogEntries = 5;
        [SerializeField] private float autoFadeDelaySeconds = 4f;

        [Header("Colors & Styling")]
        [SerializeField] private Color player1Color = new Color(0f, 0.9f, 1f);       // Cyan
        [SerializeField] private Color player2Color = new Color(1f, 0.44f, 0.26f);   // Coral/Orange
        [SerializeField] private Color systemColor = new Color(0.9f, 0.9f, 0.9f);   // Off-white
        [SerializeField] private Color firewallBlockColor = new Color(1f, 0.84f, 0f); // Gold

        // Each entry stores the formatted body line and which turn it belongs to
        private readonly List<(int turnNumber, string line)> logEntries = new List<(int, string)>();
        private Coroutine activeFadeCoroutine;
        private Coroutine activeFadeInCoroutine;

        public CanvasGroup CanvasGroup => canvasGroup;

        private void Awake()
        {
            if (logTextContainer == null)
            {
                logTextContainer = GetComponent<TMP_Text>();
            }

            // Clear any placeholder text set in the inspector
            if (logTextContainer != null)
            {
                logTextContainer.text = string.Empty;
            }

            EnsureCanvasGroup();

            // Start invisible
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                if (!TryGetComponent<CanvasGroup>(out canvasGroup))
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void OnEnable()
        {
            ActionLogManager.OnLogAdded += HandleLogAdded;
            ActionLogManager.OnLogCleared += HandleLogCleared;
        }

        private void OnDisable()
        {
            ActionLogManager.OnLogAdded -= HandleLogAdded;
            ActionLogManager.OnLogCleared -= HandleLogCleared;
        }

        private void HandleLogCleared()
        {
            if (activeFadeCoroutine != null)
            {
                StopCoroutine(activeFadeCoroutine);
                activeFadeCoroutine = null;
            }

            logEntries.Clear();
            RebuildLogDisplay();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void HandleLogAdded(ActionLogMessage message)
        {
            EnsureCanvasGroup();

            string bodyLine = FormatBody(message);
            logEntries.Add((message.TurnNumber, bodyLine));

            while (logEntries.Count > maxLogEntries)
            {
                logEntries.RemoveAt(0);
            }

            RebuildLogDisplay();

            // Fade in only if the panel isn't already fully visible
            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                // Cancel any running fade-out so it doesn't fight the fade-in
                if (activeFadeCoroutine != null)
                {
                    StopCoroutine(activeFadeCoroutine);
                    activeFadeCoroutine = null;
                }

                if (activeFadeInCoroutine != null)
                {
                    StopCoroutine(activeFadeInCoroutine);
                }

                float fadeInDuration = UIManager.Instance != null
                    ? UIManager.Instance.AnnouncementFadeInDuration
                    : 0.2f;

                activeFadeInCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, fadeInDuration, () =>
                {
                    activeFadeInCoroutine = null;
                    // Start the fade-out timer only after the fade-in completes
                    if (autoFadeDelaySeconds > 0f)
                    {
                        activeFadeCoroutine = StartCoroutine(FadeLogAfterDelay(autoFadeDelaySeconds));
                    }
                }));
                return;
            }

            // Panel is already visible — just reset the fade-out timer
            if (activeFadeCoroutine != null)
            {
                StopCoroutine(activeFadeCoroutine);
            }

            if (autoFadeDelaySeconds > 0f)
            {
                activeFadeCoroutine = StartCoroutine(FadeLogAfterDelay(autoFadeDelaySeconds));
            }
        }

        /// <summary>
        /// Builds the full display string. Turn headers are printed once per run of
        /// consecutive entries sharing the same turn number; entries within that turn
        /// are indented beneath the header.
        /// </summary>
        private void RebuildLogDisplay()
        {
            if (logTextContainer == null) return;

            var sb = new System.Text.StringBuilder();
            int lastTurn = -1;

            for (int i = 0; i < logEntries.Count; i++)
            {
                int turn = logEntries[i].turnNumber;
                string line = logEntries[i].line;

                // Emit a turn header when the turn number changes
                if (turn != lastTurn)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    string turnLabel = turn > 0 ? $"Turn {turn}" : "—";
                    sb.AppendLine($"<color=#555555>— {turnLabel} —</color>");
                    lastTurn = turn;
                }

                sb.Append("  "); // indent
                sb.AppendLine(line);
            }

            logTextContainer.text = sb.ToString().TrimEnd();
        }

        /// <summary>Returns just the body text for a log entry (no turn prefix).</summary>
        private string FormatBody(ActionLogMessage msg)
        {
            string playerColorHex = PlayerColorHex(msg.PlayerIndex);

            switch (msg.EntryType)
            {
                case LogEntryType.EffectBlocked:
                    string blockHex = ColorUtility.ToHtmlStringRGB(firewallBlockColor);
                    return $"<color=#{blockHex}>🛡️</color> <color=#{playerColorHex}>{msg.Text}</color>";

                case LogEntryType.CardPlayed:
                    return $"<color=#{playerColorHex}>{msg.Text}</color>";

                case LogEntryType.TurnStart:
                    return $"⚡ <color=#{playerColorHex}>{msg.Text}</color>";

                default:
                    string sysHex = ColorUtility.ToHtmlStringRGB(systemColor);
                    return $"<color=#{sysHex}>{msg.Text}</color>";
            }
        }

        private string PlayerColorHex(int playerIndex)
        {
            if (playerIndex == 0) return ColorUtility.ToHtmlStringRGB(player1Color);
            if (playerIndex == 1) return ColorUtility.ToHtmlStringRGB(player2Color);
            return ColorUtility.ToHtmlStringRGB(systemColor);
        }

        private IEnumerator FadeLogAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            float fadeOutDuration = UIManager.Instance != null
                ? UIManager.Instance.AnnouncementFadeOutDuration
                : 0.35f;

            if (canvasGroup != null && fadeOutDuration > 0f)
            {
                activeFadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, fadeOutDuration, () =>
                {
                    logEntries.Clear();
                    RebuildLogDisplay();
                }));
            }
            else
            {
                logEntries.Clear();
                RebuildLogDisplay();
                if (canvasGroup != null) canvasGroup.alpha = 0f;
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration, System.Action onComplete = null)
        {
            float startAlpha = cg.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            cg.alpha = targetAlpha;
            activeFadeCoroutine = null;
            onComplete?.Invoke();
        }

        public void ClearLog()
        {
            logEntries.Clear();
            RebuildLogDisplay();
        }
    }
}
