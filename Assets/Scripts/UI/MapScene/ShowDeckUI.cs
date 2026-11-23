using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowDeckUI : MonoBehaviour
{
    private enum DeckMode { View, Remove, Upgrade }

    [Header("Deck panel")]
    [SerializeField] private GameObject deckPanel;
    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Card hover")]
    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 24f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

    [Header("Remove card")]
    [SerializeField] private GameObject removeControls;
    [SerializeField] private Button confirmRemoveButton;
    [SerializeField] private TMP_Text confirmRemoveText;

    [Header("Upgrade card")]
    [SerializeField] private GameObject upgradeControls;
    [SerializeField] private Button confirmUpgradeButton;
    [SerializeField] private TMP_Text upgradeInfoText;

    // Panel state and selected card.
    private DeckMode mode;
    private int selectedCardIndex = -1;
    private CardData selectedCard;
    private LibraryCardUI selectedCardUI;
    private Action<CardData> onUpgradeConfirmed;
    private Func<bool> canUpgradeNow;

    public int RemovePrice
    {
        get
        {
            RunSystem run = RunSystem.Instance;
            if (run == null) return 0;

            return run.CardRemovalPrice;
        }
    }

    private void Awake() => CloseDeck();

    // Open and close the deck panel.
    public void OpenDeck()
    {
        if (!CanOpenPanel()) return;
        OpenPanel(DeckMode.View);
    }

    public void OpenRemoveCard()
    {
        if (!CanOpenPanel()) return;

        if (removeControls == null || confirmRemoveButton == null || confirmRemoveText == null)
        {
            Debug.LogError("Remove controls are not assigned in ShowDeckUI.");
            return;
        }

        OpenPanel(DeckMode.Remove);
        confirmRemoveText.text = $"Remove: {RemovePrice} Gold";
    }

    public bool OpenUpgradeCard(Action<CardData> onUpgraded, Func<bool> canUpgrade)
    {
        if (!CanOpenPanel()) return false;
        if (onUpgraded == null || canUpgrade == null || !canUpgrade()) return false;

        if (upgradeControls == null || confirmUpgradeButton == null || upgradeInfoText == null)
        {
            Debug.LogError("Upgrade controls are not assigned in ShowDeckUI.");
            return false;
        }

        OpenPanel(DeckMode.Upgrade);
        onUpgradeConfirmed = onUpgraded;
        canUpgradeNow = canUpgrade;
        return true;
    }

    public void CloseDeck()
    {
        ClearSelection();
        onUpgradeConfirmed = null;
        canUpgradeNow = null;
        mode = DeckMode.View;

        if (removeControls != null) removeControls.SetActive(false);
        if (upgradeControls != null) upgradeControls.SetActive(false);
        if (deckPanel != null) deckPanel.SetActive(false);
    }

    private void OpenPanel(DeckMode newMode)
    {
        CloseDeck();
        mode = newMode;

        if (scrollRect != null) scrollRect.movementType = ScrollRect.MovementType.Clamped;
        if (removeControls != null) removeControls.SetActive(mode == DeckMode.Remove);
        if (upgradeControls != null) upgradeControls.SetActive(mode == DeckMode.Upgrade);

        deckPanel.SetActive(true);
        deckPanel.transform.SetAsLastSibling();
        RefreshCards();
    }

    // Confirm card removal or upgrade.
    private void ExecuteRemoveCard()
    {
        if (mode != DeckMode.Remove) return;

        if (!IsSelectionValid())
        {
            ClearSelection();
            RefreshCards();
            return;
        }

        if (!RunSystem.Instance.RemoveCardFromDeck(selectedCardIndex, RemovePrice))
        {
            confirmRemoveText.text = "Could not remove this card.";
            return;
        }

        ClearSelection();
        RefreshCards();
        confirmRemoveText.text = $"Remove: {RemovePrice} Gold";
    }

    private void ExecuteUpgradeCard()
    {
        if (mode != DeckMode.Upgrade) return;

        if (canUpgradeNow == null || !canUpgradeNow())
        {
            upgradeInfoText.text = "You cannot upgrade a card here now.";
            confirmUpgradeButton.interactable = false;
            return;
        }

        if (!IsSelectionValid())
        {
            ClearSelection();
            RefreshCards();
            return;
        }

        RunSystem run = RunSystem.Instance;
        if (!run.UpgradeDeckCard(selectedCardIndex))
        {
            ClearSelection();
            RefreshCards();
            upgradeInfoText.text = "Could not upgrade this card.";
            return;
        }

        CardData upgradedCard = run.Deck[selectedCardIndex];
        Action<CardData> callback = onUpgradeConfirmed;

        CloseDeck();
        callback?.Invoke(upgradedCard);
    }
    // Ask after a specific card has been selected.
    public void ConfirmRemoveCard()
    {
        RunSystem run = RunSystem.Instance;
        if (mode != DeckMode.Remove || !IsSelectionValid() || run.IsRunFinished || run.Gold < RemovePrice) return;

        int index = selectedCardIndex;
        CardData card = selectedCard;
        int price = RemovePrice;

        ConfirmationDialogUI.Ask(this, $"Do you want to remove {card.name} for {price} Gold?", ExecuteRemoveCard,
            () => RunSystem.Instance == run && !run.IsRunFinished && RemovePrice == price && MatchesSelection(index, card, DeckMode.Remove));
    }

    public void ConfirmUpgradeCard()
    {
        RunSystem run = RunSystem.Instance;
        if (mode != DeckMode.Upgrade || !IsSelectionValid() || run.IsRunFinished || canUpgradeNow == null || !canUpgradeNow()) return;

        int index = selectedCardIndex;
        CardData card = selectedCard;

        ConfirmationDialogUI.Ask(this, $"Do you want to upgrade {card.name}?", ExecuteUpgradeCard,
            () => RunSystem.Instance == run && !run.IsRunFinished && MatchesSelection(index, card, DeckMode.Upgrade) && canUpgradeNow != null && canUpgradeNow());
    }

    private bool MatchesSelection(int index, CardData card, DeckMode expectedMode)
    {
        return deckPanel != null && deckPanel.activeInHierarchy && mode == expectedMode &&
            selectedCardIndex == index && selectedCard == card && IsSelectionValid();
    }

    // Rebuild the deck display.
    private void RefreshCards()
    {
        if (!CanOpenPanel()) return;

        ClearSelection();

        for (int i = cardContent.childCount - 1; i >= 0; i--)
        {
            GameObject oldCard = cardContent.GetChild(i).gameObject;
            oldCard.SetActive(false);
            Destroy(oldCard);
        }

        RunSystem run = RunSystem.Instance;
        HashSet<string> displayedCardIds = new HashSet<string>();
        HashSet<CardData> displayedCardAssets = new HashSet<CardData>();
        bool hasUpgradeableCard = false;

        for (int i = 0; i < run.Deck.Count; i++)
        {
            CardData cardData = run.Deck[i];
            if (cardData == null) continue;

            bool isUpgraded = run.IsCardUpgraded(cardData);
            bool canUpgrade = cardData.CanUpgrade && !isUpgraded;

            if (mode == DeckMode.Upgrade)
            {
                if (!canUpgrade) continue;
                if (!displayedCardAssets.Add(cardData)) continue;
                if (!string.IsNullOrWhiteSpace(cardData.SaveId) && !displayedCardIds.Add(cardData.SaveId)) continue;

                hasUpgradeableCard = true;
            }

            // Keep the original deck index after filtering duplicate types.
            CreateDeckCard(cardData, i, isUpgraded, canUpgrade);
        }

        if (mode == DeckMode.Upgrade)
        {
            if (hasUpgradeableCard)
            {
                upgradeInfoText.text = "Choose a card type to preview its upgrade.";
            }
            else
            {
                upgradeInfoText.text = "No cards available to upgrade.";
            }
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void CreateDeckCard(CardData cardData, int cardIndex, bool isUpgraded, bool canUpgrade)
    {
        GameObject slot = new("DeckCardSlot_" + cardIndex, typeof(RectTransform), typeof(Image), typeof(Button), typeof(RewardCardHoverUI));
        slot.layer = cardContent.gameObject.layer;
        slot.transform.SetParent(cardContent, false);

        Image slotImage = slot.GetComponent<Image>();
        slotImage.color = new Color(1f, 1f, 1f, 0f);
        slotImage.raycastTarget = true;

        Button cardButton = slot.GetComponent<Button>();
        cardButton.targetGraphic = slotImage;
        cardButton.transition = Selectable.Transition.None;
        cardButton.interactable = true;

        LibraryCardUI cardUI = Instantiate(cardPrefab.LibraryTemplate, slot.transform);
        cardUI.gameObject.SetActive(true);
        cardUI.Setup(cardData, isUpgraded);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition3D = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = mode == DeckMode.Upgrade && !canUpgrade ? 0.4f : 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RewardCardHoverUI hoverUI = slot.GetComponent<RewardCardHoverUI>();
        hoverUI.Initialize(cardRect, hoverScale, hoverLift, hoverDuration, useSortingCanvas: false);

        bool canSelect = mode == DeckMode.Remove || (mode == DeckMode.Upgrade && canUpgrade);
        if (canSelect) cardButton.onClick.AddListener(() => SelectCard(cardIndex, cardData, cardUI));
    }

    // Select a card and preview its upgrade.
    private void SelectCard(int cardIndex, CardData cardData, LibraryCardUI cardUI)
    {
        if (mode == DeckMode.View) return;

        RunSystem run = RunSystem.Instance;
        if (run == null || run.Deck == null) return;
        if (cardIndex < 0 || cardIndex >= run.Deck.Count || run.Deck[cardIndex] != cardData) return;
        if (mode == DeckMode.Upgrade && (!cardData.CanUpgrade || run.IsCardUpgraded(cardData))) return;

        ClearSelection();
        selectedCardIndex = cardIndex;
        selectedCard = cardData;
        selectedCardUI = cardUI;

        if (mode == DeckMode.Remove)
        {
            bool canAfford = run.Gold >= RemovePrice;
            confirmRemoveButton.interactable = canAfford;
            confirmRemoveText.text = canAfford ? $"Remove: {RemovePrice} Gold" : $"Need {RemovePrice} Gold";
            return;
        }

        selectedCardUI.Setup(cardData, true);
        upgradeInfoText.text = $"{cardData.name} → {cardData.name}+\nMana: {cardData.Mana} → {cardData.UpgradedMana}\n{cardData.UpgradedDescription}";
        confirmUpgradeButton.interactable = true;
    }

    private void ClearSelection()
    {
        if (mode == DeckMode.Upgrade && selectedCardUI != null && selectedCard != null)
        {
            bool isUpgraded = RunSystem.Instance != null && RunSystem.Instance.IsCardUpgraded(selectedCard);
            selectedCardUI.Setup(selectedCard, isUpgraded);
        }

        selectedCardIndex = -1;
        selectedCard = null;
        selectedCardUI = null;

        if (confirmRemoveButton != null) confirmRemoveButton.interactable = false;
        if (confirmUpgradeButton != null) confirmUpgradeButton.interactable = false;
    }

    // Validate panel references and card selection.
    private bool CanOpenPanel()
    {
        if (RunSystem.Instance == null || RunSystem.Instance.Deck == null)
        {
            Debug.LogWarning("No active run or deck was found.");
            return false;
        }

        if (deckPanel == null || cardContent == null || cardPrefab == null || cardPrefab.LibraryTemplate == null)
        {
            Debug.LogError("Deck Panel, Card Content, Card Prefab or Library Template is missing in ShowDeckUI.");
            return false;
        }

        return true;
    }

    private bool IsSelectionValid()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.Deck == null || selectedCard == null) return false;
        if (selectedCardIndex < 0 || selectedCardIndex >= run.Deck.Count) return false;

        return run.Deck[selectedCardIndex] == selectedCard;
    }
}