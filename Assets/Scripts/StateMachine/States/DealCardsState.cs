using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using T60.Cards;
using T60.Pooling;

namespace T60.StateMachine
{
    public class DealCardsState : BaseState
    {
        [Header("Transitions")]
        [SerializeField] private BaseState playerTurnState;

        [Header("Card Setup")]
        [SerializeField] private GameObject cardPrefabOverride;

        private bool isDealing;

        public override void Enter()
        {
            base.Enter();
            isDealing = true;
            StartCoroutine(DealCardsRoutine());
        }

        private IEnumerator DealCardsRoutine()
        {
            GameManager manager = GameManager.Instance;
            int handSize = (manager != null) ? manager.InitialHandSize : 3;
            float staggerDelay = (manager != null) ? manager.DealStaggerDelay : 0.15f;
            float moveDuration = (manager != null) ? manager.CardDealMoveDuration : 0.4f;

            GameObject prefabToSpawn = cardPrefabOverride;
            if (prefabToSpawn == null && manager != null)
            {
                prefabToSpawn = manager.CardPrefab;
            }

            Vector3 spawnPos = (manager != null) ? manager.DeckPosition : Vector3.zero;

            List<Coroutine> activeMoveRoutines = new List<Coroutine>();

            for (int cardIdx = 0; cardIdx < handSize; cardIdx++)
            {
                for (int playerIdx = 0; playerIdx < 2; playerIdx++)
                {
                    Vector3 targetPos = (manager != null)
                        ? manager.GetHandCardWorldPosition(playerIdx, cardIdx, handSize)
                        : new Vector3((playerIdx == 0 ? -1f : 1f) * (cardIdx + 1), playerIdx == 0 ? -3f : 3f, 0f);

                    GameObject cardObject = null;
                    if (prefabToSpawn != null)
                    {
                        cardObject = ObjectPoolManager.SpawnObject(prefabToSpawn, spawnPos, Quaternion.identity);
                    }

                    if (cardObject != null)
                    {
                        if (cardObject.TryGetComponent<CardHandler>(out var cardHandler))
                        {
                            cardHandler.EnableDragging = false;
                            cardHandler.PlayerOwner = playerIdx;

                            if (manager != null)
                            {
                                Card templateCard = manager.GetRandomCard();
                                if (templateCard != null)
                                {
                                    cardHandler.SetCard(templateCard);
                                }
                            }
                        }

                        activeMoveRoutines.Add(StartCoroutine(AnimateCardToPosition(cardObject, spawnPos, targetPos, moveDuration)));
                    }

                    yield return new WaitForSeconds(staggerDelay);
                }
            }

            foreach (var routine in activeMoveRoutines)
            {
                if (routine != null) yield return routine;
            }

            isDealing = false;

            if (playerTurnState != null)
            {
                Runner.StateMachine.ChangeState(playerTurnState);
            }
            else
            {
                Debug.LogError("[DealCardsState] PlayerTurnState transition reference is missing!");
            }
        }

        private IEnumerator AnimateCardToPosition(GameObject cardObject, Vector3 startPos, Vector3 targetPos, float duration)
        {
            if (cardObject == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (cardObject == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                cardObject.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                yield return null;
            }

            if (cardObject != null)
            {
                cardObject.transform.position = targetPos;
                if (cardObject.TryGetComponent<CardHandler>(out var cardHandler))
                {
                    cardHandler.OriginalPosition = targetPos;
                    cardHandler.EnableDragging = true;
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            isDealing = false;
        }
    }
}
