using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class ModifyPreMoveEffect : Effect
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private bool targetOpponent = false;

        public int Amount
        {
            get => amount;
            set => amount = value;
        }

        public bool TargetOpponent
        {
            get => targetOpponent;
            set => targetOpponent = value;
        }

        public override void Execute(MatchContext context, Card sourceCard)
        {
            if (context == null) return;

            int target = targetOpponent ? context.OpponentIndex : context.ActivePlayerIndex;
            context.PreMoveCount[target] = Mathf.Max(0, context.PreMoveCount[target] + amount);

            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            Debug.Log($"[ModifyPreMoveEffect] '{cardName}' changed Player {target + 1}'s pre-moves by {amount}. Total: {context.PreMoveCount[target]}.");
        }
    }
}
