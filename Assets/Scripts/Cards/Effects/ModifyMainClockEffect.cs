using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class ModifyMainClockEffect : Effect
    {
        [SerializeField] private float timeDelta = 0f;

        public float TimeDelta
        {
            get => timeDelta;
            set => timeDelta = value;
        }

        public override void Execute(MatchContext context, Card sourceCard)
        {
            if (context == null) return;

            context.MainClockSeconds = Mathf.Max(0f, context.MainClockSeconds + timeDelta);
            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            int owner = context != null ? context.ActivePlayerIndex + 1 : 0;
            Debug.Log($"[ModifyMainClockEffect] '{cardName}' (Player {owner}) modified Main Clock by {timeDelta:F1}s. New time: {context.MainClockSeconds:F2}s.");
        }
    }
}
