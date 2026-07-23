using UnityEngine;

namespace T60.StateMachine
{
    public class PlayerTurnState : IState
    {
        private readonly MatchStateMachineRunner _runner;
        private readonly MatchContext _context;

        public PlayerTurnState(MatchStateMachineRunner runner, MatchContext context)
        {
            _runner = runner;
            _context = context;
        }

        public void Enter()
        {
            Debug.Log($"[PlayerTurnState] Beginning Turn for Player {_context.ActivePlayerIndex + 1}. Turn Clock reset to {_context.TurnClockSeconds}s.");
            // Draw 1 Protocol card at start of turn (as defined in GDD Section 6)
            Debug.Log($"[PlayerTurnState] Player {_context.ActivePlayerIndex + 1} draws 1 card.");
        }

        public void Update()
        {
            float dt = Time.deltaTime;

            // 1. Tick Main Clock (runs continuously for the entire match unless paused by Freeze Protocol)
            if (!_context.IsMainClockPaused)
            {
                _context.MainClockSeconds -= dt;

                if (_context.MainClockSeconds <= 0f)
                {
                    _context.MainClockSeconds = 0f;
                    Debug.LogWarning($"[PlayerTurnState] Main Clock reached 0:00 on Player {_context.ActivePlayerIndex + 1}'s turn!");
                    _runner.StateMachine.ChangeState(new ReflexWindowState(_runner, _context));
                    return;
                }
            }

            // 2. Tick Turn Clock (resets every 5 seconds)
            _context.TurnClockSeconds -= dt;
            if (_context.TurnClockSeconds <= 0f)
            {
                // Turn Clock Penalty: lose 1 random card, reset turn clock immediately
                Debug.LogWarning($"[PlayerTurnState] Turn Clock expired for Player {_context.ActivePlayerIndex + 1}! Losing 1 random card from hand.");
                _context.TurnClockSeconds = _context.DefaultTurnClockDuration;
            }
        }

        /// <summary>
        /// Called when the player clicks or plays a card from hand.
        /// </summary>
        public void PlayCard(string cardName, float mainClockTimeDelta = 0f)
        {
            Debug.Log($"[PlayerTurnState] Player {_context.ActivePlayerIndex + 1} played card: {cardName}.");

            // Apply Main Clock time shift if applicable (e.g., Coolant adds time, Overload subtracts)
            if (mainClockTimeDelta != 0f)
            {
                _context.MainClockSeconds = Mathf.Max(0f, _context.MainClockSeconds + mainClockTimeDelta);
                Debug.Log($"[PlayerTurnState] Main Clock adjusted by {mainClockTimeDelta}s. Current: {_context.MainClockSeconds:F2}s");
            }

            // In T60, playing a card immediately hands off turn to opponent
            _context.SwitchTurn();
        }

        public void Exit()
        {
            Debug.Log($"[PlayerTurnState] Exiting turn for Player {_context.ActivePlayerIndex + 1}.");
        }
    }
}
