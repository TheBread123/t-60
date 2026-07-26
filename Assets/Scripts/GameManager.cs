using System.Collections.Generic;
using UnityEngine;
using T60.Cards;
using T60.StateMachine;
using System;

namespace T60
{
    [Serializable]
    public struct CardCategoryColor
    {
        public CardCategory targetCardCategory;
        public Color targetColor;
    }

    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Global Area Transforms")]
        [SerializeField] private Transform deckTransform;
        [SerializeField] private Transform dropAreaTransform;
        [SerializeField] private RectTransform playerOneHandAreaTransform;
        [SerializeField] private RectTransform playerTwoHandAreaTransform;

        [Header("System References")]
        [SerializeField] private MatchStateMachineRunner matchRunner;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private List<Card> cardSelectionCollection = new List<Card>();

        [Header("Game Settings")]
        [SerializeField] private int initialHandSize = 3;
        [SerializeField] private int maxHandSize = 6;
        [SerializeField] private int maxPlayedCardsInArea = 10;
        [SerializeField] private float dealStaggerDelay = 0.15f;
        [SerializeField] private float cardDealMoveDuration = 0.4f;
        [SerializeField] private float cardHandSpacing = 1.2f;

        [Header("Game Settings")]
        [SerializeField] private List<CardCategoryColor> cardCategoryColorCollection;

        public List<CardCategoryColor> CardCategoryColorCollection => cardCategoryColorCollection;

        public bool TryGetCategoryColor(CardCategory category, out Color color)
        {
            color = Color.white;
            if (cardCategoryColorCollection != null)
            {
                foreach (var categoryColor in cardCategoryColorCollection)
                {
                    if (categoryColor.targetCardCategory == category)
                    {
                        color = categoryColor.targetColor;
                        return true;
                    }
                }
            }
            return false;
        }

        public Color GetCategoryColor(CardCategory category)
        {
            if (TryGetCategoryColor(category, out Color color))
            {
                return color;
            }
            return Color.white;
        }

        [Header("Manager Settings")]
        [SerializeField] private bool dontDestroyOnLoad = false;

        public Transform DeckTransform
        {
            get => deckTransform;
            set => deckTransform = value;
        }

        public Transform DropAreaTransform
        {
            get => dropAreaTransform;
            set => dropAreaTransform = value;
        }

        public RectTransform PlayerOneHandAreaTransform
        {
            get => playerOneHandAreaTransform;
            set => playerOneHandAreaTransform = value;
        }

        public RectTransform PlayerTwoHandAreaTransform
        {
            get => playerTwoHandAreaTransform;
            set => playerTwoHandAreaTransform = value;
        }

        public MatchStateMachineRunner MatchRunner
        {
            get
            {
                if (matchRunner == null)
                {
#if UNITY_2023_1_OR_NEWER
                    matchRunner = FindFirstObjectByType<MatchStateMachineRunner>();
#else
                    matchRunner = FindObjectOfType<MatchStateMachineRunner>();
#endif
                }
                return matchRunner;
            }
            set => matchRunner = value;
        }

        public GameObject CardPrefab
        {
            get => cardPrefab;
            set => cardPrefab = value;
        }

        public List<Card> CardSelectionCollection => cardSelectionCollection;

        public int InitialHandSize => initialHandSize;
        public int MaxHandSize => maxHandSize;
        public int MaxPlayedCardsInArea => maxPlayedCardsInArea;
        public float DealStaggerDelay => dealStaggerDelay;
        public float CardDealMoveDuration => cardDealMoveDuration;
        public float CardHandSpacing => cardHandSpacing;

        public MatchContext MatchContext => MatchRunner != null ? MatchRunner.Context : null;

        public Vector3 DeckPosition => deckTransform != null ? deckTransform.position : Vector3.zero;
        public Vector3 DeckScale => deckTransform != null ? deckTransform.localScale : Vector3.one;
        public Vector3 DropAreaPosition => dropAreaTransform != null ? dropAreaTransform.position : Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (CardInputManager.Instance == null)
            {
                CardInputManager.GetOrCreateInstance();
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Card GetRandomCard()
        {
            if (cardSelectionCollection == null || cardSelectionCollection.Count == 0)
            {
                Debug.LogWarning("[GameManager] Card selection collection is empty!");
                return null;
            }

            // Higher Card.Weight = rarer draw, so cards are chosen proportional to 1/weight.
            float totalSelectionWeight = 0f;
            foreach (var card in cardSelectionCollection)
            {
                totalSelectionWeight += 1f / Mathf.Max(1, card != null ? card.Weight : 1);
            }

            float roll = UnityEngine.Random.Range(0f, totalSelectionWeight);
            float cumulative = 0f;
            foreach (var card in cardSelectionCollection)
            {
                cumulative += 1f / Mathf.Max(1, card != null ? card.Weight : 1);
                if (roll <= cumulative)
                {
                    return card;
                }
            }

            return cardSelectionCollection[cardSelectionCollection.Count - 1];
        }

        public Vector3 GetHandCardWorldPosition(int playerIndex, int cardIndex, int totalCards, Camera cam = null)
        {
            RectTransform handRect = (playerIndex == 0) ? playerOneHandAreaTransform : playerTwoHandAreaTransform;
            if (handRect == null) return Vector3.zero;

            if (cam == null) cam = Camera.main;
            if (cam == null) return handRect.position;

            Canvas canvas = handRect.GetComponentInParent<Canvas>();
            Vector2 screenPoint;

            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenPoint = handRect.position;
            }
            else
            {
                Camera canvasCam = (canvas != null && canvas.worldCamera != null) ? canvas.worldCamera : cam;
                screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, handRect.position);
            }

            float zDist = Mathf.Abs(cam.transform.position.z);
            Vector3 centerWorld = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, zDist));
            centerWorld.z = 0f;

            float offset = (cardIndex - (totalCards - 1) * 0.5f) * cardHandSpacing;
            return centerWorld + new Vector3(offset, 0f, 0f);
        }

        private readonly List<CardHandler> playerOneHand = new List<CardHandler>();
        private readonly List<CardHandler> playerTwoHand = new List<CardHandler>();
        private readonly List<CardHandler> playedCards = new List<CardHandler>();

        public List<CardHandler> PlayerOneHand => playerOneHand;
        public List<CardHandler> PlayerTwoHand => playerTwoHand;
        public List<CardHandler> PlayedCards => playedCards;

        public List<CardHandler> GetPlayerHand(int playerIndex)
        {
            return playerIndex == 0 ? playerOneHand : playerTwoHand;
        }

        public void RegisterCardInHand(int playerIndex, CardHandler card)
        {
            if (card == null) return;
            card.PlayerOwner = playerIndex;
            List<CardHandler> hand = GetPlayerHand(playerIndex);
            if (!hand.Contains(card))
            {
                hand.Add(card);
            }
        }

        public void RemoveCardFromHand(CardHandler card)
        {
            if (card == null) return;
            playerOneHand.Remove(card);
            playerTwoHand.Remove(card);
        }

        public void RegisterPlayedCard(CardHandler card)
        {
            if (card == null) return;
            RemoveCardFromHand(card);
            if (!playedCards.Contains(card))
            {
                playedCards.Add(card);
            }

            while (maxPlayedCardsInArea > 0 && playedCards.Count > maxPlayedCardsInArea)
            {
                CardHandler oldestCard = playedCards[0];
                playedCards.RemoveAt(0);
                if (oldestCard != null && oldestCard.gameObject != null)
                {
                    Debug.Log($"[GameManager] Recycled oldest played card '{oldestCard.CardData?.CardName}' with fade out animation as play area limit of {maxPlayedCardsInArea} was reached.");
                    oldestCard.FadeOutAndDespawn(0.4f);
                }
            }
        }

        public void RecalculateHandLayout(int playerIndex)
        {
            List<CardHandler> hand = GetPlayerHand(playerIndex);
            int count = hand.Count;
            for (int i = 0; i < count; i++)
            {
                if (hand[i] != null && !hand[i].IsPlayed)
                {
                    Vector3 pos = GetHandCardWorldPosition(playerIndex, i, count);
                    hand[i].OriginalPosition = pos;
                }
            }
        }

        public void ClearAllCards()
        {
            foreach (var card in playerOneHand)
            {
                if (card != null && card.gameObject != null)
                    T60.Pooling.ObjectPoolManager.DespawnObject(card.gameObject);
            }
            foreach (var card in playerTwoHand)
            {
                if (card != null && card.gameObject != null)
                    T60.Pooling.ObjectPoolManager.DespawnObject(card.gameObject);
            }
            foreach (var card in playedCards)
            {
                if (card != null && card.gameObject != null)
                    T60.Pooling.ObjectPoolManager.DespawnObject(card.gameObject);
            }
            playerOneHand.Clear();
            playerTwoHand.Clear();
            playedCards.Clear();

            // Safety fallback for any active CardHandlers in scene
#if UNITY_2023_1_OR_NEWER
            CardHandler[] sceneCards = FindObjectsByType<CardHandler>(FindObjectsSortMode.None);
#else
            CardHandler[] sceneCards = FindObjectsOfType<CardHandler>();
#endif
            foreach (var card in sceneCards)
            {
                if (card != null && card.gameObject != null && card.gameObject.activeSelf)
                {
                    T60.Pooling.ObjectPoolManager.DespawnObject(card.gameObject);
                }
            }

            if (T60.Pooling.ObjectPoolManager.Instance != null)
            {
                T60.Pooling.ObjectPoolManager.Instance.DespawnAllActive();
            }
        }

        public CardHandler DrawCardForPlayer(int playerIndex, Transform spawnTransform = null)
        {
            List<CardHandler> hand = GetPlayerHand(playerIndex);
            if (hand != null && hand.Count >= maxHandSize)
            {
                Debug.LogWarning($"[GameManager] Player {playerIndex + 1} hand is full ({hand.Count}/{maxHandSize} cards). Cannot draw additional card.");
                return null;
            }

            Card templateCard = GetRandomCard();
            if (templateCard == null) return null;

            GameObject prefabToSpawn = CardPrefab;
            if (prefabToSpawn == null) return null;

            Vector3 spawnPos = spawnTransform != null ? spawnTransform.position : DeckPosition;
            Vector3 startScale = spawnTransform != null ? spawnTransform.localScale : DeckScale;
            Vector3 targetScale = prefabToSpawn.transform.localScale;

            GameObject cardObj = T60.Pooling.ObjectPoolManager.SpawnObject(prefabToSpawn, spawnPos, Quaternion.identity);
            if (cardObj != null && cardObj.TryGetComponent<CardHandler>(out var cardHandler))
            {
                cardHandler.SetCard(templateCard);
                cardHandler.PlayerOwner = playerIndex;
                cardHandler.EnableDragging = false;
                cardHandler.IsAnimatingDraw = true;
                cardHandler.OriginalScale = targetScale;
                cardObj.transform.localScale = startScale;

                RegisterCardInHand(playerIndex, cardHandler);
                RecalculateHandLayout(playerIndex);

                StartCoroutine(AnimateCardDrawRoutine(cardHandler, spawnPos, startScale, targetScale, cardDealMoveDuration));
                return cardHandler;
            }
            return null;
        }

        private System.Collections.IEnumerator AnimateCardDrawRoutine(CardHandler cardHandler, Vector3 startPos, Vector3 startScale, Vector3 targetScale, float duration)
        {
            if (cardHandler == null) yield break;

            GameObject cardObj = cardHandler.gameObject;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (cardObj == null || cardHandler == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 currentTargetPos = cardHandler.OriginalPosition;
                cardObj.transform.position = Vector3.Lerp(startPos, currentTargetPos, smoothT);
                cardObj.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                yield return null;
            }

            if (cardHandler != null && cardObj != null)
            {
                cardObj.transform.position = cardHandler.OriginalPosition;
                cardObj.transform.localScale = targetScale;
                cardHandler.OriginalScale = targetScale;
                cardHandler.IsAnimatingDraw = false;
                cardHandler.EnableDragging = true;
            }
        }

        public static GameManager GetOrCreateInstance()
        {
            if (Instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                Instance = FindFirstObjectByType<GameManager>();
#else
                Instance = FindObjectOfType<GameManager>();
#endif
                if (Instance == null)
                {
                    GameObject managerObj = new GameObject("[GameManager]");
                    Instance = managerObj.AddComponent<GameManager>();
                }
            }

            return Instance;
        }
    }
}
