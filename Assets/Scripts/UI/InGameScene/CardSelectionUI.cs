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
    [SerializeField] private GameObject closeButton;
    [SerializeField, Range(1, 5)] private int maxColumns = 5;
    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 24f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

    public bool IsOpen { get; private set; }
    public bool IsReadOnly { get; private set; }

    private Action<Card> onCardSelected;
    private Action onSelectionFinished;
    private Button optionalFinishButton;
    private TMP_Text optionalFinishLabel;
    private string originalCloseButtonLabel;
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
        if (IsOpen && IsReadOnly &&
            ((ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming) ||
             (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)))
        {
            Close();
            return;
        }

        if (IsOpen && scrollRect.viewport.rect.size != viewportSize)
        {
            RefreshLayout();
        }
    }

    public bool Open(List<Card> cards, string message, Action<Card> onSelected,
        bool readOnly = false, Action onFinished = null)
    {
        if (IsOpen)
        {
            Debug.LogWarning("A card selection is already open.");
            return false;
        }

        if (panel == null || cardContent is not RectTransform || cardViewPrefab == null ||
            cardViewPrefab.LibraryTemplate == null || scrollRect == null || scrollRect.viewport == null)
        {
            Debug.LogError("CardSelectionUI is missing references in the Inspector.");
            return false;
        }

        if (cards == null || (!readOnly && onSelected == null))
        {
            return false;
        }

        if ((readOnly || onFinished != null) && closeButton == null)
        {
            Debug.LogError("Assign the Close Button in CardSelectionUI before viewing a pile or allowing Done.", this);
            return false;
        }

        if (onFinished != null && closeButton.GetComponent<Button>() == null)
        {
            Debug.LogError("The Close Button needs a Button component to finish card selection.", this);
            return false;
        }

        List<Card> choices = cards.FindAll(card => card != null);

        if (choices.Count == 0 && !readOnly)
        {
            return false;
        }

        cardGrid = cardContent.GetComponent<GridLayoutGroup>();

        if (cardGrid == null)
        {
            Debug.LogError("CardSelectionUI needs a GridLayoutGroup on Card Content.");
            return false;
        }

        contentLayout = cardContent.GetComponent<LayoutElement>();

        if (contentLayout == null)
        {
            contentLayout = cardContent.gameObject.AddComponent<LayoutElement>();
        }

        ClearCards();

        onCardSelected = onSelected;
        onSelectionFinished = onFinished;
        IsReadOnly = readOnly;
        IsOpen = true;
        choiceCount = choices.Count;

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (closeButton != null) closeButton.SetActive(readOnly || onFinished != null);
        if (onFinished != null)
        {
            optionalFinishButton = closeButton.GetComponent<Button>();
            optionalFinishButton.onClick.AddListener(FinishOptionalSelection);
            optionalFinishLabel = closeButton.GetComponentInChildren<TMP_Text>(true);

            if (optionalFinishLabel != null)
            {
                originalCloseButtonLabel = optionalFinishLabel.text;
                optionalFinishLabel.text = "Done";
            }
        }
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
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)cardContent);
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;

        return true;
    }

    private void CreateCardChoice(Card card, int index)
    {
        GameObject slot = new("CardSelectionSlot_" + index,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(RewardCardHoverUI));

        slot.layer = cardContent.gameObject.layer;
        slot.transform.SetParent(cardContent, false);

        Image slotImage = slot.GetComponent<Image>();
        slotImage.color = new Color(1f, 1f, 1f, 0f);
        slotImage.raycastTarget = true;

        Button button = slot.GetComponent<Button>();
        button.targetGraphic = slotImage;
        button.transition = Selectable.Transition.None;

        LibraryCardUI cardUI = Instantiate(cardViewPrefab.LibraryTemplate, slot.transform);
        cardUI.gameObject.SetActive(true);
        cardUI.Setup(card);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition3D = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;
        float scale = Mathf.Min(cardGrid.cellSize.x / cardRect.rect.width,
            cardGrid.cellSize.y / cardRect.rect.height);
        cardUI.SetLayoutScale(Vector3.one * scale);

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        slot.GetComponent<RewardCardHoverUI>().Initialize(
            cardRect, hoverScale, hoverLift, hoverDuration, useSortingCanvas: false);
        button.onClick.AddListener(() => SelectCard(card));
    }

    private void RefreshLayout()
    {
        viewportSize = scrollRect.viewport.rect.size;
        float availableWidth = viewportSize.x - cardGrid.padding.horizontal;
        int columns = Mathf.FloorToInt((availableWidth + cardGrid.spacing.x) /
            (cardGrid.cellSize.x + cardGrid.spacing.x));

        cardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        cardGrid.constraintCount = Mathf.Max(1, Mathf.Min(maxColumns, choiceCount, columns));
        contentLayout.minHeight = viewportSize.y;

        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)cardContent);
    }

    private void SelectCard(Card card)
    {
        if (!IsOpen || IsReadOnly)
        {
            return;
        }

        Action<Card> callback = onCardSelected;
        onSelectionFinished = null;

        Close();

        callback?.Invoke(card);
    }

    private void FinishOptionalSelection()
    {
        if (IsOpen && onSelectionFinished != null)
        {
            Close();
        }
    }

    public void Close()
    {
        Action finishedCallback = onSelectionFinished;
        onSelectionFinished = null;

        if (optionalFinishButton != null)
        {
            optionalFinishButton.onClick.RemoveListener(FinishOptionalSelection);
            optionalFinishButton = null;
        }

        if (optionalFinishLabel != null)
        {
            optionalFinishLabel.text = originalCloseButtonLabel;
            optionalFinishLabel = null;
        }

        IsOpen = false;
        IsReadOnly = false;
        onCardSelected = null;
        if (closeButton != null) closeButton.SetActive(false);

        if (scrollRect != null)
        {
            scrollRect.StopMovement();
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }

        finishedCallback?.Invoke();
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
