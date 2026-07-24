using UnityEngine;

namespace T60.StateMachine
{
    public class GameOverState : BaseState
    {
        public override void Enter()
        {
            base.Enter();
            if (Context != null)
            {
                Context.MatchOver = true;
                Debug.Log($"[GameOverState] MATCH OVER! Winner: Player {Context.WinnerPlayerIndex + 1}");
            }
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log("[GameOverState] Leaving Game Over screen.");
        }
    }
}
