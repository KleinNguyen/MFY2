using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowDeckUI : MonoBehaviour
{
    private enum DeckMode
    {
        View,
        Remove,
        Upgrade
    }

    [SerializeField] private GameObject deckPanel;
    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 24f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

    [SerializeField] private GameObject removeControls;
    [SerializeField] private Button confirmRemoveButton;
    [SerializeField] private TMP_Text confirmRemoveText;
    [SerializeField, Min(0)] private int removePrice = 75;
    [SerializeField] private GameObject upgradeControls;
    [SerializeField] private Button confirmUpgradeButton;
    [SerializeField] private TMP_Text upgradeInfoText;

    private DeckMode mode;

    private int selectedCardIndex = -1;
    private CardData selectedCard;
    private LibraryCardUI selectedCardUI;

    private System.Action<CardData> onUpgradeConfirmed;
    private System.Func<bool> canUpgradeNow;

    private void Awake() => CloseDeck();

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

        confirmRemoveText.text = "Remove: " + removePrice + " Gold";
    }

    public bool OpenUpgradeCard(System.Action<CardData> onUpgraded, System.Func<bool> canUpgrade)
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

    private void OpenPanel(DeckMode newMode)
    {
        CloseDeck();

        mode = newMode;
        if (scrollRect != null)
        {
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        if (removeControls != null) removeControls.SetActive(mode == DeckMode.Remove);
        if (upgradeControls != null) upgradeControls.SetActive(mode == DeckMode.Upgrade);

        deckPanel.SetActive(true);
        deckPanel.transform.SetAsLastSibling();

        RefreshCards();
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

    private void RefreshCards()
    {
        if (!CanOpenPanel()) return;

        for (int i = cardContent.childCount - 1; i >= 0; i--)
        {
            GameObject oldCard = cardContent.GetChild(i).gameObject;

            oldCard.SetActive(false);
            Destroy(oldCard);
        }

        RunSystem run = RunSystem.Instance;
        bool hasUpgradeableCard = false;

        for (int i = 0; i < run.Deck.Count; i++)
        {
            CardData cardData = run.Deck[i];

            if (cardData == null) continue;

            bool isUpgraded = run.IsCardUpgraded(cardData);
            bool canUpgrade = cardData.CanUpgrade && !isUpgraded;

            if (canUpgrade)
            {
                hasUpgradeableCard = true;
            }

            CreateDeckCard(cardData, i, isUpgraded, canUpgrade);
        }

        if (mode == DeckMode.Upgrade)
        {
            upgradeInfoText.text = hasUpgradeableCard
                ? "Choose a card to preview its upgrade."
                : "No cards available to upgrade.";
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
    private void CreateDeckCard(
    CardData cardData,
    int cardIndex,
    bool isUpgraded,
    bool canUpgrade
)
    {
        GameObject slot = new(
            "DeckCardSlot_" + cardIndex,
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

        Button cardButton = slot.GetComponent<Button>();
        cardButton.targetGraphic = slotImage;
        cardButton.transition = Selectable.Transition.None;
        cardButton.interactable = true;

        LibraryCardUI cardUI = Instantiate(
            cardPrefab.LibraryTemplate,
            slot.transform
        );

        cardUI.gameObject.SetActive(true);
        cardUI.Setup(cardData, isUpgraded);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();

        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition3D = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = mode == DeckMode.Upgrade && !canUpgrade
            ? 0.4f
            : 1f;

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

        bool canSelect = mode == DeckMode.Remove ||
            (mode == DeckMode.Upgrade && canUpgrade);

        if (canSelect)
        {
            cardButton.onClick.AddListener(
                () => SelectCard(cardIndex, cardData, cardUI)
            );
        }
    }

    private void SelectCard(int cardIndex, CardData cardData, LibraryCardUI cardUI)
    {
        if (mode == DeckMode.View) return;

        RunSystem run = RunSystem.Instance;

        if (run == null || run.Deck == null) return;
        if (cardIndex < 0 || cardIndex >= run.Deck.Count) return;
        if (run.Deck[cardIndex] != cardData) return;

        if (mode == DeckMode.Upgrade && (!cardData.CanUpgrade || run.IsCardUpgraded(cardData))) return;

        ClearSelection();

        selectedCardIndex = cardIndex;
        selectedCard = cardData;
        selectedCardUI = cardUI;

        if (mode == DeckMode.Remove)
        {
            confirmRemoveButton.interactable = run.Gold >= removePrice;
            confirmRemoveText.text = run.Gold >= removePrice ? "Remove: " + removePrice + " Gold" : "Need " + removePrice + " Gold";
            return;
        }

        selectedCardUI.Setup(cardData, true);

        upgradeInfoText.text = cardData.name + " → " + cardData.name + "+\nMana: " + cardData.Mana + " → " + cardData.UpgradedMana + "\n" + cardData.UpgradedDescription;

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

    private bool IsSelectionValid()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.Deck == null || selectedCard == null) return false;
        if (selectedCardIndex < 0 || selectedCardIndex >= run.Deck.Count) return false;

        return run.Deck[selectedCardIndex] == selectedCard;
    }

    public void ConfirmRemoveCard()
    {
        if (mode != DeckMode.Remove) return;

        if (!IsSelectionValid())
        {
            ClearSelection();
            RefreshCards();
            return;
        }

        bool success = RunSystem.Instance.RemoveCardFromDeck(selectedCardIndex, removePrice);

        if (!success)
        {
            confirmRemoveText.text = "Could not remove this card.";
            return;
        }

        ClearSelection();
        RefreshCards();

        confirmRemoveText.text = "Remove: " + removePrice + " Gold";
    }

    public void ConfirmUpgradeCard()
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
        System.Action<CardData> callback = onUpgradeConfirmed;

        CloseDeck();

        callback?.Invoke(upgradedCard);
    }
}