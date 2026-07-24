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
            Debug.Log($"[PlayerTurnState] Beginning Turn for Player {_context.ActivePlayerIndex + 1}.");
        }

        public void Update()
        {
            float dt = Time.deltaTime;

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

            _context.TurnClockSeconds -= dt;
            if (_context.TurnClockSeconds <= 0f)
            {
                Debug.LogWarning($"[PlayerTurnState] Turn Clock expired for Player {_context.ActivePlayerIndex + 1}! Losing card.");
                _context.TurnClockSeconds = _context.DefaultTurnClockDuration;
            }
        }

        public void PlayCard(string cardName, float mainClockTimeDelta = 0f)
        {
            Debug.Log($"[PlayerTurnState] Player {_context.ActivePlayerIndex + 1} played card: {cardName}.");

            if (mainClockTimeDelta != 0f)
            {
                _context.MainClockSeconds = Mathf.Max(0f, _context.MainClockSeconds + mainClockTimeDelta);
            }

            _context.SwitchTurn();
        }

        public void Exit()
        {
            Debug.Log($"[PlayerTurnState] Exiting turn for Player {_context.ActivePlayerIndex + 1}.");
        }
    }
}
