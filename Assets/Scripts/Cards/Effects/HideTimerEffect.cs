using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class HideTimerEffect : Effect
    {
        public override void Execute(MatchContext context, Card sourceCard)
        {
            if (context == null) return;

            context.IsTimerHidden = true;
            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            Debug.Log($"[HideTimerEffect] '{cardName}' hid the timer.");
        }
    }
}
