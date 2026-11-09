using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Button), typeof(Image))]
public class RewardCardHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform visual;
    private Button slotButton;
    private Image slotImage;
    private Canvas visualCanvas;

    private Vector2 normalPosition;
    private Vector3 normalScale;

    private bool normalOverrideSorting;
    private int normalSortingOrder;
    private int normalSortingLayer;

    private float hoverScale;
    private float hoverLift;
    private float hoverDuration;

    private bool locked;

    public void Initialize(
        RectTransform cardVisual,
        float scale,
        float lift,
        float duration,
        bool useSortingCanvas = true
    )
    {
        visual = cardVisual;
        slotButton = GetComponent<Button>();
        slotImage = GetComponent<Image>();

        hoverScale = Mathf.Max(1f, scale);
        hoverLift = Mathf.Max(0f, lift);
        hoverDuration = Mathf.Max(0.01f, duration);

        normalPosition = visual.anchoredPosition;
        normalScale = visual.localScale;

        visualCanvas = useSortingCanvas
            ? visual.GetComponent<Canvas>()
            : null;

        if (useSortingCanvas && visualCanvas == null)
        {
            visualCanvas = visual.gameObject.AddComponent<Canvas>();
        }

        if (visualCanvas != null)
        {
            normalOverrideSorting = visualCanvas.overrideSorting;
            normalSortingOrder = visualCanvas.sortingOrder;
            normalSortingLayer = visualCanvas.sortingLayerID;
        }

        locked = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (locked || visual == null || slotButton == null || !slotButton.IsInteractable()) return;

        AnimateHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (locked || visual == null || !isActiveAndEnabled) return;

        AnimateHover(false);
    }

    private void AnimateHover(bool hovered)
    {
        visual.DOKill();

        if (hovered && visualCanvas != null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();

            visualCanvas.overrideSorting = true;

            if (parentCanvas != null)
            {
                visualCanvas.sortingLayerID = parentCanvas.sortingLayerID;
                visualCanvas.sortingOrder = parentCanvas.sortingOrder + 1;
            }
            else
            {
                visualCanvas.sortingOrder = 1;
            }
        }
        else
        {
            RestoreSorting();
        }

        Vector2 targetPosition = hovered ? normalPosition + Vector2.up * hoverLift : normalPosition;
        Vector3 targetScale = hovered ? normalScale * hoverScale : normalScale;

        visual.DOAnchorPos(targetPosition, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        visual.DOScale(targetScale, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void Finish(bool hideVisual)
    {
        locked = true;

        if (slotButton != null) slotButton.interactable = false;
        if (slotImage != null) slotImage.raycastTarget = false;

        ResetVisual();

        if (hideVisual && visual != null) visual.gameObject.SetActive(false);
    }

    private void RestoreSorting()
    {
        if (visualCanvas == null) return;

        visualCanvas.overrideSorting = normalOverrideSorting;
        visualCanvas.sortingOrder = normalSortingOrder;
        visualCanvas.sortingLayerID = normalSortingLayer;
    }

    private void ResetVisual()
    {
        if (visual == null) return;

        visual.DOKill();
        visual.anchoredPosition = normalPosition;
        visual.localScale = normalScale;

        RestoreSorting();
    }

    private void OnDisable() => ResetVisual();
}