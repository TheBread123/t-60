using UnityEngine;
using T60.StateMachine;

namespace T60.Cards
{
    public abstract class Card : ScriptableObject
    {
        [SerializeField] private int playerOwner;

        public virtual int PlayerOwner
        {
            get => playerOwner;
            set => playerOwner = value;
        }

        public abstract void CardEffect(MatchContext context);
    }
}
