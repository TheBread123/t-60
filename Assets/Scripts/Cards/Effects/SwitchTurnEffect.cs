using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class SwitchTurnEffect : Effect
    {
        public override void Execute(MatchContext context, Card sourceCard)
        {
            if (context == null) return;

            // Turn switching is now handled automatically by PlayerTurnState upon playing any card.
            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            Debug.Log($"[SwitchTurnEffect] Executed from '{cardName}'. Turn switch is managed automatically by turn state.");
        }
    }
}
