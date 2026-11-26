using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ActTransitionBanner : MonoBehaviour
{
    private const float HoldDuration = 2.5f;
    private const float SlideDuration = 0.5f;

    private static ActTransitionBanner instance;

    private RectTransform canvasRect;
    private RectTransform bannerRect;
    private CanvasGroup bannerCanvasGroup;
    private TMP_Text actNumberText;
    private TMP_Text titleText;
    private Sequence activeSequence;
    private int pendingActNumber;
    private string pendingTitle;

    public static void RequestCurrentActIntro()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.CurrentAct == null)
        {
            Debug.LogError("Cannot show the act banner because the current act is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(run.CurrentAct.ChapterTitle))
        {
            Debug.LogError("Chapter Title is not assigned for the current Act.", run.CurrentAct);
            return;
        }

        if (instance == null)
        {
            GameObject bannerObject = new GameObject("Act Transition Banner");
            instance = bannerObject.AddComponent<ActTransitionBanner>();
        }

        instance.pendingActNumber = run.CurrentActIndex + 1;
        instance.pendingTitle = run.CurrentAct.ChapterTitle;

        if (SceneManager.GetActiveScene().name == "MapScene")
        {
            instance.ShowPendingIntro();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateBanner();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        activeSequence?.Kill();

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MapScene")
        {
            ShowPendingIntro();
        }
    }

    private void CreateBanner()
    {
        GameObject canvasObject = new GameObject(
            "ActTransitionCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();
        GameObject bannerObject = new GameObject(
            "ActBanner",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        bannerRect = bannerObject.GetComponent<RectTransform>();
        bannerRect.SetParent(canvasRect, false);
        bannerRect.anchorMin = new Vector2(0.06f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.94f, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.sizeDelta = new Vector2(0f, 220f);

        Image bannerImage = bannerObject.GetComponent<Image>();
        bannerImage.color = new Color(0.08f, 0.07f, 0.06f, 0.86f);
        bannerImage.raycastTarget = false;

        bannerCanvasGroup = bannerObject.GetComponent<CanvasGroup>();
        bannerCanvasGroup.interactable = false;
        bannerCanvasGroup.blocksRaycasts = false;
        bannerCanvasGroup.alpha = 0f;
        bannerObject.SetActive(false);

        actNumberText = CreateText(bannerObject.transform, "ActNumber", 28f);
        actNumberText.text = "CHAPTER";
        actNumberText.color = new Color(0.86f, 0.81f, 0.69f, 1f);
        actNumberText.fontStyle = FontStyles.Bold;
        actNumberText.rectTransform.anchorMin = new Vector2(0f, 0.65f);
        actNumberText.rectTransform.anchorMax = new Vector2(1f, 1f);
        actNumberText.rectTransform.offsetMin = new Vector2(30f, 0f);
        actNumberText.rectTransform.offsetMax = new Vector2(-30f, -24f);

        titleText = CreateText(bannerObject.transform, "ChapterTitle", 58f);
        titleText.color = new Color(1f, 0.83f, 0.42f, 1f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 0.6f);
        titleText.rectTransform.offsetMin = new Vector2(30f, 16f);
        titleText.rectTransform.offsetMax = new Vector2(-30f, 0f);
    }

    private static TMP_Text CreateText(Transform parent, string objectName, float fontSize)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        if (text.font == null)
        {
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (text.font == null)
        {
            Debug.LogError("No TextMesh Pro font asset is available for the act banner.", text);
        }

        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private void ShowPendingIntro()
    {
        if (string.IsNullOrEmpty(pendingTitle))
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        activeSequence?.Kill();
        actNumberText.text = "CHAPTER " + pendingActNumber;
        titleText.text = pendingTitle;
        bannerRect.anchoredPosition = new Vector2(-GetOffscreenDistance(), 0f);
        bannerCanvasGroup.alpha = 0f;
        bannerRect.gameObject.SetActive(true);

        activeSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .Join(bannerRect.DOAnchorPos(Vector2.zero, SlideDuration).SetEase(Ease.OutCubic))
            .Join(bannerCanvasGroup.DOFade(1f, 0.2f))
            .AppendInterval(HoldDuration)
            .Append(bannerRect.DOAnchorPos(
                new Vector2(GetOffscreenDistance(), 0f),
                SlideDuration).SetEase(Ease.InCubic))
            .Join(bannerCanvasGroup.DOFade(0f, SlideDuration))
            .OnComplete(() => bannerRect.gameObject.SetActive(false));

        pendingTitle = null;
    }

    private float GetOffscreenDistance()
    {
        return canvasRect.rect.width + bannerRect.rect.width / 2f + 20f;
    }
}
