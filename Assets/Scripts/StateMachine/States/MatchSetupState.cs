using UnityEngine;

namespace T60.StateMachine
{
    public class MatchSetupState : IState
    {
        private readonly MatchStateMachineRunner _runner;
        private readonly MatchContext _context;

        public MatchSetupState(MatchStateMachineRunner runner, MatchContext context)
        {
            _runner = runner;
            _context = context;
        }

        public void Enter()
        {
            Debug.Log("[MatchSetupState] Initializing match parameters...");

            // Reset match values based on GDD specs
            _context.MainClockSeconds = 60f;
            _context.DefaultTurnClockDuration = 5f;
            _context.TurnClockSeconds = _context.DefaultTurnClockDuration;
            _context.ActivePlayerIndex = 0;
            _context.MatchOver = false;

            // Simulate deck setup & dealing 5 cards to each player
            Debug.Log("[MatchSetupState] Shuffling Protocol Deck (61 cards) & dealing 5 cards to P1 and P2.");
        }

        public void Update()
        {
            // Transition immediately to the first player's turn once setup is complete
            _runner.StateMachine.ChangeState(new PlayerTurnState(_runner, _context));
        }

        public void Exit()
        {
            Debug.Log("[MatchSetupState] Match initialized. Starting main clock countdown.");
        }
    }
}
