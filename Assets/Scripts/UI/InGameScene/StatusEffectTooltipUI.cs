using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class StatusEffectTooltipUI : Singleton<StatusEffectTooltipUI>
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField]
    private Vector2 mouseOffset = new Vector2(24f, -20f);

    private RectTransform canvasRect;
    private MonoBehaviour owner;

    protected override void Awake()
    {
        base.Awake();

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        canvasRect = GetComponent<RectTransform>();

        if (panel != null)
        {
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 1f);

            CanvasGroup group = panel.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = panel.gameObject.AddComponent<CanvasGroup>();
            }

            group.interactable = false;
            group.blocksRaycasts = false;

            foreach (
                Graphic graphic in
                panel.GetComponentsInChildren<Graphic>(true)
            )
            {
                graphic.raycastTarget = false;
            }
        }

        Close();
    }

    private void LateUpdate()
    {
        if (panel == null || !panel.gameObject.activeSelf)
        {
            return;
        }

        if (owner == null || !owner.isActiveAndEnabled)
        {
            Close();
            return;
        }

        UpdatePosition();
    }

    public void Show(
        StatusEffectUI source,
        StatusEffectType statusType,
        int stackCount
    )
    {
        string title =
            statusType.ToString().Replace("_", " ") +
            " (" + stackCount + ")";

        Show(
            source,
            title,
            GetDescription(statusType)
        );
    }

    public void Show(
        MonoBehaviour source,
        string title,
        string description
    )
    {
        if (
            source == null ||
            !source.isActiveAndEnabled ||
            panel == null ||
            titleText == null ||
            descriptionText == null
        )
        {
            return;
        }

        owner = source;

        if (titleText.text != title)
        {
            titleText.text = title;
        }

        string details = description;

        if (string.IsNullOrWhiteSpace(details))
        {
            details = "No description available.";
        }

        if (descriptionText.text != details)
        {
            descriptionText.text = details;
        }

        if (!panel.gameObject.activeSelf)
        {
            panel.gameObject.SetActive(true);
        }

        panel.SetAsLastSibling();

        UpdatePosition();
    }

    public void Hide(MonoBehaviour source)
    {
        if (owner != source)
        {
            return;
        }

        Close();
    }

    private void Close()
    {
        owner = null;

        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    private void UpdatePosition()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            null,
            out Vector2 position
        );

        position += mouseOffset;

        Rect bounds = canvasRect.rect;
        const float padding = 12f;

        position.x = Mathf.Clamp(
            position.x,
            bounds.xMin + padding,
            bounds.xMax - panel.rect.width - padding
        );

        position.y = Mathf.Clamp(
            position.y,
            bounds.yMin + panel.rect.height + padding,
            bounds.yMax - padding
        );

        panel.anchoredPosition = position;
    }

    private string GetDescription(StatusEffectType statusType)
    {
        switch (statusType)
        {
            case StatusEffectType.ARMOR:
                return "Blocks 1 damage per Armor.";

            case StatusEffectType.WEAK:
                return "Deal 25% less Attack damage. 1 turn per stack.";

            case StatusEffectType.POISON:
                return "At turn start, take 1 damage per stack, then clear all Poison. Blocked by Armor.";

            case StatusEffectType.EXPOSED:
                return "Take 50% more Attack damage. 1 turn per stack.";

            case StatusEffectType.ARMOR_BREAK:
                return "Breaks 25% of current Armor (rounded up). Repeats on later turns. Loses 1 stack at turn end.";

            case StatusEffectType.STRENGTH:
                return "+1 Attack damage per stack.";

            case StatusEffectType.COMBO:
                return "Yellow Hero resource spent by cards. Kept between turns; resets each battle.";

            default:
                return "No description available.";
        }
    }
}