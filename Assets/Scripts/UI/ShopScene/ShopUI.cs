using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private MapUI mapUI;
    [SerializeField] private GameObject shopInventoryPanel;

    [SerializeField] private List<CardData> shopCards;
    [SerializeField] private int commonPrice = 50;
    [SerializeField] private int uncommonPrice = 75;
    [SerializeField] private int rarePrice = 120;

    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardButtonTexts;
    [SerializeField] private CardView cardPrefab;

    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 20f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

    private readonly List<LibraryCardUI> cardViews = new();

    [SerializeField] private int healthPotionPrice = 50;
    [SerializeField] private int manaPotionPrice = 50;

    [SerializeField] private Button healthButton;
    [SerializeField] private Button manaButton;

    private bool healthPotionPurchased;
    private bool manaPotionPurchased;

    private readonly List<CardData> currentCards = new();
    private readonly List<int> currentPrices = new();

    public void Setup()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null ||
            run.HeroData == null ||
            shopInventoryPanel == null ||
            cardPrefab == null ||
            cardPrefab.LibraryTemplate == null ||
            cardButtons == null ||
            cardButtonTexts == null)
        {
            Debug.LogError(
                "ShopUI is missing references. Check the Inspector.",
                this
            );

            return;
        }

        shopInventoryPanel.SetActive(false);

        ClearCardViews();

        currentCards.Clear();
        currentPrices.Clear();

        healthPotionPurchased = false;
        manaPotionPurchased = false;

        if (healthButton != null)
        {
            healthButton.interactable = true;
        }

        if (manaButton != null)
        {
            manaButton.interactable = true;
        }

        List<CardData> availableCards = new();

        if (shopCards != null)
        {
            foreach (CardData card in shopCards)
            {
                if (card == null) continue;
                if (card.Character != run.HeroData.Character) continue;
                if (card.Rarity == CardRarity.Basic) continue;
                if (availableCards.Contains(card)) continue;

                availableCards.Add(card);
            }
        }

        if (availableCards.Count == 0)
        {
            Debug.LogError(
                "Shop has no valid cards for " +
                run.HeroData.Character +
                ". Check Shop Cards in the Inspector.",
                this
            );
        }

        for (int i = 0; i < cardButtons.Length; i++)
        {
            currentCards.Add(null);
            currentPrices.Add(0);

            Button button = cardButtons[i];

            if (button == null) continue;

            if (i >= cardButtonTexts.Length ||
                cardButtonTexts[i] == null ||
                availableCards.Count == 0)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            CardData card = CardRaritySelector.TakeRandomCard(
                availableCards
            );

            if (card == null)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            int price = GetCardPrice(card);

            if (i == 0)
            {
                price = Mathf.RoundToInt(price * 0.8f);
            }

            currentCards[i] = card;
            currentPrices[i] = price;

            button.gameObject.SetActive(true);
            button.interactable = true;

            CreateShopCard(i, card, price);
        }
    }
    private void CreateShopCard(
    int index,
    CardData cardData,
    int price
)
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

        LibraryCardUI cardUI = Instantiate(
            cardPrefab.LibraryTemplate,
            button.transform
        );

        cardUI.gameObject.SetActive(true);
        cardUI.Setup(cardData, false);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();

        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition3D = Vector3.zero;
        cardRect.sizeDelta = new Vector2(260f, 340f);
        cardRect.localScale = Vector3.one;
        cardRect.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        TMP_Text priceText = cardButtonTexts[index];

        priceText.gameObject.SetActive(true);
        priceText.raycastTarget = false;
        priceText.enableAutoSizing = false;
        priceText.fontSize = 22f;
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.color = new Color(1f, 0.83f, 0.4f, 1f);

        priceText.text = index == 0
            ? "20% OFF\n" + price + " Gold"
            : price + " Gold";

        RectTransform priceRect = priceText.rectTransform;

        priceRect.anchorMin = new Vector2(0.5f, 0f);
        priceRect.anchorMax = new Vector2(0.5f, 0f);
        priceRect.pivot = new Vector2(0.5f, 1f);
        priceRect.anchoredPosition3D = new Vector3(0f, -8f, 0f);
        priceRect.sizeDelta = new Vector2(280f, 48f);
        priceRect.localScale = Vector3.one;
        priceRect.localRotation = Quaternion.identity;

        RewardCardHoverUI hoverUI =
            button.GetComponent<RewardCardHoverUI>();

        if (hoverUI == null)
        {
            hoverUI = button.gameObject.AddComponent<RewardCardHoverUI>();
        }

        hoverUI.Initialize(
            cardRect,
            hoverScale,
            hoverLift,
            hoverDuration,
            useSortingCanvas: false
        );

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
    public void OpenMerchantInventory()
    {
        shopInventoryPanel.SetActive(true);
    }

    public void CloseShopInventory()
    {
        shopInventoryPanel.SetActive(false);
    }

    public void BuyCard(int index)
    {
        if (index < 0 || index >= currentCards.Count) return;

        Button button = cardButtons[index];

        if (button == null || !button.interactable) return;

        CardData card = currentCards[index];

        if (card == null) return;

        int price = currentPrices[index];

        bool success = RunSystem.Instance.BuyCard(card, price);

        if (!success)
        {
            Debug.Log("Not enough gold to buy " + card.name);
            return;
        }

        button.interactable = false;

        RewardCardHoverUI hoverUI =
            button.GetComponent<RewardCardHoverUI>();

        if (hoverUI != null)
        {
            hoverUI.Finish(true);
        }

        if (index < cardButtonTexts.Length &&
            cardButtonTexts[index] != null)
        {
            cardButtonTexts[index].text = "Sold";
        }

        Debug.Log("Purchased: " + card.name);
    }
    public void BuyHealthPotion()
    {
        if (healthPotionPurchased) return;

        bool success = RunSystem.Instance.BuyHealthPotion(
            healthPotionPrice
        );

        if (!success)
        {
            Debug.Log("Not enough gold to buy Health Potion.");
            return;
        }

        healthPotionPurchased = true;

        if (healthButton != null)
        {
            healthButton.interactable = false;
        }

        Debug.Log("Purchased: Health Potion");
    }

    public void BuyManaPotion()
    {
        if (manaPotionPurchased) return;

        bool success = RunSystem.Instance.BuyManaPotion(
            manaPotionPrice
        );

        if (!success)
        {
            Debug.Log("Not enough gold to buy Mana Potion.");
            return;
        }

        manaPotionPurchased = true;

        if (manaButton != null)
        {
            manaButton.interactable = false;
        }

        Debug.Log("Purchased: Mana Potion");
    }
    private int GetCardPrice(CardData card)
    {
        if (card.Rarity == CardRarity.Common)
        {
            return commonPrice;
        }

        if (card.Rarity == CardRarity.Uncommon)
        {
            return uncommonPrice;
        }

        if (card.Rarity == CardRarity.Rare)
        {
            return rarePrice;
        }

        return 0;
    }

    public void LeaveShop()
    {
        RunSystem.Instance.CompleteMapPoint();

        gameObject.SetActive(false);

        mapUI.UpdateMapUI();
    }
}