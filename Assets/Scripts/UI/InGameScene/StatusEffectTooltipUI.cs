using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class StatusEffectTooltipUI : Singleton<StatusEffectTooltipUI>
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Vector2 mouseOffset = new(24f, -20f);

    private RectTransform canvasRect;
    private StatusEffectUI owner;

    protected override void Awake()
    {
        base.Awake();

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

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

            foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        Close();
    }

    private void LateUpdate()
    {
        if (panel == null || !panel.gameObject.activeSelf) return;

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
        if (source == null ||
            panel == null ||
            titleText == null ||
            descriptionText == null)
        {
            return;
        }

        owner = source;

        titleText.text =
            statusType.ToString().Replace("_", " ") +
            " (" + stackCount + ")";

        descriptionText.text = GetDescription(statusType);

        panel.gameObject.SetActive(true);
        panel.SetAsLastSibling();

        UpdatePosition();
    }

    public void Hide(StatusEffectUI source)
    {
        if (owner != source) return;

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
        float padding = 12f;

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
        return statusType switch
        {
            StatusEffectType.ARMOR =>
                "Absorbs incoming damage. Each point of Armor blocks 1 damage.",

            StatusEffectType.WEAK =>
                "Reduces enemy attack damage by 25%. Loses 1 stack after the enemy attacks.",

            StatusEffectType.POISON =>
                "At the start of its turn, takes damage equal to its Poison stacks, then loses 1 stack.",

            StatusEffectType.EXPOSED =>
                "Takes 50% more damage from attacks. Loses 1 stack at the end of its turn.",

            StatusEffectType.ARMOR_BREAK =>
                "Gains 25% less Armor. Loses 1 stack at the end of its turn.",

            StatusEffectType.STRENGTH =>
                "Each stack adds 1 damage to hero attacks.",

            StatusEffectType.DEXTERITY =>
                "Each stack adds 1 Armor to Armor gained from cards.",

            StatusEffectType.COMBO =>
                "Resource used by Yellow cards. Kept between turns and spent by card effects. Resets each combat.",

            _ => "No description available."
        };
    }
}
