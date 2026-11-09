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

    private void Awake()
    {
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
        stackCountText.text = stackCount.ToString();

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