using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public abstract class Effect
    {
        public abstract void Execute(MatchContext context, Card sourceCard);
    }
}
