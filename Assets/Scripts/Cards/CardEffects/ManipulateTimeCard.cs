using UnityEngine;
using T60.StateMachine;

namespace T60.Cards.CardEffects
{
    [CreateAssetMenu(menuName = "Cards/Manipulate Time", fileName = "ManipulateTimeCard")]
    public class ManipulateTimeCard : Card
    {
        [SerializeField] private float mainClockTimeDelta = -15f;

        public override void CardEffect(MatchContext context)
        {
            context.MainClockSeconds = Mathf.Max(0f, context.MainClockSeconds + mainClockTimeDelta);
            Debug.Log($"[ManipulateTimeCard] Player {PlayerOwner + 1} shifted Main Clock by {mainClockTimeDelta}s!");
        }
    }
}
