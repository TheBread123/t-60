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

        // Card-effect state, indexed by player (0 or 1). Not yet consumed by the turn
        // loop / draw / UI systems — those integrations are a separate follow-up.
        public float[] PendingTurnClockBonus { get; } = new float[2];
        public bool[] EffectsBlocked { get; } = new bool[2];
        public int[] NextTurnDrawDelta { get; } = new int[2];
        public int[] PendingHandRemoval { get; } = new int[2];
        public int[] PreMoveCount { get; } = new int[2];
        public bool IsTimerHidden { get; set; } = false;

        public int OpponentIndex => (ActivePlayerIndex == 0) ? 1 : 0;

        public void SwitchTurn()
        {
            ActivePlayerIndex = (ActivePlayerIndex == 0) ? 1 : 0;
            TurnClockSeconds = DefaultTurnClockDuration;
            Debug.Log($"[MatchContext] Turn switched to Player {ActivePlayerIndex + 1}. Turn clock reset to {TurnClockSeconds}s.");
        }
    }
}
