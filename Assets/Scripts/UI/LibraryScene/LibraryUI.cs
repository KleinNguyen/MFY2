using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LibraryUI : MonoBehaviour
{
    [Header("Card List")]

    [SerializeField] private List<CardData> allCards = new();

    [SerializeField] private Transform cardContent;

    [SerializeField] private LibraryCardUI cardPrefab;

    [SerializeField] private ScrollRect cardScrollView;

    [Header("Search")]

    [SerializeField] private TMP_InputField searchInput;

    [Header("Rarity")]

    [SerializeField] private Toggle basicToggle;

    [SerializeField] private Toggle commonToggle;

    [SerializeField] private Toggle uncommonToggle;

    [SerializeField] private Toggle rareToggle;

    [Header("Sort")]

    [SerializeField] private Toggle ascendingToggle;

    [SerializeField] private Toggle descendingToggle;

    [Header("Upgrade Preview")]

    [SerializeField] private Toggle viewUpgradeToggle;

    private readonly List<CardData> uniqueCards = new();

    // 0 = All, 1 = Red, 2 = Green, 3 = Yellow
    private int selectedCharacter = 0;

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        SetupCardList();

        ascendingToggle.SetIsOnWithoutNotify(true);
        descendingToggle.SetIsOnWithoutNotify(false);

        AddListeners();

        RefreshLibrary();
    }

    private bool HasRequiredReferences()
    {
        if (cardContent == null ||
            cardPrefab == null ||
            searchInput == null ||
            basicToggle == null ||
            commonToggle == null ||
            uncommonToggle == null ||
            rareToggle == null ||
            ascendingToggle == null ||
            descendingToggle == null ||
            viewUpgradeToggle == null)
        {
            Debug.LogError(
                "LibraryUI is missing references. " +
                "Check LibraryManager in the Inspector."
            );

            return false;
        }

        return true;
    }

    private void SetupCardList()
    {
        uniqueCards.Clear();

        HashSet<CardData> addedCards = new();

        foreach (CardData card in allCards)
        {
            if (card == null)
            {
                continue;
            }

            if (!addedCards.Add(card))
            {
                continue;
            }

            uniqueCards.Add(card);
        }
    }

    private void AddListeners()
    {
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        basicToggle.onValueChanged.AddListener(OnFilterChanged);
        commonToggle.onValueChanged.AddListener(OnFilterChanged);
        uncommonToggle.onValueChanged.AddListener(OnFilterChanged);
        rareToggle.onValueChanged.AddListener(OnFilterChanged);

        viewUpgradeToggle.onValueChanged.AddListener(OnFilterChanged);

        ascendingToggle.onValueChanged.AddListener(OnAscendingChanged);
        descendingToggle.onValueChanged.AddListener(OnDescendingChanged);
    }

    private void OnDestroy()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        }

        if (basicToggle != null)
        {
            basicToggle.onValueChanged.RemoveListener(OnFilterChanged);
        }

        if (commonToggle != null)
        {
            commonToggle.onValueChanged.RemoveListener(OnFilterChanged);
        }

        if (uncommonToggle != null)
        {
            uncommonToggle.onValueChanged.RemoveListener(OnFilterChanged);
        }

        if (rareToggle != null)
        {
            rareToggle.onValueChanged.RemoveListener(OnFilterChanged);
        }

        if (viewUpgradeToggle != null)
        {
            viewUpgradeToggle.onValueChanged.RemoveListener(
                OnFilterChanged
            );
        }

        if (ascendingToggle != null)
        {
            ascendingToggle.onValueChanged.RemoveListener(
                OnAscendingChanged
            );
        }

        if (descendingToggle != null)
        {
            descendingToggle.onValueChanged.RemoveListener(
                OnDescendingChanged
            );
        }
    }

    private void OnSearchChanged(string value)
    {
        RefreshLibrary();
    }

    private void OnFilterChanged(bool value)
    {
        RefreshLibrary();
    }

    private void OnAscendingChanged(bool isOn)
    {
        descendingToggle.SetIsOnWithoutNotify(!isOn);

        RefreshLibrary();
    }

    private void OnDescendingChanged(bool isOn)
    {
        ascendingToggle.SetIsOnWithoutNotify(!isOn);

        RefreshLibrary();
    }

    public void SelectCharacter(int characterIndex)
    {
        if (characterIndex < 1 || characterIndex > 3)
        {
            Debug.LogWarning(
                "Invalid character filter: " + characterIndex
            );

            return;
        }

        if (selectedCharacter == characterIndex)
        {
            selectedCharacter = 0;
        }
        else
        {
            selectedCharacter = characterIndex;
        }

        RefreshLibrary();
    }

    public void RefreshLibrary()
    {
        List<CardData> filteredCards = new();

        foreach (CardData card in uniqueCards)
        {
            if (!MatchesSearch(card))
            {
                continue;
            }

            if (!MatchesCharacter(card))
            {
                continue;
            }

            if (!MatchesRarity(card))
            {
                continue;
            }

            filteredCards.Add(card);
        }

        SortCards(filteredCards);
        DisplayCards(filteredCards);
    }

    private bool MatchesSearch(CardData card)
    {
        string search = searchInput.text.Trim();

        if (string.IsNullOrEmpty(search))
        {
            return true;
        }

        return card.name.IndexOf(
            search,
            StringComparison.OrdinalIgnoreCase
        ) >= 0;
    }

    private bool MatchesCharacter(CardData card)
    {
        switch (selectedCharacter)
        {
            case 1:
                return card.Character == CharacterType.Red;

            case 2:
                return card.Character == CharacterType.Green;

            case 3:
                return card.Character == CharacterType.Yellow;

            default:
                return true;
        }
    }

    private bool MatchesRarity(CardData card)
    {
        bool anySelected =
            basicToggle.isOn ||
            commonToggle.isOn ||
            uncommonToggle.isOn ||
            rareToggle.isOn;

        if (!anySelected)
        {
            return true;
        }

        if (basicToggle.isOn &&
            card.Rarity == CardRarity.Basic)
        {
            return true;
        }

        if (commonToggle.isOn &&
            card.Rarity == CardRarity.Common)
        {
            return true;
        }

        if (uncommonToggle.isOn &&
            card.Rarity == CardRarity.Uncommon)
        {
            return true;
        }

        if (rareToggle.isOn &&
            card.Rarity == CardRarity.Rare)
        {
            return true;
        }

        return false;
    }

    private void SortCards(List<CardData> cards)
    {
        cards.Sort((a, b) =>
        {
            if (ascendingToggle.isOn)
            {
                return string.Compare(
                    a.name,
                    b.name,
                    StringComparison.OrdinalIgnoreCase
                );
            }

            return string.Compare(
                b.name,
                a.name,
                StringComparison.OrdinalIgnoreCase
            );
        });
    }

    private void DisplayCards(List<CardData> cards)
    {
        for (int i = cardContent.childCount - 1; i >= 0; i--)
        {
            GameObject oldCard = cardContent.GetChild(i).gameObject;

            oldCard.SetActive(false);
            Destroy(oldCard);
        }

        foreach (CardData card in cards)
        {
            LibraryCardUI cardUI = Instantiate(
                cardPrefab,
                cardContent
            );

            cardUI.Setup(
                card,
                viewUpgradeToggle.isOn
            );
        }

        Canvas.ForceUpdateCanvases();

        if (cardScrollView != null)
        {
            cardScrollView.verticalNormalizedPosition = 1f;
        }
    }
}