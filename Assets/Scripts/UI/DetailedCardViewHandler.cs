using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using T60.Cards;

namespace T60.UI
{
    [DisallowMultipleComponent]
    public class DetailedCardViewHandler : MonoBehaviour
    {
        [Header("Player Target Settings")]
        [Tooltip("Target player index for this detailed card view (0 = Player 1, 1 = Player 2).")]
        [SerializeField] private int targetPlayerIndex = 0;

        [Header("UI Components (Canvas Overlay)")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image artImage;
        [SerializeField] private Image categoryBackgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Display & Fade Options")]
        [SerializeField] private bool hidePanelWhenNoCard = false;
        [SerializeField] private bool preferHoverOverKeyboardSelection = true;
        [SerializeField] private bool useFadeAnimation = false;
        [SerializeField] private float fadeDuration = 0.2f;

        private Card currentDisplayedCard;
        private Coroutine activeFadeRoutine;

        public int TargetPlayerIndex
        {
            get => targetPlayerIndex;
            set => targetPlayerIndex = value;
        }

        public Card CurrentDisplayedCard => currentDisplayedCard;

        public TMP_Text NameText
        {
            get => nameText;
            set => nameText = value;
        }

        public TMP_Text DescriptionText
        {
            get => descriptionText;
            set => descriptionText = value;
        }

        public Image ArtImage
        {
            get => artImage;
            set => artImage = value;
        }

        public Image CategoryBackgroundImage
        {
            get => categoryBackgroundImage;
            set => categoryBackgroundImage = value;
        }

        public CanvasGroup ViewCanvasGroup
        {
            get => canvasGroup;
            set => canvasGroup = value;
        }

        public bool UseFadeAnimation
        {
            get => useFadeAnimation;
            set => useFadeAnimation = value;
        }

        public float FadeDuration
        {
            get => fadeDuration;
            set => fadeDuration = value;
        }

        private void Awake()
        {
            EnsureCanvasGroup();
        }

        public void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                if (!TryGetComponent<CanvasGroup>(out canvasGroup))
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Start()
        {
            RefreshView(true);
        }

        private void Update()
        {
            RefreshView();
        }

        public void RefreshView(bool isForcedRefresh = false)
        {
            CardHandler targetCardHandler = GetActiveTargetCard();
            Card targetCardData = targetCardHandler != null ? targetCardHandler.CardData : null;

            if (isForcedRefresh || targetCardData != currentDisplayedCard)
            {
                currentDisplayedCard = targetCardData;
                UpdateUI(targetCardData);
            }
        }

        public CardHandler GetActiveTargetCard()
        {
            if (preferHoverOverKeyboardSelection)
            {
                CardHandler hovered = CardHandler.GetHoveredCard(targetPlayerIndex);
                if (hovered != null && hovered.CardData != null && !hovered.IsPlayed)
                {
                    return hovered;
                }
            }

            if (CardInputManager.Instance != null)
            {
                CardHandler selected = CardInputManager.Instance.GetSelectedCard(targetPlayerIndex);
                if (selected != null && selected.CardData != null && !selected.IsPlayed)
                {
                    return selected;
                }
            }

            return null;
        }

        public void SetCard(Card card)
        {
            currentDisplayedCard = card;
            UpdateUI(card);
        }

        public void ClearView()
        {
            currentDisplayedCard = null;
            UpdateUI(null);
        }

        public void SetAlpha(float alpha)
        {
            if (activeFadeRoutine != null)
            {
                StopCoroutine(activeFadeRoutine);
                activeFadeRoutine = null;
            }

            EnsureCanvasGroup();
            if (canvasGroup != null)
            {
                float clampedAlpha = Mathf.Clamp01(alpha);
                canvasGroup.alpha = clampedAlpha;
                canvasGroup.blocksRaycasts = clampedAlpha > 0.01f;
            }
        }

        public void FadeTo(float targetAlpha, float duration = -1f, Action onComplete = null)
        {
            float dur = duration >= 0f ? duration : fadeDuration;

            EnsureCanvasGroup();
            if (canvasGroup != null)
            {
                if (activeFadeRoutine != null)
                {
                    StopCoroutine(activeFadeRoutine);
                }
                activeFadeRoutine = StartCoroutine(CanvasGroupFadeRoutine(targetAlpha, dur, onComplete));
            }
        }

        private IEnumerator CanvasGroupFadeRoutine(float targetAlpha, float duration, Action onComplete)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            float clampedTarget = Mathf.Clamp01(targetAlpha);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, clampedTarget, t);
                canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.01f;
                yield return null;
            }

            canvasGroup.alpha = clampedTarget;
            canvasGroup.blocksRaycasts = clampedTarget > 0.01f;
            activeFadeRoutine = null;
            onComplete?.Invoke();
        }

        private void SetPanelVisibility(bool isVisible)
        {
            float targetAlpha = (hidePanelWhenNoCard && !isVisible) ? 0f : 1f;

            if (useFadeAnimation && Application.isPlaying)
            {
                FadeTo(targetAlpha);
            }
            else
            {
                SetAlpha(targetAlpha);
            }
        }

        private void UpdateUI(Card card)
        {
            if (card == null)
            {
                if (nameText != null) nameText.text = "";
                if (descriptionText != null) descriptionText.text = "";
                if (artImage != null)
                {
                    artImage.sprite = null;
                    artImage.enabled = false;
                }
                if (categoryBackgroundImage != null)
                {
                    categoryBackgroundImage.color = Color.clear;
                }
                SetPanelVisibility(false);
                return;
            }

            SetPanelVisibility(true);

            if (nameText != null)
            {
                nameText.text = !string.IsNullOrEmpty(card.CardName) ? card.CardName : "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = !string.IsNullOrEmpty(card.Description) ? card.Description : "";
            }

            if (artImage != null)
            {
                artImage.sprite = card.Art;
                artImage.enabled = (card.Art != null);
            }

            if (categoryBackgroundImage != null)
            {
                if (GameManager.Instance != null && GameManager.Instance.TryGetCategoryColor(card.CardCategory, out Color categoryColor))
                {
                    categoryBackgroundImage.color = categoryColor;
                }
                else
                {
                    categoryBackgroundImage.color = Color.white;
                }
            }
        }
    }
}
