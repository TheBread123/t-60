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

            context.SwitchTurn();
        }
    }
}
