using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RestUI : MonoBehaviour
{
    [SerializeField] private MapUI mapUI;

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Dropdown cardDropdown;

    private readonly List<CardData> upgradeableCards = new();

    public void Setup()
    {
        UpdateRestUI();
        SetupCardDropdown();
    }

    private void UpdateRestUI()
    {
        healthText.text =
            "HP: " +
            RunSystem.Instance.CurrentHealth +
            " / " +
            RunSystem.Instance.HeroData.Health;

        messageText.text = "Choose to rest or upgrade a card type";
    }

    private void SetupCardDropdown()
    {
        upgradeableCards.Clear();
        cardDropdown.ClearOptions();

        List<string> options = new();

        foreach (CardData cardData in RunSystem.Instance.Deck)
        {
            if (cardData == null) continue;
            if (!cardData.CanUpgrade) continue;
            if (RunSystem.Instance.IsCardUpgraded(cardData)) continue;
            if (upgradeableCards.Contains(cardData)) continue;

            upgradeableCards.Add(cardData);
            options.Add(cardData.name + " → " + cardData.name + "+");
        }

        if (options.Count == 0)
        {
            options.Add("No cards available to upgrade");
        }

        cardDropdown.AddOptions(options);
        cardDropdown.value = 0;
        cardDropdown.RefreshShownValue();
    }

    public void Rest()
    {
        int maxHealth = RunSystem.Instance.HeroData.Health;
        int healAmount = Mathf.CeilToInt(maxHealth * 0.3f);

        RunSystem.Instance.Heal(healAmount);

        FinishRest();
    }

    public void UpgradeCard()
    {
        if (upgradeableCards.Count == 0)
        {
            messageText.text = "No cards available to upgrade";
            return;
        }

        int selectedIndex = cardDropdown.value;

        if (selectedIndex < 0 || selectedIndex >= upgradeableCards.Count) return;

        CardData cardData = upgradeableCards[selectedIndex];

        bool success = RunSystem.Instance.UpgradeCard(cardData);

        if (!success)
        {
            messageText.text = "Could not upgrade this card";
            return;
        }

        FinishRest();
    }

    private void FinishRest()
    {
        RunSystem.Instance.CompleteMapPoint();

        gameObject.SetActive(false);

        mapUI.UpdateMapUI();
    }
}