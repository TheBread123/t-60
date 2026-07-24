using UnityEngine;

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

            if (Context != null)
            {
                Context.MainClockSeconds = initialMainClockSeconds;
                Context.DefaultTurnClockDuration = defaultTurnClockDuration;
                Context.TurnClockSeconds = Context.DefaultTurnClockDuration;
                Context.ActivePlayerIndex = 0;
                Context.WinnerPlayerIndex = -1;
                Context.MatchOver = false;
            }

            Debug.Log("[MatchSetupState] Match parameters set. Moving to card dealing.");
        }

        public override void Update()
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
