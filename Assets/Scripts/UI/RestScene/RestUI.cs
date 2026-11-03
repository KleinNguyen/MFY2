using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RestUI : MonoBehaviour
{
    private bool hasRested;
    private bool hasUpgraded;
    [SerializeField] private MapUI mapUI;

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text messageText;

    [SerializeField] private TMP_Dropdown cardDropdown;
    [SerializeField] private GameObject upgradePanel;

    private readonly List<CardData> upgradeableCards = new();

    public void Setup()
    {
        hasRested = false;
        hasUpgraded = false;

        upgradePanel.SetActive(false);

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
    public void OpenUpgradePanel()
    {
        SetupCardDropdown();
        upgradePanel.SetActive(true);
    }

    public void CloseUpgradePanel()
    {
        upgradePanel.SetActive(false);
    }

    public void Rest()
    {
        if (hasRested)
        {
            messageText.text = "You have already rested";
            return;
        }

        int maxHealth = RunSystem.Instance.HeroData.Health;
        int healAmount = Mathf.CeilToInt(maxHealth * 0.3f);

        RunSystem.Instance.Heal(healAmount);

        hasRested = true;

        UpdateRestUI();
        messageText.text = "Health restored";
    }

    public void UpgradeCard()
    {
        if (hasUpgraded)
        {
            messageText.text = "You have already upgraded a card";
            return;
        }

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

        hasUpgraded = true;

        messageText.text = "Upgraded: " + cardData.name;
    }

    public void LeaveRest()
    {
        RunSystem.Instance.CompleteMapPoint();

        gameObject.SetActive(false);

        mapUI.UpdateMapUI();
    }
}