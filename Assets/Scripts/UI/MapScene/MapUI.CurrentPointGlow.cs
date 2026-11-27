using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class MapUI
{
    // Current point glow
    private void UpdateCurrentPointGlow(RunSystem run)
    {
        int pointId = run.CompletedMapPointId;
        if (pointId == glowingMapPointId && currentPointGlow != null) return;
        ClearCurrentPointGlow();
        glowingMapPointId = pointId;
        if (pointId < 0 || !pointButtons.TryGetValue(pointId, out Button pointButton) || pointButton == null) return;

        Image pointImage = pointButton.GetComponent<Image>();
        TMP_Text pointText = pointButton.GetComponentInChildren<TMP_Text>(true);
        bool useImage = pointImage != null && pointImage.sprite != null;
        if (!useImage && (pointText == null || !pointText.gameObject.activeSelf)) return;

        currentPointGlow = new GameObject("CurrentMapPointGlow", typeof(RectTransform), typeof(CanvasRenderer));
        currentPointGlow.layer = mapContent.gameObject.layer;
        currentPointGlowRect = currentPointGlow.GetComponent<RectTransform>();
        currentPointGlowRect.SetParent(mapContent, false);
        currentPointGlowRect.anchorMin = Vector2.zero;
        currentPointGlowRect.anchorMax = Vector2.zero;

        RectTransform pointRect = pointButton.GetComponent<RectTransform>();
        currentPointGlowRect.pivot = pointRect.pivot;
        currentPointGlowRect.anchoredPosition = pointRect.anchoredPosition;
        currentPointGlowRect.sizeDelta = pointRect.sizeDelta;
        currentPointGlowRect.localScale = Vector3.one * 1.08f;
        currentPointGlowRect.SetSiblingIndex(pointButton.transform.GetSiblingIndex());

        if (useImage)
        {
            currentPointGlowImage = currentPointGlow.AddComponent<Image>();
            currentPointGlowImage.sprite = pointImage.sprite;
            currentPointGlowImage.type = Image.Type.Simple;
            currentPointGlowImage.preserveAspect = pointImage.preserveAspect;
            currentPointGlowImage.raycastTarget = false;
            currentPointGlowOutline = currentPointGlow.AddComponent<Outline>();
            currentPointGlowOutline.effectDistance = new Vector2(2f, 2f);
            currentPointGlowOutline.useGraphicAlpha = true;
        }
        else
        {
            currentPointGlowText = currentPointGlow.AddComponent<TextMeshProUGUI>();
            currentPointGlowText.font = pointText.font;
            currentPointGlowText.text = pointText.text;
            currentPointGlowText.fontSize = pointText.fontSize;
            currentPointGlowText.fontStyle = pointText.fontStyle;
            currentPointGlowText.enableAutoSizing = pointText.enableAutoSizing;
            currentPointGlowText.fontSizeMin = pointText.fontSizeMin;
            currentPointGlowText.fontSizeMax = pointText.fontSizeMax;
            currentPointGlowText.alignment = TextAlignmentOptions.Center;
            currentPointGlowText.raycastTarget = false;
        }

        AnimateCurrentPointGlow();
    }

    private void AnimateCurrentPointGlow()
    {
        if (currentPointGlowRect == null) return;

        float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
        currentPointGlowRect.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.2f, pulse);
        if (currentPointGlowImage != null)
        {
            currentPointGlowImage.color = new Color(
                1f,
                0.78f,
                0.12f,
                Mathf.Lerp(0.16f, 0.34f, pulse)
            );
        }

        if (currentPointGlowOutline != null)
        {
            currentPointGlowOutline.effectColor = new Color(
                1f,
                0.72f,
                0.12f,
                Mathf.Lerp(0.3f, 0.75f, pulse)
            );
        }

        if (currentPointGlowText != null)
        {
            currentPointGlowText.color = new Color(
                1f,
                0.78f,
                0.12f,
                Mathf.Lerp(0.3f, 0.7f, pulse)
            );
        }
    }

    private void ClearCurrentPointGlow()
    {
        if (currentPointGlow != null)
        {
            currentPointGlow.SetActive(false);
            Destroy(currentPointGlow);
        }

        currentPointGlow = null;
        currentPointGlowRect = null;
        currentPointGlowImage = null;
        currentPointGlowOutline = null;
        currentPointGlowText = null;
        glowingMapPointId = -1;
    }
}
