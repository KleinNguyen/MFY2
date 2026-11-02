using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class RewardUI : MonoBehaviour
{
    [SerializeField] private int goldReward = 20;
    [SerializeField] private List<CardData> rewardCards;
    [SerializeField] private TMP_Text goldRewardText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TMP_Text[] cardButtonTexts;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button continueButton;
    private readonly List<CardData> currentChoices = new();
    public void Setup()
    {
        currentChoices.Clear();
        goldRewardText.text = "Gold reward: " + goldReward;
        messageText.text = "Choose one card or skip";

        continueButton.interactable = false;
        skipButton.interactable = true;
        List<CardData> availableCards = new();

        foreach (CardData card in rewardCards)
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
            if(i<cardButtonTexts.Length && availableCards.Count > 0)
            {
                CardData card = CardRaritySelector.TakeRandomCard(availableCards);

                if (card == null)
                {
                    cardButtons[i].gameObject.SetActive(false);
                    continue;
                }

                currentChoices.Add(card);

                cardButtons[i].gameObject.SetActive(true);
                cardButtons[i].interactable = true;

                cardButtonTexts[i].text =
                    card.name + "\n" +
                    "Rarity: " + card.Rarity + "\n" +
                    "Mana: " + card.Mana + "\n" +
                    card.Description;
            } else
            {
                cardButtons[i].gameObject.SetActive(false);
            }
        }
    }
    public void SelectCard(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;
        CardData selectedCard = currentChoices[index];
        RunSystem.Instance.ClaimReward(goldReward, selectedCard);
        FinishSelection("Selected: " + selectedCard.name);
    }
    public void SkipCard()
    {
        RunSystem.Instance.ClaimReward(goldReward, null);
        FinishSelection("Card skipped");
    }
    private void FinishSelection(string message)
    {
        messageText.text = message;

        foreach (Button cardButton in cardButtons)
        {
            cardButton.interactable = false;
        }

        skipButton.interactable = false;
        continueButton.interactable = true;
    }
    public void ContinueToMap()
    {
        if (!RunSystem.Instance.RewardClaimed) return;

        RunSystem.Instance.CompleteStage();

        SceneManager.LoadScene("MapScene");
    }
}
