using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LuckyWheelUI : MonoBehaviour
{
    private const int SliceCount = 6;
    private const float ArrowSize = 150f;
    [SerializeField] private RectTransform wheelRoot;
    [SerializeField] private RectTransform pointerPivot;
    [SerializeField] private Image wheelImage;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image arrowImage;
    [SerializeField] private TMP_Text wheelText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button continueButton;
    [Header("Character-specific wheel art")]
    [SerializeField] private Sprite redWheelSprite;
    [SerializeField] private Sprite greenWheelSprite;
    [SerializeField] private Sprite yellowWheelSprite;
    [SerializeField] private Sprite redArrowSprite;
    [SerializeField] private Sprite greenArrowSprite;
    [SerializeField] private Sprite yellowArrowSprite;
    [SerializeField] private Sprite redCharacterSprite;
    [SerializeField] private Sprite greenCharacterSprite;
    [SerializeField] private Sprite yellowCharacterSprite;
    [SerializeField, Min(0.1f)] private float redCharacterScale = 1.12f;
    [SerializeField, Min(0.1f)] private float greenCharacterScale = 0.92f;
    [SerializeField, Min(0.1f)] private float yellowCharacterScale = 0.96f;
    [SerializeField, Min(0f)] private float labelRadius = 175f;
    [SerializeField, Min(0.1f)] private float spinDuration = 4f;

    private readonly TMP_Text[] sliceLabels = new TMP_Text[SliceCount];
    private string[] labels;
    private Action<int> resultCallback;
    private Action closedCallback;
    private Tween spinTween;
    private int selectedIndex = -1;
    private bool spinning;
    private bool hasResult;
    private bool allowCancel;

    public bool IsSpinning => spinning;
    public bool HasResult => hasResult;
    public bool IsConfigured =>
        wheelRoot != null &&
        pointerPivot != null &&
        wheelImage != null &&
        characterImage != null &&
        arrowImage != null &&
        wheelText != null &&
        resultText != null &&
        spinButton != null &&
        continueButton != null;

    private void Awake()
    {
        if (spinButton != null) spinButton.onClick.AddListener(Spin);
        if (continueButton != null) continueButton.onClick.AddListener(Close);
    }
    private void LateUpdate()
    {
        if (wheelRoot == null) return;

        // Keep each label upright while its parent wheel rotates.
        Quaternion counterRotation = Quaternion.Inverse(wheelRoot.localRotation);
        foreach (TMP_Text label in sliceLabels)
        {
            if (label != null) label.rectTransform.localRotation = counterRotation;
        }
    }

    public bool Show(
        string[] options,
        Action<int> onResult,
        Action onClosed = null,
        bool canCancel = false,
        int previousResult = -1,
        string closeCaption = "Continue",
        CharacterType character = CharacterType.Red
    )
    {
        if (!IsConfigured || options == null || options.Length != SliceCount)
        {
            Debug.LogError("Assign all LuckyWheelUI references and provide exactly six labels.", this);
            return false;
        }

        if (spinning) return false;
        if (!SetCharacterArt(character)) return false;

        spinTween?.Kill();
        labels = (string[])options.Clone();
        resultCallback = onResult;
        closedCallback = onClosed;
        allowCancel = canCancel;
        selectedIndex = previousResult >= 0 && previousResult < SliceCount ? previousResult : -1;
        hasResult = selectedIndex >= 0;

        for (int i = 0; i < SliceCount; i++)
        {
            if (sliceLabels[i] == null)
            {
                sliceLabels[i] = Instantiate(wheelText, wheelRoot);
                sliceLabels[i].name = "WheelLabel_" + i;
            }

            TMP_Text label = sliceLabels[i];
            RectTransform rect = label.rectTransform;
            float angle = -i * 60f * Mathf.Deg2Rad;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * labelRadius;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = new Vector2(160f, 88f);

            label.text = labels[i];
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 20f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.color = new Color32(24, 34, 45, 255);
            label.outlineColor = new Color32(255, 242, 205, 255);
            label.outlineWidth = 0.12f;
            label.raycastTarget = false;
            label.gameObject.SetActive(true);
        }

        wheelText.gameObject.SetActive(false);
        pointerPivot.localRotation = Quaternion.identity;
        wheelRoot.localRotation = Quaternion.Euler(0f, 0f, hasResult ? selectedIndex * 60f : 0f);
        resultText.text = hasResult ? "Result: " + labels[selectedIndex] : "Press Spin.";
        spinButton.interactable = !hasResult;
        continueButton.interactable = hasResult || allowCancel;

        TMP_Text closeLabel = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (closeLabel != null) closeLabel.text = closeCaption;

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        return true;
    }

    private bool SetCharacterArt(CharacterType character)
    {
        Sprite wheelSprite;
        Sprite arrowSprite;
        Sprite portraitSprite;
        float portraitScale;
        float arrowHorizontalPosition;

        switch (character)
        {
            case CharacterType.Red:
                wheelSprite = redWheelSprite;
                arrowSprite = redArrowSprite;
                portraitSprite = redCharacterSprite;
                portraitScale = redCharacterScale;
                arrowHorizontalPosition = 315f;
                break;
            case CharacterType.Green:
                wheelSprite = greenWheelSprite;
                arrowSprite = greenArrowSprite;
                portraitSprite = greenCharacterSprite;
                portraitScale = greenCharacterScale;
                arrowHorizontalPosition = 315f;
                break;
            case CharacterType.Yellow:
                wheelSprite = yellowWheelSprite;
                arrowSprite = yellowArrowSprite;
                portraitSprite = yellowCharacterSprite;
                portraitScale = yellowCharacterScale;
                arrowHorizontalPosition = 290f;
                break;
            default:
                Debug.LogError("LuckyWheelUI received an unsupported character type: " + character, this);
                return false;
        }

        if (wheelSprite == null || arrowSprite == null || portraitSprite == null)
        {
            Debug.LogError("Assign the wheel, arrow, and chibi sprites for " + character + " in LuckyWheelUI.", this);
            return false;
        }

        wheelImage.sprite = wheelSprite;
        wheelImage.preserveAspect = true;
        arrowImage.sprite = arrowSprite;
        arrowImage.preserveAspect = true;
        arrowImage.rectTransform.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        arrowImage.rectTransform.anchoredPosition = new Vector2(arrowHorizontalPosition, 0f);
        arrowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        characterImage.sprite = portraitSprite;
        characterImage.preserveAspect = true;
        characterImage.rectTransform.localScale = Vector3.one * portraitScale;
        return true;
    }

    public void Spin()
    {
        if (!IsConfigured || spinning || hasResult || labels == null || !gameObject.activeInHierarchy) return;

        selectedIndex = Random.Range(0, SliceCount);
        spinning = true;
        SfxManager.PlaySfx(SfxId.LuckyWheelSpin);
        spinButton.interactable = false;
        continueButton.interactable = false;
        resultText.text = "Spinning...";

        float current = wheelRoot.localEulerAngles.z;
        float target = Mathf.Repeat(selectedIndex * 60f, 360f);
        float travel = 360f * Random.Range(4, 7) + Mathf.Repeat(current - target, 360f);

        spinTween = wheelRoot
            .DOLocalRotate(
                new Vector3(0f, 0f, current - travel),
                spinDuration,
                RotateMode.FastBeyond360
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(CompleteSpin);
    }

    private void CompleteSpin()
    {
        if (!spinning || hasResult) return;

        spinning = false;
        hasResult = true;
        spinTween = null;
        if (resultText != null) resultText.text = "Result: " + labels[selectedIndex];
        if (continueButton != null) continueButton.interactable = true;

        Action<int> callback = resultCallback;
        resultCallback = null;
        callback?.Invoke(selectedIndex);
    }

    public void Close()
    {
        if (spinning || (!hasResult && !allowCancel)) return;

        Action callback = closedCallback;
        closedCallback = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }

    public void Hide() => gameObject.SetActive(false);

    private void OnDisable()
    {
        if (!spinning) return;
        spinTween?.Kill();
        CompleteSpin();
    }

#if UNITY_EDITOR
    [SerializeField] private bool previewOnStart = true;

    private void Start()
    {
        if (previewOnStart && labels == null) ShowPreview();
    }

    [ContextMenu("Test/Show Wheel")]
    private void ShowPreview()
    {
        if (!Application.isPlaying) return;

        string[] options =
        {
            "FULL HEAL",
            "EXTRA TURN",
            "+2 MANA",
            "5 JUNK",
            "LOSE 30% HP",
            "GET ALL DEBUFFS\n3 TURNS"
        };
        Show(options, index => Debug.Log($"Wheel result {index}: {options[index]}", this));
    }
#endif
}