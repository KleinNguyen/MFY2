using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorySlideshowUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text textTemplate;
    [SerializeField] private Sprite[] storyImages;
    [SerializeField] private string[] storyTexts;
    [SerializeField] private float secondsPerImage = 3f;
    [SerializeField] private float slideDuration = 1.5f;

    private IEnumerator Start()
    {
        if (backgroundImage == null || textTemplate == null)
        {
            Debug.LogError("Story slideshow needs a background Image and a text template.");
            yield break;
        }

        if (storyImages == null || storyImages.Length == 0 ||
            storyTexts == null || storyTexts.Length != storyImages.Length)
        {
            Debug.LogError("Story slideshow needs matching story images and text entries.");
            yield break;
        }

        for (int i = 0; i < storyImages.Length; i++)
        {
            if (storyImages[i] == null)
            {
                Debug.LogError("Story image " + (i + 1) + " is not assigned.");
                yield break;
            }
        }

        Canvas storyCanvas = backgroundImage.canvas;
        RectTransform viewport = backgroundImage.transform.parent as RectTransform;
        if (storyCanvas == null || viewport == null)
        {
            Debug.LogError("The story background must be inside a UI Canvas.");
            yield break;
        }

        RectTransform currentRect = backgroundImage.rectTransform;
        float width = viewport.rect.width;
        if (width <= 0f)
        {
            Debug.LogError("The story Canvas has no available width.");
            yield break;
        }

        Image incomingImage = CreateIncomingImage(currentRect);
        TMP_Text storyText = CreateStoryText(storyCanvas.transform);
        if (storyText == null)
        {
            Destroy(incomingImage.gameObject);
            yield break;
        }

        RectTransform incomingRect = incomingImage.rectTransform;

        backgroundImage.sprite = storyImages[0];
        backgroundImage.raycastTarget = false;
        storyText.text = storyTexts[0];
        incomingImage.sprite = storyImages[0];
        incomingRect.anchoredPosition = currentRect.anchoredPosition + Vector2.right * width;

        float waitDuration = Mathf.Max(0f, secondsPerImage);
        float transitionDuration = Mathf.Max(0.01f, slideDuration);

        for (int i = 1; i < storyImages.Length; i++)
        {
            yield return new WaitForSecondsRealtime(waitDuration);

            Vector2 startPosition = currentRect.anchoredPosition;
            incomingRect.anchoredPosition = startPosition + Vector2.right * width;
            incomingImage.sprite = storyImages[i];
            storyText.text = storyTexts[i];

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                currentRect.anchoredPosition = startPosition - Vector2.right * (width * progress);
                incomingRect.anchoredPosition = startPosition + Vector2.right * (width * (1f - progress));
                yield return null;
            }

            currentRect.anchoredPosition = startPosition - Vector2.right * width;
            incomingRect.anchoredPosition = startPosition;

            Image previousImage = backgroundImage;
            backgroundImage = incomingImage;
            incomingImage = previousImage;

            RectTransform previousRect = currentRect;
            currentRect = incomingRect;
            incomingRect = previousRect;
        }
    }

    private Image CreateIncomingImage(RectTransform sourceRect)
    {
        GameObject incomingObject = new GameObject(
            "StoryIncomingImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        incomingObject.transform.SetParent(sourceRect.parent, false);

        RectTransform incomingRect = incomingObject.GetComponent<RectTransform>();
        incomingRect.anchorMin = sourceRect.anchorMin;
        incomingRect.anchorMax = sourceRect.anchorMax;
        incomingRect.pivot = sourceRect.pivot;
        incomingRect.sizeDelta = sourceRect.sizeDelta;
        incomingRect.localScale = sourceRect.localScale;
        incomingRect.localRotation = sourceRect.localRotation;
        incomingRect.anchoredPosition = sourceRect.anchoredPosition;
        incomingRect.SetAsFirstSibling();

        Image incomingImage = incomingObject.GetComponent<Image>();
        incomingImage.sprite = backgroundImage.sprite;
        incomingImage.color = backgroundImage.color;
        incomingImage.type = backgroundImage.type;
        incomingImage.preserveAspect = backgroundImage.preserveAspect;
        incomingImage.raycastTarget = false;
        return incomingImage;
    }

    private TMP_Text CreateStoryText(Transform canvasTransform)
    {
        GameObject panelObject = new GameObject(
            "StoryCaptionPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(canvasTransform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 170f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);
        panelImage.raycastTarget = false;

        GameObject textObject = Instantiate(textTemplate.gameObject, panelRect);
        textObject.name = "StoryText";
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(40f, 15f);
        textRect.offsetMax = new Vector2(-40f, -15f);

        TMP_Text storyText = textObject.GetComponent<TMP_Text>();
        storyText.text = string.Empty;
        storyText.fontSize = 30f;
        storyText.enableAutoSizing = true;
        storyText.fontSizeMin = 18f;
        storyText.fontSizeMax = 30f;
        storyText.fontStyle = FontStyles.Normal;
        storyText.color = Color.white;
        storyText.alignment = TextAlignmentOptions.Center;
        storyText.raycastTarget = false;
        return storyText;
    }
}
