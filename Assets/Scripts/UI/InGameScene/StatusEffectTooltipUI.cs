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
    private MonoBehaviour owner;

    // Configure the tooltip without blocking pointer events.
    protected override void Awake()
    {
        base.Awake();

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        canvasRect = GetComponent<RectTransform>();

        if (panel != null)
        {
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 1f);

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.gameObject.AddComponent<CanvasGroup>();

            group.interactable = false;
            group.blocksRaycasts = false;

            foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
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

    // Preserve existing status effect tooltips.
    public void Show(StatusEffectUI source, StatusEffectType statusType, int stackCount)
    {
        string title = statusType.ToString().Replace("_", " ") + " (" + stackCount + ")";
        Show(source, title, GetDescription(statusType));
    }

    // Display custom content for perks and potions.
    public void Show(MonoBehaviour source, string title, string description)
    {
        if (source == null || !source.isActiveAndEnabled || panel == null || titleText == null || descriptionText == null) return;

        owner = source;

        if (titleText.text != title) titleText.text = title;

        string details = string.IsNullOrWhiteSpace(description) ? "No description available." : description;
        if (descriptionText.text != details) descriptionText.text = details;

        if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);

        panel.SetAsLastSibling();
        UpdatePosition();
    }

    public void Hide(MonoBehaviour source)
    {
        if (owner != source) return;
        Close();
    }

    private void Close()
    {
        owner = null;
        if (panel != null) panel.gameObject.SetActive(false);
    }

    // Keep the tooltip inside the screen.
    private void UpdatePosition()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out Vector2 position);
        position += mouseOffset;

        Rect bounds = canvasRect.rect;
        const float padding = 12f;

        position.x = Mathf.Clamp(position.x, bounds.xMin + padding, bounds.xMax - panel.rect.width - padding);
        position.y = Mathf.Clamp(position.y, bounds.yMin + panel.rect.height + padding, bounds.yMax - padding);

        panel.anchoredPosition = position;
    }

    private string GetDescription(StatusEffectType statusType)
    {
        return statusType switch
        {
            StatusEffectType.ARMOR => "Absorbs incoming damage. Each point of Armor blocks 1 damage.",
            StatusEffectType.WEAK => "Reduces attack damage by 25%. Each stack lasts one turn.",
            StatusEffectType.POISON => "At the start of its turn, takes damage equal to its Poison stacks, then loses 1 stack.",
            StatusEffectType.EXPOSED => "Takes 50% more damage from attacks. Stacks track duration in turns.",
            StatusEffectType.ARMOR_BREAK => "Each turn, retains 75% of current Armor, rounded down, then loses 1 stack. Stacks still count down at 0 Armor.",
            StatusEffectType.STRENGTH => "Each stack adds 1 damage to attacks.",
            StatusEffectType.DEXTERITY => "Each stack adds 1 Armor to Armor gained from cards.",
            StatusEffectType.COMBO => "Resource used by Yellow Hero. Kept between turns and spent by card effects. Resets each combat.",
            _ => "No description available."
        };
    }
}