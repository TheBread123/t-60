using UnityEngine;

namespace T60.StateMachine
{
    public class MatchContext
    {
        public float MainClockSeconds { get; set; } = 60f;
        public float TurnClockSeconds { get; set; } = 5f;
        public float DefaultTurnClockDuration { get; set; } = 5f;

        public int ActivePlayerIndex { get; set; } = 0;
        public int WinnerPlayerIndex { get; set; } = -1;

        public bool IsMainClockPaused { get; set; } = false;
        public bool MatchOver { get; set; } = false;

        public void SwitchTurn()
        {
            ActivePlayerIndex = (ActivePlayerIndex == 0) ? 1 : 0;
            TurnClockSeconds = DefaultTurnClockDuration;
            Debug.Log($"[MatchContext] Turn switched to Player {ActivePlayerIndex + 1}. Turn clock reset to {TurnClockSeconds}s.");
        }
    }
}
