using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class ModifyNextTurnClockEffect : Effect
    {
        [SerializeField] private float secondsDelta = 3f;
        [SerializeField] private bool targetOpponent = false;

        public float SecondsDelta
        {
            get => secondsDelta;
            set => secondsDelta = value;
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
            context.PendingTurnClockBonus[target] += secondsDelta;

            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            Debug.Log($"[ModifyNextTurnClockEffect] '{cardName}' changed Player {target + 1}'s next turn clock by {secondsDelta:F1}s.");
        }
    }
}
