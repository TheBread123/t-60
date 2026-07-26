using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using TMPro;
using T60.StateMachine;
using T60.Pooling;
using T60.UI;

namespace T60.Cards
{
    [DisallowMultipleComponent]
    public class CardHandler : MonoBehaviour, IPoolable
    {
        [Header("Card Data")]
        [SerializeField] private Card cardData;
        [SerializeField] private int playerOwner = 0;

        [Header("Visual Components (2D / UI)")]
        [SerializeField] private SpriteRenderer rootRenderer;
        [SerializeField] private SpriteRenderer artRenderer;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private AlphaGroupFader alphaFader;

        public AlphaGroupFader AlphaFader => alphaFader;

        public SpriteRenderer RootRenderer
        {
            get => rootRenderer;
            set => rootRenderer = value;
        }

        public SpriteRenderer ArtRenderer
        {
            get => artRenderer;
            set => artRenderer = value;
        }

        [Header("Drag & Drop Settings")]
        [SerializeField] private bool enableDragging = true;
        [SerializeField] private float dropDistanceThreshold = 2.5f;
        [SerializeField] private Transform dropTargetTransform;
        [SerializeField] private bool returnToOriginOnInvalidDrop = true;
        [SerializeField] private float returnSpeed = 15f;
        [SerializeField] private int dragSortingOrderOffset = 50;

        [Header("Hover & Turn Highlight Settings")]
        [SerializeField] private float hoverScaleMultiplier = 1.15f;
        [SerializeField] private float activeTurnScaleMultiplier = 1.05f;
        [SerializeField] private float activeTurnLiftAmount = 0.35f;
        [SerializeField] private float scaleLerpSpeed = 12f;

        [Header("Played Card Settings")]
        [SerializeField] private float playedPositionOffsetRadius = 0.4f;
        [SerializeField] private float playedTiltMaxAngle = 15f;
        [SerializeField] private float playedMoveSpeed = 15f;

        [Header("Events")]
        public UnityEvent<CardHandler> OnCardPlayed;

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Vector3 dragOffset;
        private bool isDragging;
        private bool isHovered;
        private bool isReturningToOrigin;
        private bool hasExplicitOriginalPosition;
        private Camera mainCamera;

        private bool isPlayed = false;
        private Vector3 playedTargetPosition;
        private Quaternion playedTargetRotation;
        private static int globalPlayedSortingOrder = 1000;

        private SortingGroup cachedSortingGroup;
        private int originalGroupSortingOrder;
        private readonly Dictionary<Renderer, int> cachedRenderersSortingOrder = new Dictionary<Renderer, int>();

        private static readonly Dictionary<int, CardHandler> hoveredCardPerPlayer = new Dictionary<int, CardHandler>();

        public static CardHandler GetHoveredCard(int playerIndex)
        {
            if (hoveredCardPerPlayer.TryGetValue(playerIndex, out CardHandler card))
            {
                return card;
            }
            return null;
        }

        public Card CardData
        {
            get => cardData;
            set
            {
                cardData = value;
                RefreshCard();
            }
        }

        public int PlayerOwner
        {
            get => playerOwner;
            set => playerOwner = value;
        }

        public Vector3 OriginalPosition
        {
            get => originalPosition;
            set
            {
                originalPosition = value;
                hasExplicitOriginalPosition = true;
            }
        }

        public Vector3 OriginalScale
        {
            get => originalScale;
            set => originalScale = value;
        }

        public bool IsAnimatingDraw { get; set; }

        public bool EnableDragging
        {
            get => enableDragging;
            set => enableDragging = value;
        }

        public float DropDistanceThreshold
        {
            get => dropDistanceThreshold;
            set => dropDistanceThreshold = value;
        }

        public bool IsPlayed => isPlayed;

        public void OnSpawn()
        {
            hasExplicitOriginalPosition = false;
            isPlayed = false;
            isDragging = false;
            isHovered = false;
            isReturningToOrigin = false;
            IsAnimatingDraw = false;

            EnsureAlphaFader();
            if (alphaFader != null)
            {
                alphaFader.OnSpawn();
            }
        }

        public void OnDespawn()
        {
            hasExplicitOriginalPosition = false;
            isPlayed = false;
            isDragging = false;
            isHovered = false;
            isReturningToOrigin = false;
            IsAnimatingDraw = false;

            if (hoveredCardPerPlayer.TryGetValue(playerOwner, out var current) && current == this)
            {
                hoveredCardPerPlayer.Remove(playerOwner);
            }

            if (alphaFader != null)
            {
                alphaFader.OnDespawn();
            }
        }

        private void OnDisable()
        {
            if (hoveredCardPerPlayer.TryGetValue(playerOwner, out var current) && current == this)
            {
                hoveredCardPerPlayer.Remove(playerOwner);
            }
        }

        public void EnsureAlphaFader()
        {
            if (alphaFader == null)
            {
                if (!TryGetComponent<AlphaGroupFader>(out alphaFader))
                {
                    alphaFader = gameObject.AddComponent<AlphaGroupFader>();
                }
            }
        }

        public void SetAlpha(float alpha)
        {
            EnsureAlphaFader();
            if (alphaFader != null)
            {
                alphaFader.SetAlpha(alpha);
            }
        }

        public void FadeTo(float targetAlpha, float duration = -1f, System.Action onComplete = null)
        {
            EnsureAlphaFader();
            if (alphaFader != null)
            {
                alphaFader.FadeTo(targetAlpha, duration, onComplete);
            }
        }

        public void FadeOutAndDespawn(float duration = -1f, System.Action onDespawned = null)
        {
            EnsureAlphaFader();
            if (alphaFader != null)
            {
                alphaFader.FadeOutAndDespawn(duration, onDespawned);
            }
            else
            {
                ObjectPoolManager.DespawnObject(gameObject);
            }
        }

        private void Reset()
        {
            EnsureCollider();
            EnsureAlphaFader();
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            EnsureCollider();
            EnsureAlphaFader();
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
            }
            RefreshCard();
        }

        private void Start()
        {
            if (!hasExplicitOriginalPosition)
            {
                originalPosition = transform.position;
            }
            originalRotation = transform.rotation;
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
            }
            RefreshCard();
        }

        private void Update()
        {
            if (IsAnimatingDraw) return;

            float dt = Time.unscaledDeltaTime;

            if (isPlayed)
            {
                transform.position = Vector3.Lerp(transform.position, playedTargetPosition, dt * playedMoveSpeed);
                transform.rotation = Quaternion.Lerp(transform.rotation, playedTargetRotation, dt * playedMoveSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, dt * scaleLerpSpeed);
                return;
            }

            MatchContext context = GameManager.Instance != null ? GameManager.Instance.MatchContext : null;
            bool isActivePlayerTurn = (context != null && context.ActivePlayerIndex == playerOwner);

            int selectedIdx = (CardInputManager.Instance != null) ? CardInputManager.Instance.GetSelectedIndex(playerOwner) : -1;
            List<CardHandler> hand = (GameManager.Instance != null) ? GameManager.Instance.GetPlayerHand(playerOwner) : null;
            int cardIndexInHand = (hand != null) ? hand.IndexOf(this) : -1;
            bool isSelectedInHand = (cardIndexInHand >= 0 && cardIndexInHand == selectedIdx);

            if (isReturningToOrigin)
            {
                transform.position = Vector3.Lerp(transform.position, originalPosition, dt * returnSpeed);
                transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, dt * returnSpeed);

                if (Vector3.Distance(transform.position, originalPosition) < 0.01f)
                {
                    transform.position = originalPosition;
                    transform.rotation = originalRotation;
                    isReturningToOrigin = false;
                }
            }
            else if (!isDragging && enableDragging)
            {
                float activeTurnLift = isActivePlayerTurn ? activeTurnLiftAmount : 0f;
                float selectedLift = (isSelectedInHand || isHovered) ? (activeTurnLiftAmount * 1.2f) : 0f;
                Vector3 targetPos = originalPosition + new Vector3(0f, activeTurnLift + selectedLift, 0f);
                transform.position = Vector3.Lerp(transform.position, targetPos, dt * scaleLerpSpeed);
            }

            Vector3 targetScale = originalScale;
            if (isDragging)
            {
                targetScale = originalScale * 1.08f;
            }
            else if (isSelectedInHand || isHovered)
            {
                targetScale = originalScale * hoverScaleMultiplier;
            }
            else if (isActivePlayerTurn)
            {
                targetScale = originalScale * activeTurnScaleMultiplier;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, dt * scaleLerpSpeed);
        }

        public void EnsureCollider()
        {
            bool hasCollider2D = TryGetComponent<Collider2D>(out _);
            bool hasCollider3D = TryGetComponent<Collider>(out _);

            if (!hasCollider2D && !hasCollider3D)
            {
                BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();

                if (TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
                {
                    boxCol.size = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
                }

                Debug.Log($"[CardHandler] No Collider detected on '{gameObject.name}'. Auto-added BoxCollider2D for drag interaction.");
            }
        }

        public void SetCard(Card newCard)
        {
            cardData = newCard;
            RefreshCard();
        }

        public void RefreshCard()
        {
            if (rootRenderer == null)
            {
                rootRenderer = GetComponent<SpriteRenderer>();
            }

            if (cardData == null)
            {
                if (nameText != null) nameText.text = "";
                if (descriptionText != null) descriptionText.text = "";
                if (artRenderer != null)
                {
                    artRenderer.sprite = null;
                    artRenderer.enabled = false;
                }
                if (rootRenderer != null)
                {
                    rootRenderer.color = Color.white;
                }
                return;
            }

            if (nameText != null)
            {
                nameText.text = !string.IsNullOrEmpty(cardData.CardName) ? cardData.CardName : "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = !string.IsNullOrEmpty(cardData.Description) ? cardData.Description : "";
            }

            if (artRenderer != null)
            {
                artRenderer.sprite = cardData.Art;
                artRenderer.enabled = (cardData.Art != null);
            }

            if (rootRenderer != null)
            {
                if (GameManager.Instance != null && GameManager.Instance.TryGetCategoryColor(cardData.CardCategory, out Color categoryColor))
                {
                    rootRenderer.color = categoryColor;
                }
                else
                {
                    rootRenderer.color = Color.white;
                }
            }
        }

        private void OnMouseEnter()
        {
            if (isPlayed) return;

            isHovered = true;
            hoveredCardPerPlayer[playerOwner] = this;

            if (!isDragging)
            {
                ElevateSortingOrder();
            }

            if (CardInputManager.Instance != null && CardInputManager.Instance.EnableMouseHoverSelection)
            {
                List<CardHandler> hand = GameManager.Instance != null ? GameManager.Instance.GetPlayerHand(playerOwner) : null;
                if (hand != null)
                {
                    int myIdx = hand.IndexOf(this);
                    if (myIdx >= 0)
                    {
                        CardInputManager.Instance.SetSelectedIndex(playerOwner, myIdx);
                    }
                }
            }
        }

        private void OnMouseExit()
        {
            if (isPlayed) return;

            isHovered = false;
            if (hoveredCardPerPlayer.TryGetValue(playerOwner, out var current) && current == this)
            {
                hoveredCardPerPlayer.Remove(playerOwner);
            }

            if (!isDragging)
            {
                RestoreSortingOrder();
            }
        }

        private void OnMouseDown()
        {
            if (isPlayed || !enableDragging) return;

            MatchStateMachineRunner runner = GameManager.Instance != null ? GameManager.Instance.MatchRunner : null;
            if (runner != null && runner.StateMachine != null && !(runner.StateMachine.CurrentState is PlayerTurnState))
            {
                string stateName = runner.StateMachine.CurrentState != null ? runner.StateMachine.CurrentState.GetType().Name : "None";
                Debug.LogWarning($"[CardHandler] Cannot interact with card '{cardData?.CardName}' outside of PlayerTurnState! Current state: {stateName}");
                return;
            }

            MatchContext context = GameManager.Instance != null ? GameManager.Instance.MatchContext : null;
            if (context != null && playerOwner != context.ActivePlayerIndex)
            {
                Debug.LogWarning($"[CardHandler] Cannot interact with card owned by Player {playerOwner + 1} on Player {context.ActivePlayerIndex + 1}'s turn!");
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            isReturningToOrigin = false;
            isDragging = true;

            originalRotation = transform.rotation;

            ElevateSortingOrder();

            dragOffset = transform.position - GetMouseWorldPosition();
        }

        private void OnMouseDrag()
        {
            if (isPlayed || !isDragging) return;

            transform.position = GetMouseWorldPosition() + dragOffset;
        }

        private void OnMouseUp()
        {
            if (isPlayed || !isDragging) return;

            isDragging = false;
            isHovered = false;

            RestoreSortingOrder();

            Vector3 baseTargetPosition = GetDropTargetPosition();
            float distance = Vector3.Distance(transform.position, baseTargetPosition);

            if (distance <= dropDistanceThreshold)
            {
                Vector2 randomOffset = Random.insideUnitCircle * playedPositionOffsetRadius;
                playedTargetPosition = baseTargetPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
                float randomZAngle = Random.Range(-playedTiltMaxAngle, playedTiltMaxAngle);
                playedTargetRotation = Quaternion.Euler(0f, 0f, randomZAngle);

                isPlayed = true;
                enableDragging = false;

                DisableColliders();
                SetPlayedSortingOrder(globalPlayedSortingOrder++);

                ExecuteCardPlay();
            }
            else if (returnToOriginOnInvalidDrop)
            {
                isReturningToOrigin = true;
            }
        }

        public void PlayCardFromKeyboard()
        {
            if (isPlayed || IsAnimatingDraw || !enableDragging) return;

            MatchStateMachineRunner runner = GameManager.Instance != null ? GameManager.Instance.MatchRunner : null;
            if (runner != null && runner.StateMachine != null && !(runner.StateMachine.CurrentState is PlayerTurnState))
            {
                string stateName = runner.StateMachine.CurrentState != null ? runner.StateMachine.CurrentState.GetType().Name : "None";
                Debug.LogWarning($"[CardHandler] Cannot play card '{cardData?.CardName}' outside of PlayerTurnState! Current state: {stateName}");
                return;
            }

            MatchContext context = GameManager.Instance != null ? GameManager.Instance.MatchContext : null;
            if (context != null && playerOwner != context.ActivePlayerIndex)
            {
                Debug.LogWarning($"[CardHandler] Cannot play card owned by Player {playerOwner + 1} on Player {context.ActivePlayerIndex + 1}'s turn!");
                return;
            }

            Vector3 baseTargetPosition = GetDropTargetPosition();
            Vector2 randomOffset = Random.insideUnitCircle * playedPositionOffsetRadius;
            playedTargetPosition = baseTargetPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
            float randomZAngle = Random.Range(-playedTiltMaxAngle, playedTiltMaxAngle);
            playedTargetRotation = Quaternion.Euler(0f, 0f, randomZAngle);

            isPlayed = true;
            enableDragging = false;
            isDragging = false;
            isHovered = false;

            DisableColliders();
            SetPlayedSortingOrder(globalPlayedSortingOrder++);

            ExecuteCardPlay();
        }

        private void DisableColliders()
        {
            if (TryGetComponent<Collider2D>(out var col2D))
            {
                col2D.enabled = false;
            }
            if (TryGetComponent<Collider>(out var col3D))
            {
                col3D.enabled = false;
            }
        }

        private void SetPlayedSortingOrder(int order)
        {
            if (TryGetComponent<SortingGroup>(out var sg))
            {
                sg.sortingOrder = order;
            }
            else
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        r.sortingOrder = order;
                    }
                }
            }
        }

        private void ElevateSortingOrder()
        {
            if (TryGetComponent<SortingGroup>(out cachedSortingGroup))
            {
                originalGroupSortingOrder = cachedSortingGroup.sortingOrder;
                cachedSortingGroup.sortingOrder = originalGroupSortingOrder + dragSortingOrderOffset;
            }
            else
            {
                cachedRenderersSortingOrder.Clear();
                Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
                foreach (var r in childRenderers)
                {
                    if (r != null)
                    {
                        cachedRenderersSortingOrder[r] = r.sortingOrder;
                        r.sortingOrder += dragSortingOrderOffset;
                    }
                }
            }
        }

        private void RestoreSortingOrder()
        {
            if (cachedSortingGroup != null)
            {
                cachedSortingGroup.sortingOrder = originalGroupSortingOrder;
                cachedSortingGroup = null;
            }
            else if (cachedRenderersSortingOrder.Count > 0)
            {
                foreach (var kvp in cachedRenderersSortingOrder)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.sortingOrder = kvp.Value;
                    }
                }
                cachedRenderersSortingOrder.Clear();
            }
        }

        private Vector3 GetDropTargetPosition()
        {
            if (dropTargetTransform != null)
            {
                return dropTargetTransform.position;
            }

            if (GameManager.Instance != null && GameManager.Instance.DropAreaTransform != null)
            {
                return GameManager.Instance.DropAreaTransform.position;
            }

            if (mainCamera != null)
            {
                Vector3 centerScreenPoint = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, Mathf.Abs(mainCamera.transform.position.z - transform.position.z));
                return mainCamera.ScreenToWorldPoint(centerScreenPoint);
            }

            return Vector3.zero;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (mainCamera == null) return transform.position;

            Vector3 mousePoint = Input.mousePosition;
            mousePoint.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            return mainCamera.ScreenToWorldPoint(mousePoint);
        }

        private void ExecuteCardPlay()
        {
            if (cardData == null)
            {
                Debug.LogWarning($"[CardHandler] Cannot play card on '{gameObject.name}': CardData is null.");
                return;
            }

            Debug.Log($"[CardHandler] Card '{cardData.CardName}' dropped in target area! Triggering effect...");

            MatchStateMachineRunner runner = null;
            if (GameManager.Instance != null && GameManager.Instance.MatchRunner != null)
            {
                runner = GameManager.Instance.MatchRunner;
            }
            else
            {
#if UNITY_2023_1_OR_NEWER
                runner = FindFirstObjectByType<MatchStateMachineRunner>();
#else
                runner = FindObjectOfType<MatchStateMachineRunner>();
#endif
            }

            if (runner != null)
            {
                if (runner.StateMachine != null && runner.StateMachine.CurrentState is PlayerTurnState playerTurnState)
                {
                    playerTurnState.PlayCard(cardData);
                }
                else if (runner.StateMachine != null && runner.StateMachine.CurrentState != null)
                {
                    Debug.LogWarning($"[CardHandler] Prevented playing card '{cardData.CardName}' outside of PlayerTurnState! Current state: {runner.StateMachine.CurrentState.GetType().Name}");
                    return;
                }
                else if (runner.Context != null)
                {
                    cardData.Play(runner.Context);
                }
                else
                {
                    Debug.LogWarning("[CardHandler] MatchStateMachineRunner present but MatchContext is null!");
                }
            }
            else
            {
                Debug.LogWarning("[CardHandler] MatchStateMachineRunner not found in scene. Played card effect locally without context.");
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayedCard(this);
                GameManager.Instance.RecalculateHandLayout(playerOwner);
            }

            OnCardPlayed?.Invoke(this);
        }
    }
}

