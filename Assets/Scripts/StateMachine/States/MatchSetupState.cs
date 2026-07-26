using UnityEngine;
using T60.UI;

namespace T60.StateMachine
{
    public class MatchSetupState : BaseState
    {
        [Header("Transitions")]
        [SerializeField] private BaseState dealCardsState;
        [SerializeField] private BaseState playerTurnState;

        [Header("Setup Parameters")]
        [SerializeField] private float initialMainClockSeconds = 60f;
        [SerializeField] private float defaultTurnClockDuration = 5f;

        public override void Enter()
        {
            base.Enter();
            Debug.Log("[MatchSetupState] Initializing match parameters...");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClearAllCards();
            }

            if (Context != null)
            {
                Context.MainClockSeconds = initialMainClockSeconds;
                Context.DefaultTurnClockDuration = defaultTurnClockDuration;
                Context.TurnClockSeconds = Context.DefaultTurnClockDuration;
                Context.ActivePlayerIndex = 0;
                Context.TurnNumber = 1;
                Context.WinnerPlayerIndex = -1;
                Context.WinReason = GameOverReason.None;
                Context.MatchOver = false;
                for (int i = 0; i < 2; i++)
                {
                    Context.PendingTurnClockBonus[i] = 0f;
                    Context.EffectsBlocked[i] = false;
                    Context.NextTurnDrawDelta[i] = 0;
                    Context.PendingHandRemoval[i] = 0;
                    Context.PreMoveCount[i] = 0;
                }
                Context.IsTimerHidden = false;
            }

            ActionLogManager.ClearLog();

            Debug.Log("[MatchSetupState] Match parameters set. Moving to card dealing.");
        }

        public override void StateUpdate()
        {
            if (dealCardsState != null)
            {
                Runner.StateMachine.ChangeState(dealCardsState);
            }
            else if (playerTurnState != null)
            {
                Runner.StateMachine.ChangeState(playerTurnState);
            }
            else
            {
                Debug.LogError("[MatchSetupState] Neither dealCardsState nor playerTurnState transition references are assigned!");
            }
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log("[MatchSetupState] Match initialized.");
        }
    }
}
