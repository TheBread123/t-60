using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using T60.Cards;
using T60.Pooling;

namespace T60.StateMachine
{
    public class DealCardsState : BaseState
    {
        [Header("Transitions")]
        [SerializeField] private BaseState matchPreparationState;
        [SerializeField] private BaseState playerTurnState;

        [Header("Card Setup & Timing")]
        [SerializeField] private GameObject cardPrefabOverride;
        [Tooltip("Configurable delay upon state enter (e.g. for displaying announcement text).")]
        [SerializeField] private float enterDelay = 1.0f;

        [Header("Deal Cards Events")]
        [SerializeField] private UnityEvent onEnterDelayCompleted;

        public UnityEvent OnEnterDelayCompleted => onEnterDelayCompleted;

        // Static events for C# UI Manager integration
        public static event System.Action OnDealCardsStarted;
        public static event System.Action OnEnterDelayFinished;

        private bool isDealing;

        public override void Enter()
        {
            base.Enter();
            isDealing = true;
            Debug.Log("[DealCardsState] Triggering Get Ready announcement...");
            OnDealCardsStarted?.Invoke();
            StartCoroutine(DealCardsRoutine());
        }

        private IEnumerator DealCardsRoutine()
        {
            if (enterDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(enterDelay);
            }

            Debug.Log($"[DealCardsState] Enter delay completed after {enterDelay}s. Invoking completion events.");
            onEnterDelayCompleted?.Invoke();
            OnEnterDelayFinished?.Invoke();

            GameManager manager = GameManager.Instance;
            int handSize = (manager != null) ? Mathf.Min(manager.InitialHandSize, manager.MaxHandSize) : 3;
            float staggerDelay = (manager != null) ? manager.DealStaggerDelay : 0.15f;
            float moveDuration = (manager != null) ? manager.CardDealMoveDuration : 0.4f;

            GameObject prefabToSpawn = cardPrefabOverride;
            if (prefabToSpawn == null && manager != null)
            {
                prefabToSpawn = manager.CardPrefab;
            }

            Vector3 spawnPos = (manager != null) ? manager.DeckPosition : Vector3.zero;
            Vector3 startScale = (manager != null) ? manager.DeckScale : Vector3.one;
            Vector3 targetScale = prefabToSpawn != null ? prefabToSpawn.transform.localScale : Vector3.one;

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
                        cardObject.transform.localScale = startScale;

                        if (cardObject.TryGetComponent<CardHandler>(out var cardHandler))
                        {
                            cardHandler.EnableDragging = false;
                            cardHandler.IsAnimatingDraw = true;
                            cardHandler.PlayerOwner = playerIdx;
                            cardHandler.OriginalScale = targetScale;

                            if (manager != null)
                            {
                                Card templateCard = manager.GetRandomCard();
                                if (templateCard != null)
                                {
                                    cardHandler.SetCard(templateCard);
                                }
                                manager.RegisterCardInHand(playerIdx, cardHandler);
                            }
                            cardHandler.OriginalPosition = targetPos;
                        }

                        activeMoveRoutines.Add(StartCoroutine(AnimateCardToPosition(cardObject, spawnPos, targetPos, startScale, targetScale, moveDuration)));
                    }

                    yield return new WaitForSecondsRealtime(staggerDelay);
                }
            }

            foreach (var routine in activeMoveRoutines)
            {
                if (routine != null) yield return routine;
            }

            if (manager != null)
            {
                manager.RecalculateHandLayout(0);
                manager.RecalculateHandLayout(1);
            }

            isDealing = false;

            if (matchPreparationState != null)
            {
                Runner.StateMachine.ChangeState(matchPreparationState);
            }
            else if (playerTurnState != null)
            {
                Runner.StateMachine.ChangeState(playerTurnState);
            }
            else
            {
                Debug.LogError("[DealCardsState] Neither matchPreparationState nor playerTurnState transition reference is assigned!");
            }
        }

        private IEnumerator AnimateCardToPosition(GameObject cardObject, Vector3 startPos, Vector3 targetPos, Vector3 startScale, Vector3 targetScale, float duration)
        {
            if (cardObject == null) yield break;
            CardHandler cardHandler = cardObject.GetComponent<CardHandler>();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (cardObject == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 currentTargetPos = cardHandler != null ? cardHandler.OriginalPosition : targetPos;
                cardObject.transform.position = Vector3.Lerp(startPos, currentTargetPos, smoothT);
                cardObject.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                yield return null;
            }

            if (cardObject != null)
            {
                Vector3 finalPos = cardHandler != null ? cardHandler.OriginalPosition : targetPos;
                cardObject.transform.position = finalPos;
                cardObject.transform.localScale = targetScale;
                if (cardHandler != null)
                {
                    cardHandler.OriginalPosition = finalPos;
                    cardHandler.OriginalScale = targetScale;
                    cardHandler.IsAnimatingDraw = false;
                    // Enable dragging only if there is no preparation state to unlock cards later
                    cardHandler.EnableDragging = (matchPreparationState == null);
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

