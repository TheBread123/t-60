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

            _context.MainClockSeconds = 60f;
            _context.DefaultTurnClockDuration = 5f;
            _context.TurnClockSeconds = _context.DefaultTurnClockDuration;
            _context.ActivePlayerIndex = 0;
            _context.MatchOver = false;

            Debug.Log("[MatchSetupState] Shuffling Protocol Deck & dealing starting cards.");
        }

        public void Update()
        {
            _runner.StateMachine.ChangeState(new PlayerTurnState(_runner, _context));
        }

        public void Exit()
        {
            Debug.Log("[MatchSetupState] Match initialized.");
        }
    }
}
