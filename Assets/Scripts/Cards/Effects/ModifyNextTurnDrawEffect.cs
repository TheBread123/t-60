using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class ModifyNextTurnDrawEffect : Effect
    {
        [SerializeField] private int drawDelta = 1;
        [SerializeField] private bool targetOpponent = false;
        [SerializeField] private bool skipNextDrawStep = false;

        public int DrawDelta
        {
            get => drawDelta;
            set => drawDelta = value;
        }

        public bool TargetOpponent
        {
            get => targetOpponent;
            set => targetOpponent = value;
        }

        public bool SkipNextDrawStep
        {
            get => skipNextDrawStep;
            set => skipNextDrawStep = value;
        }

        public override void Execute(MatchContext context, Card sourceCard)
        {
            if (context == null) return;

            int target = targetOpponent ? context.OpponentIndex : context.ActivePlayerIndex;
            if (skipNextDrawStep)
            {
                context.SkipNextDraw[target] = true;
            }
            else
            {
                context.NextTurnDrawDelta[target] += drawDelta;
            }

            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            Debug.Log($"[ModifyNextTurnDrawEffect] '{cardName}' modified Player {target + 1}'s draw step. SkipDraw: {skipNextDrawStep}, Delta: {drawDelta}.");
        }
    }
}
