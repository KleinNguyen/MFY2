using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PerkUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image image;
    public Perk Perk { get; private set; }

    public void Setup(Perk perk)
    {
        if (perk == null || image == null) return;

        Perk = perk;
        image.sprite = perk.Image;
        image.raycastTarget = true;

        Canvas canvas = image.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Perk != null && StatusEffectTooltipUI.Instance != null)
            StatusEffectTooltipUI.Instance.Show(this, Perk.Title, Perk.Description);
    }

    public void OnPointerExit(PointerEventData eventData) => HideTooltip();
    private void OnDisable() => HideTooltip();

    private void HideTooltip()
    {
        if (StatusEffectTooltipUI.Instance != null)
            StatusEffectTooltipUI.Instance.Hide(this);
    }
}