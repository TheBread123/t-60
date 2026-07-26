using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using T60.StateMachine;

namespace T60.UI
{
    /// <summary>
    /// Accumulates the full action log for the current play session and displays it
    /// in a ScrollRect on the Game Over screen. Cleared automatically on match restart.
    /// Attach this component to the log history panel GameObject (the one that contains
    /// or is the parent of the Scroll View). Wire up the three serialized references
    /// in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class ActionLogHistoryHandler : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The TMP_Text inside the ScrollRect Content object.")]
        [SerializeField] private TMP_Text historyText;

        [Tooltip("The ScrollRect used to scroll the log. Auto-found in children if left empty.")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Colors & Styling")]
        [SerializeField] private Color player1Color = new Color(0f, 0.9f, 1f);       // Cyan
        [SerializeField] private Color player2Color = new Color(1f, 0.44f, 0.26f);   // Coral/Orange
        [SerializeField] private Color systemColor = new Color(0.9f, 0.9f, 0.9f);   // Off-white
        [SerializeField] private Color firewallBlockColor = new Color(1f, 0.84f, 0f); // Gold

        // Full unbounded session history: (turnNumber, formatted body line)
        private readonly List<(int turn, string line)> history = new List<(int, string)>();

        private void Awake()
        {
            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(true);

            if (historyText == null && scrollRect != null)
                historyText = scrollRect.content != null
                    ? scrollRect.content.GetComponentInChildren<TMP_Text>(true)
                    : GetComponentInChildren<TMP_Text>(true);

            if (historyText != null)
                historyText.text = string.Empty;
        }

        private void OnEnable()
        {
            ActionLogManager.OnLogAdded   += HandleLogAdded;
            ActionLogManager.OnLogCleared += HandleLogCleared;
            GameOverState.OnGameOverEntered += HandleGameOverEntered;
            GameOverState.OnGameOverExited  += HandleGameOverExited;
        }

        private void OnDisable()
        {
            ActionLogManager.OnLogAdded   -= HandleLogAdded;
            ActionLogManager.OnLogCleared -= HandleLogCleared;
            GameOverState.OnGameOverEntered -= HandleGameOverEntered;
            GameOverState.OnGameOverExited  -= HandleGameOverExited;
        }

        // ─── Event Handlers ────────────────────────────────────────────────────────

        private void HandleLogAdded(ActionLogMessage message)
        {
            // Accumulate silently — no live display needed
            string body = FormatBody(message);
            history.Add((message.TurnNumber, body));
        }

        private void HandleLogCleared()
        {
            history.Clear();
            if (historyText != null)
                historyText.text = string.Empty;
        }

        private void HandleGameOverEntered(MatchContext _)
        {
            PopulateHistory();
        }

        private void HandleGameOverExited()
        {
            // Nothing to do — the parent GameOverHUDHandler fades the whole panel out.
        }

        // ─── Populate ──────────────────────────────────────────────────────────────

        private void PopulateHistory()
        {
            if (historyText == null) return;

            historyText.text = BuildTranscript();

            StartCoroutine(ScrollToBottomAfterLayout());
        }

        /// <summary>
        /// Waits until the end of the current frame so the ContentSizeFitter has finished
        /// resizing the Content rect before we set the scroll position. Without this wait,
        /// verticalNormalizedPosition is applied while the content is still the old (wrong)
        /// height, the ScrollRect sees nothing to scroll, and elastic movement bounces it back.
        /// </summary>
        private IEnumerator ScrollToBottomAfterLayout()
        {
            // End-of-frame: all CanvasUpdateRegistry passes have completed for this frame
            yield return new WaitForEndOfFrame();

            if (scrollRect != null && scrollRect.content != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }

            // One more frame for the rebuild to propagate through the ScrollRect
            yield return null;

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        // ─── Formatting ────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the full grouped transcript. Identical layout to ActionLogHUDHandler:
        /// a "— Turn N —" header printed once per turn block, body lines indented below.
        /// </summary>
        private string BuildTranscript()
        {
            if (history.Count == 0)
                return "<color=#555555><i>No actions recorded this session.</i></color>";

            var sb = new System.Text.StringBuilder();
            int lastTurn = -1;

            for (int i = 0; i < history.Count; i++)
            {
                int turn = history[i].turn;
                string line = history[i].line;

                if (turn != lastTurn)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    string turnLabel = turn > 0 ? $"Turn {turn}" : "—";
                    sb.AppendLine($"<color=#555555>— {turnLabel} —</color>");
                    lastTurn = turn;
                }

                sb.Append("  ");
                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>Returns the colored body text for a single log entry (no turn prefix).</summary>
        private string FormatBody(ActionLogMessage msg)
        {
            string playerHex = PlayerColorHex(msg.PlayerIndex);

            switch (msg.EntryType)
            {
                case LogEntryType.EffectBlocked:
                    string blockHex = ColorUtility.ToHtmlStringRGB(firewallBlockColor);
                    return $"<color=#{blockHex}>🛡️</color> <color=#{playerHex}>{msg.Text}</color>";

                case LogEntryType.CardPlayed:
                    return $"<color=#{playerHex}>{msg.Text}</color>";

                case LogEntryType.TurnStart:
                    return $"⚡ <color=#{playerHex}>{msg.Text}</color>";

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
    }
}
