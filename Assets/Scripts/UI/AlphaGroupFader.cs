using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using T60.Pooling;

namespace T60.UI
{
    [DisallowMultipleComponent]
    public class AlphaGroupFader : MonoBehaviour, IPoolable
    {
        [Header("Target Components")]
        [SerializeField] private List<SpriteRenderer> targetSpriteRenderers = new List<SpriteRenderer>();
        [SerializeField] private List<TMP_Text> targetTexts = new List<TMP_Text>();
        [SerializeField] private List<Graphic> targetGraphics = new List<Graphic>();
        [SerializeField] private bool autoCollectOnAwake = true;

        [Header("Fade Settings")]
        [SerializeField] private float defaultFadeDuration = 0.4f;

        private Coroutine activeFadeRoutine;
        private float currentAlpha = 1f;

        public float CurrentAlpha => currentAlpha;
        public List<SpriteRenderer> TargetSpriteRenderers => targetSpriteRenderers;
        public List<TMP_Text> TargetTexts => targetTexts;
        public List<Graphic> TargetGraphics => targetGraphics;

        private void Awake()
        {
            if (autoCollectOnAwake && (targetSpriteRenderers.Count == 0 && targetTexts.Count == 0 && targetGraphics.Count == 0))
            {
                CollectComponents();
            }
        }

        public void CollectComponents()
        {
            if (targetSpriteRenderers == null)
            {
                targetSpriteRenderers = new List<SpriteRenderer>();
            }
            if (targetSpriteRenderers.Count == 0)
            {
                targetSpriteRenderers.AddRange(GetComponentsInChildren<SpriteRenderer>(true));
            }

            if (targetTexts == null)
            {
                targetTexts = new List<TMP_Text>();
            }
            if (targetTexts.Count == 0)
            {
                targetTexts.AddRange(GetComponentsInChildren<TMP_Text>(true));
            }

            if (targetGraphics == null)
            {
                targetGraphics = new List<Graphic>();
            }
            if (targetGraphics.Count == 0)
            {
                targetGraphics.AddRange(GetComponentsInChildren<Graphic>(true));
            }
        }

        public void OnSpawn()
        {
            StopActiveFade();
            SetAlpha(1f);
        }

        public void OnDespawn()
        {
            StopActiveFade();
        }

        public void SetAlpha(float alpha)
        {
            currentAlpha = Mathf.Clamp01(alpha);

            if (targetSpriteRenderers != null)
            {
                for (int i = 0; i < targetSpriteRenderers.Count; i++)
                {
                    if (targetSpriteRenderers[i] != null)
                    {
                        Color c = targetSpriteRenderers[i].color;
                        c.a = currentAlpha;
                        targetSpriteRenderers[i].color = c;
                    }
                }
            }

            if (targetTexts != null)
            {
                for (int i = 0; i < targetTexts.Count; i++)
                {
                    if (targetTexts[i] != null)
                    {
                        targetTexts[i].alpha = currentAlpha;
                    }
                }
            }

            if (targetGraphics != null)
            {
                for (int i = 0; i < targetGraphics.Count; i++)
                {
                    if (targetGraphics[i] != null)
                    {
                        Color c = targetGraphics[i].color;
                        c.a = currentAlpha;
                        targetGraphics[i].color = c;
                    }
                }
            }
        }

        public void FadeTo(float targetAlpha, float duration = -1f, Action onComplete = null)
        {
            float dur = duration > 0f ? duration : defaultFadeDuration;
            StopActiveFade();
            activeFadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, dur, onComplete));
        }

        public void FadeOutAndDespawn(float duration = -1f, Action onDespawned = null)
        {
            float dur = duration > 0f ? duration : defaultFadeDuration;
            FadeTo(0f, dur, () =>
            {
                onDespawned?.Invoke();
                ObjectPoolManager.DespawnObject(gameObject);
            });
        }

        private void StopActiveFade()
        {
            if (activeFadeRoutine != null)
            {
                StopCoroutine(activeFadeRoutine);
                activeFadeRoutine = null;
            }
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
        {
            float startAlpha = currentAlpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetAlpha(alpha);
                yield return null;
            }

            SetAlpha(targetAlpha);
            activeFadeRoutine = null;
            onComplete?.Invoke();
        }
    }
}
