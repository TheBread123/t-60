using System.Collections.Generic;
using UnityEngine;
using T60.Cards;
using T60.StateMachine;

namespace T60
{
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
        [SerializeField] private float dealStaggerDelay = 0.15f;
        [SerializeField] private float cardDealMoveDuration = 0.4f;
        [SerializeField] private float cardHandSpacing = 1.2f;

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
        public float DealStaggerDelay => dealStaggerDelay;
        public float CardDealMoveDuration => cardDealMoveDuration;
        public float CardHandSpacing => cardHandSpacing;

        public MatchContext MatchContext => MatchRunner != null ? MatchRunner.Context : null;

        public Vector3 DeckPosition => deckTransform != null ? deckTransform.position : Vector3.zero;
        public Vector3 DropAreaPosition => dropAreaTransform != null ? dropAreaTransform.position : Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

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

            int randomIndex = Random.Range(0, cardSelectionCollection.Count);
            return cardSelectionCollection[randomIndex];
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
