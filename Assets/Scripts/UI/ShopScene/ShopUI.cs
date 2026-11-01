using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private MapUI mapUI;

    [SerializeField] private List<CardData> shopCards;
    [SerializeField] private int cardPrice = 50;

    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text messageText;

    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardButtonTexts;

    private readonly List<CardData> currentCards = new();
    private readonly List<int> currentPrices = new();

    public void Setup()
    {
        currentCards.Clear();
        currentPrices.Clear();

        goldText.text = "Gold: " + RunSystem.Instance.Gold;
        messageText.text = "Choose a card to buy";

        List<CardData> availableCards = new(shopCards);

        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (i >= cardButtonTexts.Length || availableCards.Count == 0)
            {
                cardButtons[i].gameObject.SetActive(false);
                continue;
            }

            int randomIndex = Random.Range(0, availableCards.Count);

            CardData card = availableCards[randomIndex];
            availableCards.RemoveAt(randomIndex);

            int price = cardPrice;

            if (i == 0)
            {
                price = Mathf.RoundToInt(cardPrice * 0.8f);
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
                "Mana: " + card.Mana + "\n" +
                card.Description + "\n" +
                "Price: " + price + " Gold";
        }
    }

    public void BuyCard(int index)
    {
        if (index < 0 || index >= currentCards.Count) return;

        CardData card = currentCards[index];
        int price = currentPrices[index];

        bool success = RunSystem.Instance.BuyCard(card, price);

        if (!success)
        {
            messageText.text = "Not enough gold";
            return;
        }

        cardButtons[index].interactable = false;

        goldText.text = "Gold: " + RunSystem.Instance.Gold;
        messageText.text = "Purchased: " + card.name;
    }

    public void LeaveShop()
    {
        RunSystem.Instance.CompleteMapPoint();

        gameObject.SetActive(false);

        mapUI.UpdateMapUI();
    }
}