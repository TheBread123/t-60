using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace T60.StateMachine
{
    public class MatchPreparationState : BaseState
    {
        [Header("Transitions")]
        [SerializeField] private BaseState playerTurnState;

        [Header("Countdown Settings")]
        [SerializeField] private float countdownDuration = 3.0f;
        [SerializeField] private float stepDuration = 1.0f;

        [Header("Interaction Settings")]
        [SerializeField] private bool allowDraggingDuringCountdown = false;

        [Header("Countdown Events")]
        [SerializeField] private UnityEvent<int> onCountdownTickEvent;
        [SerializeField] private UnityEvent onCountdownGoEvent;

        // Static events for UI Manager integration
        public static event Action OnPreparationStarted;
        public static event Action<int> OnCountdownTick;
        public static event Action OnCountdownGo;
        public static event Action OnPreparationEnded;

        private Coroutine countdownRoutine;

        public override void Enter()
        {
            base.Enter();
            Debug.Log("[MatchPreparationState] Starting preparation countdown state...");

            SetHandCardsDraggingEnabled(allowDraggingDuringCountdown);

            OnPreparationStarted?.Invoke();
            countdownRoutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            int totalTicks = Mathf.Max(1, Mathf.RoundToInt(countdownDuration / stepDuration));

            for (int count = totalTicks; count > 0; count--)
            {
                Debug.Log($"[MatchPreparationState] Countdown: {count}");
                onCountdownTickEvent?.Invoke(count);
                OnCountdownTick?.Invoke(count);
                yield return new WaitForSecondsRealtime(stepDuration);
            }

            Debug.Log("[MatchPreparationState] Countdown: GO!");
            onCountdownGoEvent?.Invoke();
            OnCountdownGo?.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);

            SetHandCardsDraggingEnabled(true);

            if (playerTurnState != null)
            {
                Runner.StateMachine.ChangeState(playerTurnState);
            }
            else
            {
                Debug.LogError("[MatchPreparationState] playerTurnState transition reference is missing!");
            }
        }

        private void SetHandCardsDraggingEnabled(bool enabled)
        {
            if (GameManager.Instance == null) return;
            for (int p = 0; p < 2; p++)
            {
                var hand = GameManager.Instance.GetPlayerHand(p);
                if (hand == null) continue;
                foreach (var cardHandler in hand)
                {
                    if (cardHandler != null)
                    {
                        cardHandler.EnableDragging = enabled;
                    }
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }
            OnPreparationEnded?.Invoke();
            Debug.Log("[MatchPreparationState] Exiting preparation state.");
        }
    }
}
