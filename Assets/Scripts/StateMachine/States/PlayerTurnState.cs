using UnityEngine;
using T60.Cards;

namespace T60.StateMachine
{
    public class PlayerTurnState : BaseState
    {
        [Header("Transitions")]
        [SerializeField] private BaseState gameOverState;

        public override void Enter()
        {
            base.Enter();
            if (Context != null)
            {
                Debug.Log($"[PlayerTurnState] Beginning Turn for Player {Context.ActivePlayerIndex + 1}.");
            }
        }

        public override void Update()
        {
            if (Context == null) return;

            float dt = Time.deltaTime;

            if (!Context.IsMainClockPaused)
            {
                Context.MainClockSeconds -= dt;

                if (Context.MainClockSeconds <= 0f)
                {
                    Context.MainClockSeconds = 0f;
                    int loserIndex = Context.ActivePlayerIndex;
                    Context.WinnerPlayerIndex = (loserIndex == 0) ? 1 : 0;
                    Debug.LogWarning($"[PlayerTurnState] Main Clock reached 0:00 on Player {loserIndex + 1}'s turn! Transitioning to Game Over.");
                    
                    if (gameOverState != null)
                    {
                        Runner.StateMachine.ChangeState(gameOverState);
                    }
                    else
                    {
                        Debug.LogError("[PlayerTurnState] GameOverState transition reference is missing!");
                    }
                    return;
                }
            }

            Context.TurnClockSeconds -= dt;
            if (Context.TurnClockSeconds <= 0f)
            {
                Debug.LogWarning($"[PlayerTurnState] Turn Clock expired for Player {Context.ActivePlayerIndex + 1}! Losing card.");
                Context.TurnClockSeconds = Context.DefaultTurnClockDuration;
            }
        }

        public void PlayCard(Card card)
        {
            if (Context == null) return;
            if (card == null)
            {
                Debug.LogWarning("[PlayerTurnState] Attempted to play a null Card!");
                return;
            }

            Debug.Log($"[PlayerTurnState] Player {Context.ActivePlayerIndex + 1} playing card '{card.CardName}'.");
            card.Play(Context);
        }

        public override void Exit()
        {
            base.Exit();
            if (Context != null)
            {
                Debug.Log($"[PlayerTurnState] Exiting turn for Player {Context.ActivePlayerIndex + 1}.");
            }
        }
    }
}
