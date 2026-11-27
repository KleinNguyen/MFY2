using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class MapUI
{
    // Legend and timer
    private void StyleMapLegend()
    {
        if (mapLegend == null) return;

        foreach (Button entry in mapLegend.GetComponentsInChildren<Button>(true))
        {
            entry.enabled = false;
            bool isTitle = entry.name == "MapLegend - Title";
            Image icon = entry.GetComponent<Image>();
            if (icon != null)
            {
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.color = isTitle ? new Color(1f, 1f, 1f, 0f) : Color.white;
                if (!isTitle) ConfigureIconOutline(icon, new Color(0.96f, 0.72f, 0.3f, 0.8f));
            }

            TMP_Text label = entry.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                Debug.LogError("Map legend entry is missing its label: " + entry.name, this);
                continue;
            }

            StyleText(
                label,
                isTitle ? 30f : 24f,
                new Color(0.96f, 0.93f, 0.85f),
                isTitle ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft
            );
            ConfigureTextShadow(label);
        }
    }

    private void SetupRunTimer()
    {
        if (runTimerDisplay == null) return;

        Transform textTransform = runTimerDisplay.Find("RunTimerText");
        runTimerText = textTransform != null ? textTransform.GetComponentInChildren<TMP_Text>(true) : null;
        if (runTimerText == null)
        {
            Debug.LogError("Run Timer Display needs a RunTimerText object with a TMP text component.", this);
            return;
        }

        StyleText(runTimerText, 34f, Color.white, TextAlignmentOptions.Center);
        ConfigureTextShadow(runTimerText);

        Transform iconTransform = runTimerDisplay.Find("RunTimerIcon");
        Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (icon == null)
        {
            Debug.LogError("Run Timer Display needs a RunTimerIcon object with an Image component.", this);
            return;
        }

        icon.preserveAspect = true;
        icon.raycastTarget = false;
        ConfigureIconOutline(icon, new Color(0.96f, 0.72f, 0.3f, 0.85f));
        UpdateRunTimer();
    }

    private void UpdateRunTimer()
    {
        RunSystem run = RunSystem.Instance;
        if (runTimerText == null || run == null) return;

        int elapsedSeconds = Mathf.FloorToInt(run.ElapsedRunTime);
        int hours = elapsedSeconds / 3600;
        int minutes = (elapsedSeconds / 60) % 60;
        int seconds = elapsedSeconds % 60;
        runTimerText.text = hours > 0 ? $"{hours:00}:{minutes:00}:{seconds:00}" : $"{minutes:00}:{seconds:00}";
    }

    // Styling helpers
    private void StyleText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
    {
        text.font = mapMessageText.font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
    }

    private static void ConfigureIconOutline(Image image, Color color)
    {
        Outline outline = image.GetComponent<Outline>();
        if (outline == null) outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1.5f, 1.5f);
    }

    private static void ConfigureTextShadow(TMP_Text text)
    {
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private static void HideScrollbar(Scrollbar scrollbar)
    {
        if (scrollbar == null) return;
        CanvasGroup canvasGroup = scrollbar.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = scrollbar.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void FocusCurrentMapPoint()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || mapScrollRect == null || mapContent == null || mapScrollRect.viewport == null) return;

        int pointId = run.CompletedMapPointId;
        if (pointId == lastFocusedPointId) return;

        lastFocusedPointId = pointId;
        Canvas.ForceUpdateCanvases();
        mapScrollRect.StopMovement();

        if (pointId < 0)
        {
            mapScrollRect.verticalNormalizedPosition = 0f;
            return;
        }

        if (!pointButtons.TryGetValue(pointId, out Button pointButton) || pointButton == null) return;

        RectTransform viewport = mapScrollRect.viewport;
        Bounds contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, mapContent);
        float scrollableHeight = contentBounds.size.y - viewport.rect.height;
        if (scrollableHeight <= 0f) return;

        float pointY = viewport.InverseTransformPoint(pointButton.transform.position).y;
        float desiredY = viewport.rect.yMin + viewport.rect.height * 0.3f;
        float offset = (pointY - desiredY) / scrollableHeight;

        mapScrollRect.verticalNormalizedPosition = Mathf.Clamp01(mapScrollRect.verticalNormalizedPosition + offset);
    }
}
