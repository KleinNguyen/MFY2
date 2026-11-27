using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardexCardFlightEffect : MonoBehaviour
{
    private const float FlightDuration = 0.4f;
    private const float ArcHeight = 220f;
    private const int TrailPointCount = 24;
    private const float TrailLength = 0.62f;

    private readonly HashSet<GameObject> activeOverlays = new();

    public bool TryFly(RectTransform card, Action onComplete = null)
    {
        if (card == null)
        {
            Debug.LogError("Cannot animate a missing card to the Cardex.", this);
            return false;
        }

        GameObject header = GameObject.Find("RunHeaderCanvas");
        RectTransform target = header != null
            ? header.transform.Find("DeckButton") as RectTransform
            : null;
        if (target == null)
        {
            Debug.LogError("Cannot find the Cardex button on RunHeaderCanvas.", this);
            return false;
        }

        StartCoroutine(AnimateCard(card, target, onComplete));
        return true;
    }

    private IEnumerator AnimateCard(RectTransform card, RectTransform target, Action onComplete)
    {
        Vector2 startScreenPosition = RectTransformUtility.WorldToScreenPoint(null, card.position);
        Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(null, target.position);
        Vector2 cardSize = card.rect.size;
        Vector3 cardScale = card.localScale;
        Quaternion cardRotation = card.localRotation;

        Canvas overlayCanvas = CreateFlightOverlay(card, target);
        GameObject overlayObject = overlayCanvas.gameObject;
        activeOverlays.Add(overlayObject);
        Canvas.ForceUpdateCanvases();
        RectTransform overlayRect = overlayCanvas.transform as RectTransform;

        // Convert both endpoints into overlay space before animating the card.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                startScreenPosition,
                null,
                out Vector2 startPosition
            ) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                targetScreenPosition,
                null,
                out Vector2 endPosition
            ))
        {
            Debug.LogError("Cannot calculate the Cardex flight path.", this);
            CompleteFlight(overlayObject, onComplete);
            yield break;
        }

        GameObject trailObject = new(
            "CardexFlightTrail",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(CardexFlightTrailGraphic)
        );
        trailObject.layer = overlayObject.layer;
        trailObject.transform.SetParent(overlayRect, false);

        RectTransform trailRect = trailObject.GetComponent<RectTransform>();
        trailRect.anchorMin = Vector2.zero;
        trailRect.anchorMax = Vector2.one;
        trailRect.offsetMin = Vector2.zero;
        trailRect.offsetMax = Vector2.zero;

        CardexFlightTrailGraphic trail = trailObject.GetComponent<CardexFlightTrailGraphic>();
        trail.raycastTarget = false;
        Vector2[] path = new Vector2[TrailPointCount];

        card.SetParent(overlayRect, false);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = cardSize;
        card.anchoredPosition = startPosition;
        card.localScale = cardScale;
        card.localRotation = cardRotation;
        card.SetAsLastSibling();

        Tween flightTween = DOTween.To(
            () => 0f,
            progress =>
            {
                card.anchoredPosition = EvaluateFlight(startPosition, endPosition, progress);

                float scaleProgress = progress <= 0.5f
                    ? Mathf.SmoothStep(0f, 1f, progress * 2f)
                    : Mathf.SmoothStep(0f, 1f, (progress - 0.5f) * 2f);
                float scaleMultiplier = progress <= 0.5f
                    ? Mathf.Lerp(1f, 1.4f, scaleProgress)
                    : Mathf.Lerp(1.4f, 0.015f, scaleProgress);
                card.localScale = cardScale * scaleMultiplier;
                card.localRotation = cardRotation * Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Sin(progress * Mathf.PI) * 14f
                );

                UpdateTrail(trail, path, startPosition, endPosition, progress);
            },
            1f,
            FlightDuration
        ).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(overlayObject);

        yield return flightTween.WaitForCompletion();

        if (card != null) Destroy(card.gameObject);

        Tween fadeTween = DOTween.To(
            () => trail.Opacity,
            value => trail.Opacity = value,
            0f,
            0.22f
        ).SetUpdate(true).SetLink(overlayObject);
        yield return fadeTween.WaitForCompletion();

        CompleteFlight(overlayObject, onComplete);
    }

    private Canvas CreateFlightOverlay(RectTransform card, RectTransform target)
    {
        GameObject overlayObject = new(
            "CardexFlightOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        overlayObject.layer = card.gameObject.layer;

        Canvas overlayCanvas = overlayObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;

        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        overlayCanvas.sortingLayerID = targetCanvas != null ? targetCanvas.sortingLayerID : 0;
        overlayCanvas.sortingOrder = targetCanvas != null
            ? Mathf.Max(300, targetCanvas.sortingOrder + 1)
            : 300;

        CanvasScaler overlayScaler = overlayObject.GetComponent<CanvasScaler>();
        CanvasScaler sourceScaler = card.GetComponentInParent<CanvasScaler>();
        if (sourceScaler != null)
        {
            overlayScaler.uiScaleMode = sourceScaler.uiScaleMode;
            overlayScaler.referenceResolution = sourceScaler.referenceResolution;
            overlayScaler.screenMatchMode = sourceScaler.screenMatchMode;
            overlayScaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            overlayScaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
            overlayScaler.scaleFactor = sourceScaler.scaleFactor;
        }

        return overlayCanvas;
    }

    private static Vector2 EvaluateFlight(Vector2 start, Vector2 end, float progress)
    {
        Vector2 position = Vector2.Lerp(start, end, progress);
        return position + Vector2.up * (ArcHeight * 4f * progress * (1f - progress));
    }

    private static void UpdateTrail(
        CardexFlightTrailGraphic trail,
        Vector2[] path,
        Vector2 start,
        Vector2 end,
        float progress
    )
    {
        float trailStart = Mathf.Max(0f, progress - TrailLength);

        for (int i = 0; i < path.Length; i++)
        {
            float sample = i / (float)(path.Length - 1);
            path[i] = EvaluateFlight(start, end, Mathf.Lerp(trailStart, progress, sample));
        }

        trail.SetPath(path);
    }

    private void CompleteFlight(GameObject overlayObject, Action onComplete)
    {
        activeOverlays.Remove(overlayObject);
        if (overlayObject != null) Destroy(overlayObject);
        if (onComplete != null) onComplete();
    }

    private void OnDisable()
    {
        foreach (GameObject overlay in activeOverlays)
        {
            if (overlay != null) Destroy(overlay);
        }

        activeOverlays.Clear();
    }
}
