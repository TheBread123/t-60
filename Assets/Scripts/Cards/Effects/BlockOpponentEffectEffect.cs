using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class BlockOpponentEffectEffect : Effect
    {
        public override void Execute(MatchContext context, Card sourceCard)
        {
            if (context == null) return;

            context.EffectsBlocked[context.OpponentIndex] = true;
            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            Debug.Log($"[BlockOpponentEffectEffect] '{cardName}' flagged Player {context.OpponentIndex + 1}'s next effect to be blocked.");
        }
    }
}
