using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using T60.StateMachine;

namespace T60.UI
{
    [DisallowMultipleComponent]
    public class TurnIndicatorPanelHandler : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private Graphic leftBlueGraphic;
        [SerializeField] private Graphic rightRedGraphic;

        [Header("Colors")]
        [SerializeField] private Color activeBlueColor = new Color(0.12f, 0.53f, 0.90f, 1.0f);
        [SerializeField] private Color inactiveBlueColor = new Color(0.45f, 0.45f, 0.50f, 0.5f);
        [SerializeField] private Color activeRedColor = new Color(0.90f, 0.22f, 0.21f, 1.0f);
        [SerializeField] private Color inactiveRedColor = new Color(0.50f, 0.45f, 0.45f, 0.5f);

        [Header("Settings")]
        [SerializeField] private float transitionDuration = 0.25f;

        private Coroutine transitionRoutine;

        private void Awake()
        {
            ResetToInactive();
        }

        private void OnEnable()
        {
            PlayerTurnState.OnTurnStarted += SetTurn;
            DealCardsState.OnDealCardsStarted += ResetToInactive;
            MatchPreparationState.OnPreparationStarted += ResetToInactive;
        }

        private void OnDisable()
        {
            PlayerTurnState.OnTurnStarted -= SetTurn;
            DealCardsState.OnDealCardsStarted -= ResetToInactive;
            MatchPreparationState.OnPreparationStarted -= ResetToInactive;
        }

        public void ResetToInactive()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
            ApplyColors(inactiveBlueColor, inactiveRedColor);
        }

        public void SetTurn(int playerIndex) => SetTurn(playerIndex, false);

        public void SetTurn(int playerIndex, bool immediate)
        {
            bool isP1 = (playerIndex == 0);

            Color targetBlue = isP1 ? activeBlueColor : inactiveBlueColor;
            Color targetRed = isP1 ? inactiveRedColor : activeRedColor;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (immediate || transitionDuration <= 0f)
            {
                ApplyColors(targetBlue, targetRed);
            }
            else
            {
                transitionRoutine = StartCoroutine(AnimateTransition(targetBlue, targetRed));
            }
        }

        private void ApplyColors(Color blue, Color red)
        {
            if (leftBlueGraphic != null) leftBlueGraphic.color = blue;
            if (rightRedGraphic != null) rightRedGraphic.color = red;
        }

        private IEnumerator AnimateTransition(Color targetBlue, Color targetRed)
        {
            Color startBlue = leftBlueGraphic != null ? leftBlueGraphic.color : targetBlue;
            Color startRed = rightRedGraphic != null ? rightRedGraphic.color : targetRed;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / transitionDuration;

                ApplyColors(Color.Lerp(startBlue, targetBlue, t), Color.Lerp(startRed, targetRed, t));
                yield return null;
            }

            ApplyColors(targetBlue, targetRed);
            transitionRoutine = null;
        }
    }
}
