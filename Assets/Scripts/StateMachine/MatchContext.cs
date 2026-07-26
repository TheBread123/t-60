using UnityEngine;

namespace T60.StateMachine
{
    public enum GameOverReason
    {
        None = 0,
        TimerExpired = 1,
        ResourceExhaustion = 2,
        ImmediateVictory = 3
    }

    public class MatchContext
    {
        public float MainClockSeconds { get; set; } = 60f;
        public float TurnClockSeconds { get; set; } = 5f;
        public float DefaultTurnClockDuration { get; set; } = 5f;

        public int ActivePlayerIndex { get; set; } = 0;
        public int TurnNumber { get; set; } = 1;
        public int WinnerPlayerIndex { get; set; } = -1;
        public GameOverReason WinReason { get; set; } = GameOverReason.None;

        public bool IsMainClockPaused { get; set; } = false;
        public bool MatchOver { get; set; } = false;

        // Card-effect state, indexed by player (0 or 1). Not yet consumed by the turn
        // loop / draw / UI systems — those integrations are a separate follow-up.
        public float[] PendingTurnClockBonus { get; } = new float[2];
        public bool[] EffectsBlocked { get; } = new bool[2];
        public bool[] SkipNextDraw { get; } = new bool[2];
        public int[] NextTurnDrawDelta { get; } = new int[2];
        public int[] PendingHandRemoval { get; } = new int[2];
        public int[] PreMoveCount { get; } = new int[2];
        public int TimerHiddenTurns { get; set; } = 0;
        public bool IsTimerHidden
        {
            get => TimerHiddenTurns > 0;
            set => TimerHiddenTurns = value ? 2 : 0;
        }

        public int OpponentIndex => (ActivePlayerIndex == 0) ? 1 : 0;

        public void SwitchTurn()
        {
            ActivePlayerIndex = (ActivePlayerIndex == 0) ? 1 : 0;
            TurnClockSeconds = DefaultTurnClockDuration;
            TurnNumber++;
            Debug.Log($"[MatchContext] Turn switched to Player {ActivePlayerIndex + 1}. Turn clock reset to {TurnClockSeconds}s. Turn {TurnNumber}.");
        }
    }
}
