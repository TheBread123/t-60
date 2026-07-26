using UnityEngine;
using T60.StateMachine;

namespace T60.UI
{
    public enum LogEntryType
    {
        Info,
        CardPlayed,
        EffectBlocked,
        TurnStart,
        TimerChange
    }

    public struct ActionLogMessage
    {
        public string Text;
        public LogEntryType EntryType;
        public int PlayerIndex; // 0 = Player 1, 1 = Player 2, -1 = System
        public int TurnNumber;  // Turn on which this message was logged
    }

    public static class ActionLogManager
    {
        public static event System.Action<ActionLogMessage> OnLogAdded;
        public static event System.Action OnLogCleared;

        public static void ClearLog()
        {
            OnLogCleared?.Invoke();
        }
        public static void LogCardPlayed(int playerIndex, string cardName, string effectSummary)
        {
            string playerLabel = $"Player {playerIndex + 1}";
            string text = $"{playerLabel} played <b>{cardName}</b>";
            if (!string.IsNullOrEmpty(effectSummary))
            {
                text += $" <i>({effectSummary})</i>";
            }

            Log(text, LogEntryType.CardPlayed, playerIndex);
        }

        public static void LogEffectBlocked(int playerIndex, string cardName)
        {
            string playerLabel = $"Player {playerIndex + 1}";
            string text = $"🛡️ <b>[FIREWALL]</b> {playerLabel}'s <b>{cardName}</b> was <b>CANCELED</b>!";

            Log(text, LogEntryType.EffectBlocked, playerIndex);
        }

        public static void LogTurnEvent(int playerIndex, string eventDescription)
        {
            string playerLabel = $"Player {playerIndex + 1}";
            string text = $"{playerLabel}: {eventDescription}";

            Log(text, LogEntryType.TurnStart, playerIndex);
        }

        public static void LogTimerEvent(string message)
        {
            Log(message, LogEntryType.TimerChange, -1);
        }

        public static void LogInfo(string message)
        {
            Log(message, LogEntryType.Info, -1);
        }

        public static void Log(string text, LogEntryType entryType, int playerIndex, int turnNumber = -1)
        {
            int turn = turnNumber >= 0 ? turnNumber : ResolveCurrentTurn();

            var msg = new ActionLogMessage
            {
                Text = text,
                EntryType = entryType,
                PlayerIndex = playerIndex,
                TurnNumber = turn
            };

            Debug.Log($"[ActionLog] (Turn {turn}) ({entryType}) {text}");
            OnLogAdded?.Invoke(msg);
        }

        private static int ResolveCurrentTurn()
        {
            if (GameManager.Instance != null)
            {
                var ctx = GameManager.Instance.MatchContext;
                if (ctx != null) return ctx.TurnNumber;
            }
            return 0;
        }
    }
}
