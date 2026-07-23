using UnityEngine;

namespace T60.StateMachine
{
    /// <summary>
    /// Holds runtime game state and clock values shared across match states.
    /// In T60 (MELTDOWN: Core Protocol):
    /// - Main Clock: Fixed 60s countdown running continuously for the whole match.
    /// - Turn Clock: Resets to 5s at the start of every turn and after penalty ticks.
    /// </summary>
    public class MatchContext
    {
        // Clocks
        public float MainClockSeconds { get; set; } = 60f;
        public float TurnClockSeconds { get; set; } = 5f;
        public float DefaultTurnClockDuration { get; set; } = 5f;

        // Player Tracking (0 = Player 1, 1 = Player 2)
        public int ActivePlayerIndex { get; set; } = 0;
        public int WinnerPlayerIndex { get; set; } = -1; // -1 means no winner yet

        // Flags
        public bool IsMainClockPaused { get; set; } = false;
        public bool ReflexWindowActive { get; set; } = false;
        public bool MatchOver { get; set; } = false;

        public void SwitchTurn()
        {
            ActivePlayerIndex = (ActivePlayerIndex == 0) ? 1 : 0;
            TurnClockSeconds = DefaultTurnClockDuration;
            Debug.Log($"[MatchContext] Turn switched to Player {ActivePlayerIndex + 1}. Turn clock reset to {TurnClockSeconds}s.");
        }
    }
}
