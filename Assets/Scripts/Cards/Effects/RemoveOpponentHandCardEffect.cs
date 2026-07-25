using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class RemoveOpponentHandCardEffect : Effect
    {
        [SerializeField] private int removeCount = 1;

        public int RemoveCount
        {
            get => removeCount;
            set => removeCount = value;
        }

        public override void Execute(MatchContext context, Card sourceCard)
        {
            if (context == null) return;

            context.PendingHandRemoval[context.OpponentIndex] += removeCount;
            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            Debug.Log($"[RemoveOpponentHandCardEffect] '{cardName}' queued removal of {removeCount} card(s) from Player {context.OpponentIndex + 1}'s hand.");
        }
    }
}
