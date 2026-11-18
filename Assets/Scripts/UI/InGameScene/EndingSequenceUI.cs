using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingSequenceUI : MonoBehaviour
{
    [SerializeField] private Sprite sceneImage;
    [SerializeField, Min(0f)] private float sceneDuration = 10f;
    [SerializeField, TextArea(2, 5)] private string subtitle =
        "Một hành trình khép lại, nhưng câu chuyện vẫn còn ở phía trước...";
    [SerializeField, TextArea(8, 24)] private string credits =
        "MFY2\n\nCẢM ƠN BẠN ĐÃ CHƠI\n\nPhát triển\nTên của bạn\n\nThiết kế trò chơi\nTên của bạn\n\nLập trình\nTên của bạn\n\nĐồ họa và âm thanh\nTên của bạn\n\nĐặc biệt cảm ơn\nTất cả những người đã chơi\n\nHẾT";
    [SerializeField, Min(1f)] private float creditsScrollSpeed = 55f;

    private void Start()
    {
        EnsureEventSystem();
        Canvas canvas = CreateCanvas();
        RectTransform canvasRect = canvas.transform as RectTransform;

        GameObject storyGroup = CreateFullScreenGroup("StoryScreen", canvasRect);
        CreateStoryBackground(storyGroup.transform);
        CreateSubtitle(storyGroup.transform);

        GameObject creditsGroup = CreateFullScreenGroup("CreditsScreen", canvasRect);
        creditsGroup.AddComponent<RectMask2D>();
        TMP_Text creditsText = CreateCreditsText(creditsGroup.transform, canvasRect);
        CreateMenuButton(creditsGroup.transform);
        creditsGroup.SetActive(false);

        StartCoroutine(PlaySequence(storyGroup, creditsGroup, creditsText, canvasRect));
    }

    private IEnumerator PlaySequence(
        GameObject storyGroup,
        GameObject creditsGroup,
        TMP_Text creditsText,
        RectTransform canvasRect
    )
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, sceneDuration));

        storyGroup.SetActive(false);
        creditsGroup.SetActive(true);
        RectTransform creditsRect = creditsText.rectTransform;
        creditsRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            canvasRect.rect.width * 0.82f);
        Canvas.ForceUpdateCanvases();
        creditsText.ForceMeshUpdate();

        float textHeight = Mathf.Max(creditsText.preferredHeight, creditsRect.rect.height);
        creditsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
        creditsRect.anchoredPosition = new Vector2(0f, -canvasRect.rect.height - 30f);

        float endingPosition = textHeight + 30f;
        float scrollSpeed = Mathf.Max(1f, creditsScrollSpeed);
        while (creditsRect.anchoredPosition.y < endingPosition)
        {
            Vector2 position = creditsRect.anchoredPosition;
            position.y += scrollSpeed * Time.unscaledDeltaTime;
            creditsRect.anchoredPosition = position;
            yield return null;
        }

        creditsRect.anchoredPosition = new Vector2(0f, endingPosition);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "EndingCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static GameObject CreateFullScreenGroup(string name, RectTransform parent)
    {
        GameObject group = new GameObject(name, typeof(RectTransform));
        RectTransform rect = group.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return group;
    }

    private void CreateStoryBackground(Transform parent)
    {
        GameObject background = new GameObject(
            "BlackBackgroundAndImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = background.GetComponent<Image>();
        image.sprite = sceneImage;
        image.color = sceneImage == null ? Color.black : Color.white;
        image.preserveAspect = sceneImage != null;
        image.raycastTarget = false;
    }

    private void CreateSubtitle(Transform parent)
    {
        GameObject panel = new GameObject(
            "SubtitlePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 150f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);
        panelImage.raycastTarget = false;

        TMP_Text subtitleText = CreateText(panel.transform, "Subtitle", subtitle, 32f);
        RectTransform textRect = subtitleText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(48f, 18f);
        textRect.offsetMax = new Vector2(-48f, -18f);
        subtitleText.alignment = TextAlignmentOptions.Center;
    }

    private TMP_Text CreateCreditsText(Transform parent, RectTransform canvasRect)
    {
        TMP_Text text = CreateText(parent, "CreditsText", credits, 34f);
        text.alignment = TextAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Top;
        text.enableAutoSizing = true;
        text.fontSizeMin = 22f;
        text.fontSizeMax = 34f;
        text.margin = new Vector4(20f, 40f, 20f, 40f);

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(canvasRect.rect.width * 0.82f, 0f);
        rect.anchoredPosition = Vector2.zero;
        return text;
    }

    private void CreateMenuButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "BackToMenuButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-40f, 32f);
        buttonRect.sizeDelta = new Vector2(240f, 68f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.34f, 0.16f, 0.08f, 0.96f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => SceneManager.LoadScene("MenuScene"));

        TMP_Text label = CreateText(buttonObject.transform, "ButtonLabel", "Back to Menu", 26f);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        if (text.font == null)
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (text.font == null)
            Debug.LogError("No TextMesh Pro font asset is available for the ending screen.", text);

        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }
}
