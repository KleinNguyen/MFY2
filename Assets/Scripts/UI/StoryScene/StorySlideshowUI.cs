using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StorySlideshowUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text textTemplate;
    [SerializeField] private Sprite[] storyImages;
    [SerializeField] private string[] storyTexts;
    [SerializeField] private float secondsPerImage = 3f;
    [FormerlySerializedAs("slideDuration")]
    [SerializeField] private float crossFadeDuration = 0.5f;
    [SerializeField] private float typewriterDelay = 0.03f;

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

        Image incomingImage = CreateIncomingImage(backgroundImage.rectTransform);
        TMP_Text storyText = CreateStoryText(storyCanvas.transform);
        if (storyText == null)
        {
            Destroy(incomingImage.gameObject);
            yield break;
        }

        RectTransform incomingRect = incomingImage.rectTransform;

        backgroundImage.sprite = storyImages[0];
        backgroundImage.raycastTarget = false;
        SetImageAlpha(backgroundImage, 1f);
        incomingImage.sprite = storyImages[0];
        SetImageAlpha(incomingImage, 0f);
        storyText.text = storyTexts[0];

        for (int i = 0; i < storyImages.Length; i++)
        {
            if (i > 0)
            {
                incomingRect.SetAsFirstSibling();
                incomingImage.sprite = storyImages[i];
                SetImageAlpha(incomingImage, 1f);

                yield return CrossFadeImage(backgroundImage);

                Image previousImage = backgroundImage;
                backgroundImage = incomingImage;
                incomingImage = previousImage;
                incomingRect = incomingImage.rectTransform;

                SetImageAlpha(backgroundImage, 1f);
                SetImageAlpha(incomingImage, 0f);
                storyText.text = storyTexts[i];
                storyText.maxVisibleCharacters = 0;
            }

            yield return RevealStoryText(storyText);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, secondsPerImage));
        }
    }

    private IEnumerator CrossFadeImage(Image currentImage)
    {
        float duration = Mathf.Max(0.01f, crossFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetImageAlpha(currentImage, 1f - progress);
            yield return null;
        }

        SetImageAlpha(currentImage, 0f);
    }

    private IEnumerator RevealStoryText(TMP_Text storyText)
    {
        storyText.maxVisibleCharacters = 0;
        storyText.ForceMeshUpdate();
        int characterCount = storyText.textInfo.characterCount;
        float delay = Mathf.Max(0f, typewriterDelay);

        for (int visibleCharacters = 1; visibleCharacters <= characterCount; visibleCharacters++)
        {
            storyText.maxVisibleCharacters = visibleCharacters;
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
        }
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
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
        textObject.SetActive(true);
        return storyText;
    }
}
