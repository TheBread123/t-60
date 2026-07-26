using System.Collections.Generic;
using UnityEngine;
using T60.StateMachine;

namespace T60.Cards
{
    [DisallowMultipleComponent]
    public class CardInputManager : MonoBehaviour
    {
        public static CardInputManager Instance { get; private set; }

        [Header("Player 1 Key Bindings (WASD - Left Player)")]
        [SerializeField] private KeyCode p1LeftKey = KeyCode.A;
        [SerializeField] private KeyCode p1RightKey = KeyCode.D;
        [SerializeField] private KeyCode p1PlayKey = KeyCode.W;

        [Header("Player 2 Key Bindings (Arrow Keys - Right Player)")]
        [SerializeField] private KeyCode p2LeftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode p2RightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode p2PlayKey = KeyCode.UpArrow;

        [Header("Mouse Integration Settings")]
        [Tooltip("When enabled, hovering over cards with mouse also updates card selection index.")]
        [SerializeField] private bool enableMouseHoverSelection = true;

        private readonly int[] selectedIndexPerPlayer = new int[2] { 0, 0 };

        public KeyCode P1LeftKey { get => p1LeftKey; set => p1LeftKey = value; }
        public KeyCode P1RightKey { get => p1RightKey; set => p1RightKey = value; }
        public KeyCode P1PlayKey { get => p1PlayKey; set => p1PlayKey = value; }

        public KeyCode P2LeftKey { get => p2LeftKey; set => p2LeftKey = value; }
        public KeyCode P2RightKey { get => p2RightKey; set => p2RightKey = value; }
        public KeyCode P2PlayKey { get => p2PlayKey; set => p2PlayKey = value; }

        public bool EnableMouseHoverSelection
        {
            get => enableMouseHoverSelection;
            set => enableMouseHoverSelection = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public int GetSelectedIndex(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 1) return 0;
            return selectedIndexPerPlayer[playerIndex];
        }

        public void ResetSelection(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 1) return;
            selectedIndexPerPlayer[playerIndex] = 0;
            Debug.Log($"[CardInputManager] Reset selection for Player {playerIndex + 1} to index 0.");
        }

        public void SetSelectedIndex(int playerIndex, int index)
        {
            if (playerIndex < 0 || playerIndex > 1) return;
            List<CardHandler> hand = GameManager.Instance != null ? GameManager.Instance.GetPlayerHand(playerIndex) : null;
            int maxIdx = hand != null && hand.Count > 0 ? hand.Count - 1 : 0;
            selectedIndexPerPlayer[playerIndex] = Mathf.Clamp(index, 0, maxIdx);
        }

        public CardHandler GetSelectedCard(int playerIndex)
        {
            if (GameManager.Instance == null) return null;
            List<CardHandler> hand = GameManager.Instance.GetPlayerHand(playerIndex);
            int idx = GetSelectedIndex(playerIndex);
            if (hand != null && idx >= 0 && idx < hand.Count)
            {
                return hand[idx];
            }
            return null;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            MatchContext context = GameManager.Instance.MatchContext;
            if (context == null || context.MatchOver) return;

            // Handle independent card navigation for Player 1 (A/D/W) and Player 2 (Left/Right/Up)
            List<CardHandler> p1Hand = GameManager.Instance.GetPlayerHand(0);
            List<CardHandler> p2Hand = GameManager.Instance.GetPlayerHand(1);

            if (p1Hand != null && p1Hand.Count > 0)
            {
                selectedIndexPerPlayer[0] = Mathf.Clamp(selectedIndexPerPlayer[0], 0, p1Hand.Count - 1);
                HandlePlayerInput(0, p1Hand, p1LeftKey, p1RightKey, p1PlayKey, context.ActivePlayerIndex == 0);
            }

            if (p2Hand != null && p2Hand.Count > 0)
            {
                selectedIndexPerPlayer[1] = Mathf.Clamp(selectedIndexPerPlayer[1], 0, p2Hand.Count - 1);
                HandlePlayerInput(1, p2Hand, p2LeftKey, p2RightKey, p2PlayKey, context.ActivePlayerIndex == 1);
            }
        }

        private void HandlePlayerInput(int playerIndex, List<CardHandler> hand, KeyCode leftKey, KeyCode rightKey, KeyCode playKey, bool isMyTurn)
        {
            int currentIdx = selectedIndexPerPlayer[playerIndex];

            if (Input.GetKeyDown(leftKey))
            {
                int nextIdx = Mathf.Max(0, currentIdx - 1);
                if (nextIdx != currentIdx)
                {
                    selectedIndexPerPlayer[playerIndex] = nextIdx;
                    Debug.Log($"[CardInputManager] Player {playerIndex + 1} selected card index {nextIdx}.");
                }
            }
            else if (Input.GetKeyDown(rightKey))
            {
                int nextIdx = Mathf.Min(hand.Count - 1, currentIdx + 1);
                if (nextIdx != currentIdx)
                {
                    selectedIndexPerPlayer[playerIndex] = nextIdx;
                    Debug.Log($"[CardInputManager] Player {playerIndex + 1} selected card index {nextIdx}.");
                }
            }
            else if (Input.GetKeyDown(playKey))
            {
                MatchStateMachineRunner runner = GameManager.Instance != null ? GameManager.Instance.MatchRunner : null;
                bool isPlayerTurnState = runner != null && runner.StateMachine != null && runner.StateMachine.CurrentState is PlayerTurnState;

                if (!isPlayerTurnState)
                {
                    string stateName = (runner != null && runner.StateMachine != null && runner.StateMachine.CurrentState != null)
                        ? runner.StateMachine.CurrentState.GetType().Name
                        : "None";
                    Debug.LogWarning($"[CardInputManager] Player {playerIndex + 1} pressed play key '{playKey}', but match is not in PlayerTurnState! Current state: {stateName}");
                    return;
                }

                if (!isMyTurn)
                {
                    Debug.LogWarning($"[CardInputManager] Player {playerIndex + 1} pressed play key '{playKey}', but it is not Player {playerIndex + 1}'s turn!");
                    return;
                }

                CardHandler selectedCard = GetSelectedCard(playerIndex);
                if (selectedCard != null && !selectedCard.IsPlayed && !selectedCard.IsAnimatingDraw && selectedCard.EnableDragging)
                {
                    Debug.Log($"[CardInputManager] Player {playerIndex + 1} pressed play key '{playKey}' for card '{selectedCard.CardData?.CardName}'.");
                    selectedCard.PlayCardFromKeyboard();
                }
                else
                {
                    Debug.LogWarning($"[CardInputManager] Player {playerIndex + 1} pressed play key, but no playable card is currently selected or card dragging is disabled.");
                }
            }
        }

        public static CardInputManager GetOrCreateInstance()
        {
            if (Instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                Instance = FindFirstObjectByType<CardInputManager>();
#else
                Instance = FindObjectOfType<CardInputManager>();
#endif
                if (Instance == null)
                {
                    GameObject inputObj = new GameObject("[CardInputManager]");
                    Instance = inputObj.AddComponent<CardInputManager>();
                }
            }
            return Instance;
        }
    }
}
