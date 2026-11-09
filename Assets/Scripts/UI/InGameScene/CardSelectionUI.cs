using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [SerializeField, Range(1, 5)] private int maxColumns = 5;
    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 24f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

    public bool IsOpen { get; private set; }

    private Action<Card> onCardSelected;
    private GridLayoutGroup cardGrid;
    private LayoutElement contentLayout;
    private Vector2 viewportSize;
    private int choiceCount;

    private void Awake()
    {
        Close();
    }

    private void OnDisable()
    {
        Close();
    }

    private void LateUpdate()
    {
        if (IsOpen && scrollRect.viewport.rect.size != viewportSize)
        {
            RefreshLayout();
        }
    }

    public bool Open(
        List<Card> cards,
        string message,
        Action<Card> onSelected
    )
    {
        if (IsOpen)
        {
            Debug.LogWarning("A card selection is already open.");
            return false;
        }

        if (panel == null ||
            cardContent is not RectTransform ||
            cardViewPrefab == null ||
            cardViewPrefab.LibraryTemplate == null ||
            scrollRect == null ||
            scrollRect.viewport == null)
        {
            Debug.LogError(
                "CardSelectionUI is missing references in the Inspector."
            );

            return false;
        }

        if (cards == null || onSelected == null)
        {
            return false;
        }

        List<Card> choices = cards.FindAll(card => card != null);

        if (choices.Count == 0)
        {
            return false;
        }

        cardGrid = cardContent.GetComponent<GridLayoutGroup>();

        if (cardGrid == null)
        {
            Debug.LogError(
                "CardSelectionUI needs a GridLayoutGroup on Card Content."
            );

            return false;
        }

        contentLayout = cardContent.GetComponent<LayoutElement>();

        if (contentLayout == null)
        {
            contentLayout = cardContent.gameObject.AddComponent<LayoutElement>();
        }

        ClearCards();

        onCardSelected = onSelected;
        choiceCount = choices.Count;
        IsOpen = true;

        panel.SetActive(true);
        cardContent.gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = message;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            CreateCardChoice(choices[i], i);
        }

        Canvas.ForceUpdateCanvases();

        RefreshLayout();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            (RectTransform)cardContent
        );

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;

        return true;
    }

    private void CreateCardChoice(Card card, int index)
    {
        GameObject slot = new(
            "CardSelectionSlot_" + index,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(RewardCardHoverUI)
        );

        slot.layer = cardContent.gameObject.layer;
        slot.transform.SetParent(cardContent, false);

        Image slotImage = slot.GetComponent<Image>();
        slotImage.color = new Color(1f, 1f, 1f, 0f);
        slotImage.raycastTarget = true;

        Button button = slot.GetComponent<Button>();
        button.targetGraphic = slotImage;
        button.transition = Selectable.Transition.None;

        LibraryCardUI cardUI = Instantiate(
            cardViewPrefab.LibraryTemplate,
            slot.transform
        );

        cardUI.gameObject.SetActive(true);
        cardUI.Setup(card);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();

        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition3D = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;

        float scale = Mathf.Min(
            cardGrid.cellSize.x / cardRect.rect.width,
            cardGrid.cellSize.y / cardRect.rect.height
        );

        cardRect.localScale = Vector3.one * scale;

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RewardCardHoverUI hoverUI = slot.GetComponent<RewardCardHoverUI>();

        hoverUI.Initialize(
            cardRect,
            hoverScale,
            hoverLift,
            hoverDuration,
            useSortingCanvas: false
        );

        button.onClick.AddListener(() => SelectCard(card));
    }

    private void RefreshLayout()
    {
        viewportSize = scrollRect.viewport.rect.size;

        float availableWidth =
            viewportSize.x - cardGrid.padding.horizontal;

        int columns = Mathf.FloorToInt(
            (availableWidth + cardGrid.spacing.x) /
            (cardGrid.cellSize.x + cardGrid.spacing.x)
        );

        cardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        cardGrid.constraintCount = Mathf.Max(
            1,
            Mathf.Min(maxColumns, choiceCount, columns)
        );

        contentLayout.minHeight = viewportSize.y;

        LayoutRebuilder.MarkLayoutForRebuild(
            (RectTransform)cardContent
        );
    }

    private void SelectCard(Card card)
    {
        if (!IsOpen)
        {
            return;
        }

        Action<Card> callback = onCardSelected;

        Close();

        callback?.Invoke(card);
    }

    public void Close()
    {
        IsOpen = false;
        onCardSelected = null;

        if (scrollRect != null)
        {
            scrollRect.StopMovement();
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void ClearCards()
    {
        for (int i = cardContent.childCount - 1; i >= 0; i--)
        {
            GameObject oldCard = cardContent.GetChild(i).gameObject;

            oldCard.SetActive(false);
            Destroy(oldCard);
        }
    }
}