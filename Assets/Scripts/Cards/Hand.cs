using UnityEngine;
using T60.StateMachine;

namespace T60.Cards
{
    public class Hand : MonoBehaviour
    {
        [SerializeField] private Card[] cards;

        public void PlayCard(int index, MatchContext context)
        {
            if (index < 0 || index >= cards.Length)
            {
                Debug.LogWarning($"[Hand] Invalid card index: {index}.");
                return;
            }

            cards[index].CardEffect(context);
        }
    }
}
