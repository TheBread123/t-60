using UnityEngine;

namespace T60.StateMachine
{
    /// <summary>
    /// Triggered the moment the Main Clock hits 0:00 during a player's turn.
    /// Gives the player a brief reaction window to slam down a Reflex card (e.g. Emergency Vent).
    /// </summary>
    public class ReflexWindowState : IState
    {
        private readonly MatchStateMachineRunner _runner;
        private readonly MatchContext _context;
        private float _reflexWindowTimer = 2.0f; // 2 seconds window to respond

        public ReflexWindowState(MatchStateMachineRunner runner, MatchContext context)
        {
            _runner = runner;
            _context = context;
        }

        public void Enter()
        {
            _context.ReflexWindowActive = true;
            Debug.LogWarning($"[ReflexWindowState] CRITICAL! Main Clock is 0:00! Player {_context.ActivePlayerIndex + 1} has {_reflexWindowTimer}s to play a Reflex Card!");
        }

        public void Update()
        {
            _reflexWindowTimer -= Time.deltaTime;

            if (_reflexWindowTimer <= 0f)
            {
                // Reflex window expired without playing a Reflex card -> Player loses!
                int loserIndex = _context.ActivePlayerIndex;
                _context.WinnerPlayerIndex = (loserIndex == 0) ? 1 : 0;
                Debug.LogError($"[ReflexWindowState] Reflex window expired! Player {loserIndex + 1} failed to play a Reflex card.");

                _runner.StateMachine.ChangeState(new GameOverState(_runner, _context));
            }
        }

        /// <summary>
        /// Called when the player plays a valid Reflex Card during the critical window.
        /// </summary>
        public void PlayReflexCard(string reflexCardName, float addedSeconds = 15f)
        {
            Debug.Log($"[ReflexWindowState] REFLEX PLAYED! Player {_context.ActivePlayerIndex + 1} played {reflexCardName}!");
            _context.MainClockSeconds += addedSeconds;
            _context.ReflexWindowActive = false;

            // Transition back to player turn with restored Main Clock
            _runner.StateMachine.ChangeState(new PlayerTurnState(_runner, _context));
        }

        public void Exit()
        {
            _context.ReflexWindowActive = false;
        }
    }
}
