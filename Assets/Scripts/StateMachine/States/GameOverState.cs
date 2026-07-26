using System;
using UnityEngine;

namespace T60.StateMachine
{
    public class GameOverState : BaseState
    {
        public static event Action<MatchContext> OnGameOverEntered;
        public static event Action OnGameOverExited;

        public override void Enter()
        {
            base.Enter();
            if (Context != null)
            {
                Context.MatchOver = true;
                Debug.Log($"[GameOverState] MATCH OVER! Winner: Player {Context.WinnerPlayerIndex + 1}, Reason: {Context.WinReason}");
                OnGameOverEntered?.Invoke(Context);

                if (T60.UI.UIManager.Instance != null && T60.UI.UIManager.Instance.GameOverHUDHandler != null)
                {
                    T60.UI.UIManager.Instance.GameOverHUDHandler.ShowGameOverHUD(Context);
                }
            }
        }

        public override void StateUpdate()
        {
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log("[GameOverState] Leaving Game Over screen.");
            OnGameOverExited?.Invoke();

            if (T60.UI.UIManager.Instance != null && T60.UI.UIManager.Instance.GameOverHUDHandler != null)
            {
                T60.UI.UIManager.Instance.GameOverHUDHandler.HideGameOverHUD();
            }
        }
    }
}
