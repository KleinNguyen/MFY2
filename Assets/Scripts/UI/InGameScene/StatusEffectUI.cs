using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusEffectUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text stackCountText;

    private StatusEffectType statusType;
    private int currentStackCount;
    private bool isHovered;
    private Color normalImageColor;
    private Color normalCountColor;
    private bool normalAutoSizing;
    private float normalFontSize;
    private float normalFontSizeMin;
    private float normalFontSizeMax;
    private TextAlignmentOptions normalAlignment;

    private void Awake()
    {
        normalImageColor = image.color;
        normalCountColor = stackCountText.color;
        normalAutoSizing = stackCountText.enableAutoSizing;
        normalFontSize = stackCountText.fontSize;
        normalFontSizeMin = stackCountText.fontSizeMin;
        normalFontSizeMax = stackCountText.fontSizeMax;
        normalAlignment = stackCountText.alignment;
        image.raycastTarget = true;
        stackCountText.raycastTarget = false;

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.WorldSpace &&
                canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    public void Set(
        StatusEffectType type,
        Sprite sprite,
        int stackCount
    )
    {
        statusType = type;
        currentStackCount = stackCount;

        image.sprite = sprite;
        bool comboFallback = type == StatusEffectType.COMBO && sprite == null;
        image.color = comboFallback ? new Color(1f, 0.83f, 0.22f) : normalImageColor;
        stackCountText.color = comboFallback ? new Color(0.16f, 0.10f, 0.02f) : normalCountColor;
        stackCountText.enableAutoSizing = comboFallback || normalAutoSizing;
        stackCountText.fontSizeMin = comboFallback ? 8f : normalFontSizeMin;
        stackCountText.fontSizeMax = comboFallback ? 20f : normalFontSizeMax;
        stackCountText.alignment = comboFallback ? TextAlignmentOptions.Center : normalAlignment;
        if (!comboFallback) stackCountText.fontSize = normalFontSize;
        stackCountText.text = comboFallback ? "C\n" + stackCount : stackCount.ToString();

        if (isHovered)
        {
            ShowTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (StatusEffectTooltipUI.Instance == null) return;

        StatusEffectTooltipUI.Instance.Show(
            this,
            statusType,
            currentStackCount
        );
    }

    private void HideTooltip()
    {
        isHovered = false;

        if (StatusEffectTooltipUI.Instance != null)
        {
            StatusEffectTooltipUI.Instance.Hide(this);
        }
    }

    private void OnDisable()
    {
        HideTooltip();
    }
}
