using UnityEngine;

namespace T60.StateMachine
{
    public class GameOverState : IState
    {
        private readonly MatchStateMachineRunner _runner;
        private readonly MatchContext _context;

        public GameOverState(MatchStateMachineRunner runner, MatchContext context)
        {
            _runner = runner;
            _context = context;
        }

        public void Enter()
        {
            _context.MatchOver = true;
            Debug.Log($"[GameOverState] MATCH OVER! Winner: Player {_context.WinnerPlayerIndex + 1}");
        }

        public void Update() { }

        public void Exit()
        {
            Debug.Log("[GameOverState] Leaving Game Over screen.");
        }
    }
}
