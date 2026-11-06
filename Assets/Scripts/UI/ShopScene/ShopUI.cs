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
        shopInventoryPanel.SetActive(false);
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

        foreach (CardData card in shopCards)
        {
            if (card == null) continue;

            if (card.Character != RunSystem.Instance.HeroData.Character)
            {
                continue;
            }

            if (card.Rarity == CardRarity.Basic)
            {
                continue;
            }

            if (!availableCards.Contains(card))
            {
                availableCards.Add(card);
            }
        }

        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (i >= cardButtonTexts.Length || availableCards.Count == 0)
            {
                cardButtons[i].gameObject.SetActive(false);
                continue;
            }

            CardData card = CardRaritySelector.TakeRandomCard(availableCards);

            if (card == null)
            {
                cardButtons[i].gameObject.SetActive(false);
                continue;
            }

            int price = GetCardPrice(card);

            if (i == 0)
            {
                price = Mathf.RoundToInt(price * 0.8f);
            }

            currentCards.Add(card);
            currentPrices.Add(price);

            cardButtons[i].gameObject.SetActive(true);
            cardButtons[i].interactable = true;

            string discountText = "";

            if (i == 0)
            {
                discountText = "20% OFF\n";
            }

            cardButtonTexts[i].text =
                discountText +
                card.name + "\n" +
                "Rarity: " + card.Rarity + "\n" +
                "Mana: " + card.Mana + "\n" +
                card.Description + "\n" +
                "Price: " + price + " Gold";
        }
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
        if (index < 0 || index >= currentCards.Count)
        {
            return;
        }

        if (!cardButtons[index].interactable)
        {
            return;
        }

        CardData card = currentCards[index];
        int price = currentPrices[index];

        bool success = RunSystem.Instance.BuyCard(card, price);

        if (!success)
        {
            Debug.Log("Not enough gold to buy " + card.name);
            return;
        }

        cardButtons[index].interactable = false;

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