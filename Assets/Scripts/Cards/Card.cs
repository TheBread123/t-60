using UnityEngine;
using T60.StateMachine;
using T60.Cards.Effects;
using T60.Cards.Attributes;

namespace T60.Cards
{
    [CreateAssetMenu(menuName = "T60/Card", fileName = "NewCard")]
    public class Card : ScriptableObject
    {
        [SerializeField] private string cardName = "New Card";
        [TextArea]
        [SerializeField] private string description = "";
        [SerializeField] private Sprite art;
        [SerializeField] private int weight = 1;
        [SerializeField] private bool isNextTurnEffect = false;

        [SerializeReference, SubclassSelector]
        private Effect[] effects = new Effect[0];

        public string CardName
        {
            get => cardName;
            set => cardName = value;
        }

        public string Description
        {
            get => description;
            set => description = value;
        }

        public Sprite Art
        {
            get => art;
            set => art = value;
        }

        // Higher weight = rarer draw; represents card value, not pick frequency directly.
        public int Weight
        {
            get => weight;
            set => weight = value;
        }

        public bool IsNextTurnEffect
        {
            get => isNextTurnEffect;
            set => isNextTurnEffect = value;
        }

        public Effect[] Effects
        {
            get => effects;
            set => effects = value;
        }

        public void SetEffects(params Effect[] newEffects)
        {
            effects = newEffects;
        }

        public virtual void Play(MatchContext context)
        {
            if (context == null) return;

            int activePlayer = context.ActivePlayerIndex + 1;
            Debug.Log($"[Card] Playing card '{cardName}' for Player {activePlayer}.");

            if (effects != null)
            {
                foreach (var effect in effects)
                {
                    if (effect != null)
                    {
                        effect.Execute(context, this);
                    }
                }
            }
        }
    }
}
