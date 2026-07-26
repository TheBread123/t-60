using UnityEngine;
using T60.Cards;
using T60.Pooling;
using T60.UI;

namespace T60.StateMachine
{
    public class PlayerTurnState : BaseState
    {
        public static event System.Action<int> OnTurnStarted;

        [Header("Transitions")]
        [SerializeField] private BaseState gameOverState;

        public override void Enter()
        {
            base.Enter();
            if (Context != null)
            {
                int activePlayer = Context.ActivePlayerIndex;
                Debug.Log($"[PlayerTurnState] Beginning Turn for Player {activePlayer + 1}.");
                OnTurnStarted?.Invoke(activePlayer);

                if (Context.TimerHiddenTurns > 0)
                {
                    Context.TimerHiddenTurns--;
                }

                // 1. Process delayed turn clock / main clock bonus (e.g. Coolant Reserve)
                if (Context.PendingTurnClockBonus[activePlayer] != 0f)
                {
                    float bonus = Context.PendingTurnClockBonus[activePlayer];
                    Context.MainClockSeconds += bonus;
                    if (UIManager.Instance != null && bonus != 0f)
                    {
                        UIManager.Instance.ShowMainTimerValueChange(bonus);
                    }
                    ActionLogManager.LogTurnEvent(activePlayer, $"gained +{bonus:F1}s Main Clock (Coolant Reserve)");
                    Debug.Log($"[PlayerTurnState] Applied delayed Main Clock bonus of +{bonus:F1}s for Player {activePlayer + 1}. New Main Clock: {Context.MainClockSeconds:F2}s.");
                    Context.PendingTurnClockBonus[activePlayer] = 0f;
                }

                // 2. Process pending hand removal (e.g. Data Purge)
                if (Context.PendingHandRemoval[activePlayer] > 0)
                {
                    int removeCount = Context.PendingHandRemoval[activePlayer];
                    Context.PendingHandRemoval[activePlayer] = 0;
                    if (GameManager.Instance != null)
                    {
                        var hand = GameManager.Instance.GetPlayerHand(activePlayer);
                        int removedActual = 0;
                        for (int i = 0; i < removeCount && hand.Count > 0; i++)
                        {
                            int randIdx = Random.Range(0, hand.Count);
                            CardHandler cardToRemove = hand[randIdx];
                            if (cardToRemove != null)
                            {
                                hand.RemoveAt(randIdx);
                                ObjectPoolManager.DespawnObject(cardToRemove.gameObject);
                                removedActual++;
                                Debug.Log($"[PlayerTurnState] Destroyed card '{cardToRemove.CardData?.CardName}' from Player {activePlayer + 1}'s hand due to Data Purge.");
                            }
                        }
                        if (removedActual > 0)
                        {
                            ActionLogManager.LogTurnEvent(activePlayer, $"lost {removedActual} card(s) to Data Purge");
                        }
                        GameManager.Instance.RecalculateHandLayout(activePlayer);
                    }
                }

                // 3. Process turn-start draw step (Default 1, plus modifiers like Scheduled Sweep or Supply Cutoff)
                bool skipDraw = Context.SkipNextDraw[activePlayer];
                Context.SkipNextDraw[activePlayer] = false;

                int drawCount = 0;
                if (skipDraw)
                {
                    drawCount = 0;
                    Context.NextTurnDrawDelta[activePlayer] = 0;
                    ActionLogManager.LogTurnEvent(activePlayer, "draw step WAS SKIPPED (Supply Cutoff)");
                }
                else
                {
                    int rawDelta = Context.NextTurnDrawDelta[activePlayer];
                    drawCount = Mathf.Max(0, 1 + rawDelta);
                    Context.NextTurnDrawDelta[activePlayer] = 0;

                    if (rawDelta != 0)
                    {
                        if (rawDelta > 0)
                        {
                            ActionLogManager.LogTurnEvent(activePlayer, $"drawing {drawCount} cards (+{rawDelta} extra draw)");
                        }
                        else
                        {
                            ActionLogManager.LogTurnEvent(activePlayer, $"drawing {drawCount} cards ({rawDelta} draw penalty)");
                        }
                    }
                }

                if (drawCount > 0 && GameManager.Instance != null)
                {
                    for (int i = 0; i < drawCount; i++)
                    {
                        GameManager.Instance.DrawCardForPlayer(activePlayer);
                    }
                    Debug.Log($"[PlayerTurnState] Player {activePlayer + 1} drew {drawCount} card(s) at start of turn.");
                }

                // 4. Check for empty hand (Default win / Resource Exhaustion)
                if (GameManager.Instance != null)
                {
                    var hand = GameManager.Instance.GetPlayerHand(activePlayer);
                    if (hand == null || hand.Count == 0)
                    {
                        int winnerIndex = (activePlayer == 0) ? 1 : 0;
                        Context.WinnerPlayerIndex = winnerIndex;
                        Context.WinReason = GameOverReason.ResourceExhaustion;
                        Debug.LogWarning($"[PlayerTurnState] Player {activePlayer + 1} has 0 cards in hand and cannot make a move! Player {winnerIndex + 1} wins by Resource Exhaustion.");
                        if (gameOverState != null && Runner != null && Runner.StateMachine != null)
                        {
                            Runner.StateMachine.ChangeState(gameOverState);
                            return;
                        }
                    }
                }

                // 5. Ensure card selection is clamped to valid hand bounds for the active player (retaining current selection)
                if (CardInputManager.Instance != null)
                {
                    CardInputManager.Instance.SetSelectedIndex(activePlayer, CardInputManager.Instance.GetSelectedIndex(activePlayer));
                }
            }
        }

        public override void StateUpdate()
        {
            if (Context == null) return;

            // Check for empty hand safety during turn update
            if (GameManager.Instance != null)
            {
                var hand = GameManager.Instance.GetPlayerHand(Context.ActivePlayerIndex);
                if (hand == null || hand.Count == 0)
                {
                    int winnerIndex = (Context.ActivePlayerIndex == 0) ? 1 : 0;
                    Context.WinnerPlayerIndex = winnerIndex;
                    Context.WinReason = GameOverReason.ResourceExhaustion;
                    Debug.LogWarning($"[PlayerTurnState] Player {Context.ActivePlayerIndex + 1} has 0 cards in hand! Player {winnerIndex + 1} wins by Resource Exhaustion.");
                    if (gameOverState != null && Runner != null && Runner.StateMachine != null)
                    {
                        Runner.StateMachine.ChangeState(gameOverState);
                        return;
                    }
                }
            }

            float dt = Time.unscaledDeltaTime;

            if (!Context.IsMainClockPaused)
            {
                Context.MainClockSeconds -= dt;

                if (Context.MainClockSeconds <= 0f)
                {
                    Context.MainClockSeconds = 0f;
                    int loserIndex = Context.ActivePlayerIndex;
                    Context.WinnerPlayerIndex = (loserIndex == 0) ? 1 : 0;
                    Context.WinReason = GameOverReason.TimerExpired;
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
                int activePlayer = Context.ActivePlayerIndex;
                Debug.LogWarning($"[PlayerTurnState] Turn Clock expired for Player {activePlayer + 1}! Losing card.");

                if (GameManager.Instance != null)
                {
                    var hand = GameManager.Instance.GetPlayerHand(activePlayer);
                    if (hand != null && hand.Count > 0)
                    {
                        int randIdx = Random.Range(0, hand.Count);
                        CardHandler cardToRemove = hand[randIdx];
                        if (cardToRemove != null)
                        {
                            string cardName = cardToRemove.CardData != null ? cardToRemove.CardData.CardName : "Unknown";
                            hand.RemoveAt(randIdx);
                            ObjectPoolManager.DespawnObject(cardToRemove.gameObject);
                            ActionLogManager.LogTurnEvent(activePlayer, $"lost '{cardName}' to Turn Clock expiring");
                            Debug.Log($"[PlayerTurnState] Removed random card '{cardName}' from Player {activePlayer + 1}'s hand due to Turn Clock expiring.");
                        }
                        GameManager.Instance.RecalculateHandLayout(activePlayer);
                    }
                }

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

            int mover = Context.ActivePlayerIndex;
            Debug.Log($"[PlayerTurnState] Player {mover + 1} playing card '{card.CardName}'.");
            card.Play(Context);

            // Firewall jams its target's next Offensive play, consumed by the block check in
            // Card.Play(). If the mover still has the flag set after playing a non-Offensive
            // card, their jammed turn just ended without triggering it — expire it now, while
            // we still have their pre-switch index, rather than the opponent's post-switch one.
            if (Context.EffectsBlocked[mover])
            {
                Context.EffectsBlocked[mover] = false;
                Debug.Log($"[PlayerTurnState] Firewall shield for Player {mover + 1} expired unused.");
                ActionLogManager.LogInfo($"🛡️ Firewall on Player {mover + 1} expired — no Offensive card was played against it.");
            }

            if (!Context.MatchOver && Runner != null && Runner.StateMachine != null)
            {
                Context.SwitchTurn();
                Runner.StateMachine.ChangeState(this);
            }
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
