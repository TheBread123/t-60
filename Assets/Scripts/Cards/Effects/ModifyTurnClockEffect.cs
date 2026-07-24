using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.Effects
{
    [System.Serializable]
    public class ModifyTurnClockEffect : Effect
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

            context.TurnClockSeconds = Mathf.Max(0f, context.TurnClockSeconds + timeDelta);
            string cardName = sourceCard != null ? sourceCard.CardName : "Unknown Card";
            int owner = context != null ? context.ActivePlayerIndex + 1 : 0;
            Debug.Log($"[ModifyTurnClockEffect] '{cardName}' (Player {owner}) modified Turn Clock by {timeDelta:F1}s. New time: {context.TurnClockSeconds:F2}s.");
        }
    }
}
