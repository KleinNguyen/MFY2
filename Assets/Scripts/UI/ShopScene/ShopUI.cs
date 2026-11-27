using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Shop panels")]
    [SerializeField] private MapUI mapUI;
    [SerializeField] private GameObject shopInventoryPanel;

    [Header("Card inventory")]
    [SerializeField] private List<CardData> shopCards;
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardButtonTexts;
    [SerializeField] private CardView cardPrefab;

    [Header("Card prices")]
    [SerializeField, Min(0)] private int commonPrice = 40;
    [SerializeField, Min(0)] private int uncommonPrice = 65;
    [SerializeField, Min(0)] private int rarePrice = 100;

    [Header("Potions")]
    [SerializeField] private Button healthButton;
    [SerializeField] private Button manaButton;
    [SerializeField, Min(0)] private int healthPotionPrice = 35;
    [SerializeField, Min(0)] private int manaPotionPrice = 35;

    [Header("Card removal")]
    [SerializeField] private Button removeButton;
    [SerializeField] private ShowDeckUI showDeckUI;

    [Header("Card hover")]
    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 20f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

    // Inventory and purchase state for the current shop visit.
    private readonly List<LibraryCardUI> cardViews = new();
    private readonly List<CardData> currentCards = new();
    private readonly List<int> currentPrices = new();
    private CardexCardFlightEffect cardFlightEffect;
    private bool healthPotionPurchased;
    private bool manaPotionPurchased;
    private int lastRemovePrice = -1;

    private void Awake()
    {
        cardFlightEffect = GetComponent<CardexCardFlightEffect>();
        if (cardFlightEffect == null) cardFlightEffect = gameObject.AddComponent<CardexCardFlightEffect>();
    }

    // Prepare a fresh inventory when entering a shop.
    public void Setup()
    {
        RunSystem run = RunSystem.Instance;
        if (!CanSetupShop(run)) return;

        shopInventoryPanel.SetActive(false);
        ClearCardViews();
        currentCards.Clear();
        currentPrices.Clear();

        healthPotionPurchased = false;
        manaPotionPurchased = false;
        UpdatePotionPriceLabels();

        if (healthButton != null) healthButton.interactable = true;
        if (manaButton != null) manaButton.interactable = true;

        List<CardData> availableCards = GetAvailableCards(run.HeroData.Character);
        if (availableCards.Count == 0) Debug.Log("No cards are available for this character.", this);

        PopulateCardSlots(availableCards);
    }

    // Open, close, and leave the shop.
    public void OpenMerchantInventory() => shopInventoryPanel.SetActive(true);
    public void CloseShopInventory() => shopInventoryPanel.SetActive(false);

    private void ExecuteLeaveShop()
    {
        RunSystem.Instance.CompleteMapPoint();
        gameObject.SetActive(false);
        mapUI.UpdateMapUI();
    }

    // Purchase cards and potions.
    private void ExecuteBuyCard(int index)
    {
        if (index < 0 || index >= currentCards.Count) return;

        Button button = cardButtons[index];
        if (button == null || !button.interactable) return;

        CardData card = currentCards[index];
        if (card == null) return;

        if (!RunSystem.Instance.BuyCard(card, currentPrices[index]))
        {
            Debug.Log("Not enough gold to buy " + card.name);
            return;
        }

        button.interactable = false;

        RewardCardHoverUI hoverUI = button.GetComponent<RewardCardHoverUI>();
        if (hoverUI != null) hoverUI.Finish(false);

        LibraryCardUI cardView = button.GetComponentInChildren<LibraryCardUI>();
        if (cardView == null)
        {
            Debug.LogError("Purchased card has no card view to animate.", this);
        }
        else if (!cardFlightEffect.TryFly(cardView.GetComponent<RectTransform>()))
        {
            cardView.gameObject.SetActive(false);
        }

        if (index < cardButtonTexts.Length && cardButtonTexts[index] != null) cardButtonTexts[index].text = "Sold";

        Debug.Log("Purchased: " + card.name);
    }

    private void ExecuteBuyHealthPotion()
    {
        if (healthPotionPurchased) return;

        if (!RunSystem.Instance.BuyHealthPotion(healthPotionPrice))
        {
            Debug.Log("Not enough gold to buy Health Potion.");
            return;
        }

        healthPotionPurchased = true;
        if (healthButton != null) healthButton.interactable = false;

        UpdatePotionPriceLabels();
        Debug.Log("Purchased: Health Potion");
    }

    private void ExecuteBuyManaPotion()
    {
        if (manaPotionPurchased) return;

        if (!RunSystem.Instance.BuyManaPotion(manaPotionPrice))
        {
            Debug.Log("Not enough gold to buy Mana Potion.");
            return;
        }

        manaPotionPurchased = true;
        if (manaButton != null) manaButton.interactable = false;

        UpdatePotionPriceLabels();
        Debug.Log("Purchased: Mana Potion");
    }
    // Refresh the shop label when the removal price changes.
    private void Update()
    {
        if (showDeckUI != null && lastRemovePrice != showDeckUI.RemovePrice)
            UpdatePotionPriceLabels();
    }

    // Ask before purchasing or completing the shop visit.
    public void BuyCard(int index)
    {
        RunSystem run = RunSystem.Instance;
        if (!CanShopNow() || index < 0 || index >= currentCards.Count ||
            index >= currentPrices.Count || index >= cardButtons.Length)
            return;

        CardData card = currentCards[index];
        int price = currentPrices[index];

        if (card == null || cardButtons[index] == null || !cardButtons[index].interactable || run.Gold < price) return;

        ConfirmationDialogUI.Ask(
            this,
            $"Do you want to buy {card.name} for {price} Gold?",
            () => ExecuteBuyCard(index),
            () => CanShopNow() &&
                RunSystem.Instance == run &&
                index < currentCards.Count &&
                index < currentPrices.Count &&
                currentCards[index] == card &&
                currentPrices[index] == price
        );
    }

    public void BuyHealthPotion() => AskPotionPurchase(true);
    public void BuyManaPotion() => AskPotionPurchase(false);

    public void LeaveShop()
    {
        RunSystem run = RunSystem.Instance;
        if (!CanShopNow()) return;

        ConfirmationDialogUI.Ask(this, "Do you want to leave the shop?", ExecuteLeaveShop,
            () => CanShopNow() && RunSystem.Instance == run);
    }

    private void AskPotionPurchase(bool health)
    {
        RunSystem run = RunSystem.Instance;
        if (!CanShopNow() || (health ? healthPotionPurchased : manaPotionPurchased)) return;

        int price = health ? healthPotionPrice : manaPotionPrice;
        if (run.Gold < price) return;

        string item = health ? "Health Potion" : "Mana Potion";
        System.Action purchase = health ? ExecuteBuyHealthPotion : ExecuteBuyManaPotion;

        ConfirmationDialogUI.Ask(
            this,
            $"Do you want to buy a {item} for {price} Gold?",
            purchase,
            () => CanShopNow() &&
                RunSystem.Instance == run &&
                price == (health ? healthPotionPrice : manaPotionPrice) &&
                !(health ? healthPotionPurchased : manaPotionPurchased)
        );
    }

    private bool CanShopNow() => isActiveAndEnabled && RunSystem.Instance != null && !RunSystem.Instance.IsRunFinished;

    // Filter the card pool and populate the available slots.
    private List<CardData> GetAvailableCards(CharacterType character)
    {
        List<CardData> availableCards = new();
        if (shopCards == null) return availableCards;

        foreach (CardData card in shopCards)
        {
            if (card == null || card.Character != character || card.Rarity == CardRarity.Basic) continue;
            if (!availableCards.Contains(card)) availableCards.Add(card);
        }

        return availableCards;
    }

    private void PopulateCardSlots(List<CardData> availableCards)
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            currentCards.Add(null);
            currentPrices.Add(0);

            Button button = cardButtons[i];
            if (button == null) continue;

            if (i >= cardButtonTexts.Length || cardButtonTexts[i] == null || availableCards.Count == 0)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            CardData card = CardRaritySelector.TakeRandomCard(availableCards);
            if (card == null)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            int price = GetCardPrice(card);
            if (i == 0) price = Mathf.RoundToInt(price * 0.8f);

            currentCards[i] = card;
            currentPrices[i] = price;
            button.gameObject.SetActive(true);
            button.interactable = true;
            CreateShopCard(i, card, price);
        }
    }

    // Create card visuals and configure their hover effects.
    private void CreateShopCard(int index, CardData cardData, int price)
    {
        Button button = cardButtons[index];
        Image buttonImage = button.GetComponent<Image>();

        if (buttonImage != null)
        {
            buttonImage.color = Color.clear;
            buttonImage.raycastTarget = true;
            button.targetGraphic = buttonImage;
        }

        button.transition = Selectable.Transition.None;

        LibraryCardUI cardUI = Instantiate(cardPrefab.LibraryTemplate, button.transform);
        cardUI.gameObject.SetActive(true);
        cardUI.Setup(cardData, RunSystem.Instance != null && RunSystem.Instance.IsCardUpgraded(cardData));

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition3D = Vector3.zero;
        cardRect.sizeDelta = new Vector2(260f, 340f);
        cardUI.SetLayoutScale(Vector3.one);
        cardRect.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        SetShopLabel(cardButtonTexts[index], index == 0 ? $"20% OFF\n{price} Gold" : $"{price} Gold");

        RewardCardHoverUI hoverUI = button.GetComponent<RewardCardHoverUI>();
        if (hoverUI == null) hoverUI = button.gameObject.AddComponent<RewardCardHoverUI>();
        hoverUI.Initialize(cardRect, hoverScale, hoverLift, hoverDuration, useSortingCanvas: false);

        cardViews.Add(cardUI);
    }

    private void ClearCardViews()
    {
        foreach (LibraryCardUI cardUI in cardViews)
        {
            if (cardUI == null) continue;

            cardUI.gameObject.SetActive(false);
            Destroy(cardUI.gameObject);
        }

        cardViews.Clear();
    }

    // Update item names, prices, and sold labels.
    private void UpdatePotionPriceLabels()
    {
        UpdateItemLabel(
            healthButton,
            healthPotionPurchased
                ? "Health Potion\nSold"
                : $"Health Potion\n{healthPotionPrice} Gold"
        );
        UpdateItemLabel(
            manaButton,
            manaPotionPurchased
                ? "Mana Potion\nSold"
                : $"Mana Potion\n{manaPotionPrice} Gold"
        );

        if (showDeckUI != null)
        {
            lastRemovePrice = showDeckUI.RemovePrice;
            UpdateItemLabel(removeButton, $"Remove Card\n{lastRemovePrice} Gold");
        }
    }

    private void UpdateItemLabel(Button button, string text)
    {
        if (button == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            Debug.LogWarning("Missing shop label: " + button.name, this);
            return;
        }

        SetShopLabel(label, text);
    }

    private void SetShopLabel(TMP_Text label, string text)
    {
        if (label == null) return;

        label.gameObject.SetActive(true);
        label.text = text;
        label.raycastTarget = false;
        label.enableAutoSizing = false;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = new Color(1f, 0.83f, 0.4f, 1f);

        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition3D = new Vector3(0f, -20f, 0f);
        rect.sizeDelta = new Vector2(280f, 60f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private int GetCardPrice(CardData card) => card.Rarity switch
    {
        CardRarity.Common => commonPrice,
        CardRarity.Uncommon => uncommonPrice,
        CardRarity.Rare => rarePrice,
        _ => 0
    };

    private bool CanSetupShop(RunSystem run)
    {
        if (run == null || run.HeroData == null || shopInventoryPanel == null ||
            cardPrefab == null || cardPrefab.LibraryTemplate == null ||
            cardButtons == null || cardButtonTexts == null)
        {
            Debug.LogError("ShopUI is missing references. Check the Inspector.", this);
            return false;
        }

        return true;
    }
    // Called by Event Trigger components on shop potion buttons.
    public void ShowHealthPotionTooltip()
    {
        if (StatusEffectTooltipUI.Instance == null) return;

        UsePotionUI potions = FindFirstObjectByType<UsePotionUI>();
        string effect = potions != null
            ? $"Restore {potions.HealthRestoreAmount} HP during combat."
            : "Restore HP during combat.";

        StatusEffectTooltipUI.Instance.Show(this, "Health Potion", $"{effect}\nPrice: {healthPotionPrice} Gold");
    }

    public void ShowManaPotionTooltip()
    {
        if (StatusEffectTooltipUI.Instance == null) return;

        UsePotionUI potions = FindFirstObjectByType<UsePotionUI>();
        string effect = potions != null
            ? $"Restore {potions.ManaRestoreAmount} mana during combat."
            : "Restore mana during combat.";

        StatusEffectTooltipUI.Instance.Show(this, "Mana Potion", $"{effect}\nPrice: {manaPotionPrice} Gold");
    }

    public void HidePotionTooltip()
    {
        if (StatusEffectTooltipUI.Instance != null)
            StatusEffectTooltipUI.Instance.Hide(this);
    }

#if UNITY_EDITOR
    // Populate the card list from project assets in the Unity Editor.
    [ContextMenu("Auto Fill Shop Cards")]
    private void AutoFillShopCards()
    {
        if (Application.isPlaying) return;

        List<CardData> cards = new();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Data/Cards" });

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null) cards.Add(card);
        }

        cards.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        UnityEditor.Undo.RecordObject(this, "Auto Fill Shop Cards");
        shopCards = cards;

        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);

        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log($"Shop Cards loaded: {shopCards.Count}", this);
    }
#endif
}