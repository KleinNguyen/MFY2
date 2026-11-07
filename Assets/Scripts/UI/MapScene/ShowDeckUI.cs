using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowDeckUI : MonoBehaviour
{
    [SerializeField] private GameObject deckPanel;
    //[SerializeField] private TMP_Text titleText;
    //[SerializeField] private TMP_Text messageText;

    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [SerializeField] private GameObject removeControls;
    [SerializeField] private Button confirmRemoveButton;
    [SerializeField] private TMP_Text confirmRemoveText;
    [SerializeField, Min(0)] private int removePrice = 75;

    private bool removeMode;
    private int selectedCardIndex = -1;
    private CardData selectedCard;

    private void Start()
    {
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }
    }

    public void OpenDeck()
    {
        if (!CanOpenPanel()) return;

        removeMode = false;
        selectedCardIndex = -1;
        selectedCard = null;

        deckPanel.SetActive(true);

        //titleText.text = "Your Deck";
        //messageText.text = "";

        removeControls.SetActive(false);

        RefreshCards();
    }

    public void OpenRemoveCard()
    {
        if (!CanOpenPanel()) return;

        removeMode = true;
        selectedCardIndex = -1;
        selectedCard = null;

        deckPanel.SetActive(true);

        //titleText.text = "Remove a Card";
        //messageText.text = "Choose a card to remove.";

        removeControls.SetActive(true);
        confirmRemoveButton.interactable = false;
        confirmRemoveText.text = "Remove: " + removePrice + " Gold";

        RefreshCards();
    }

    public void CloseDeck()
    {
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }
    }

    private bool CanOpenPanel()
    {
        if (RunSystem.Instance == null || RunSystem.Instance.Deck == null)
        {
            Debug.LogWarning("No active run or deck was found.");
            return false;
        }

        if (deckPanel == null ||
            //titleText == null ||
            //messageText == null ||
            cardContent == null ||
            cardPrefab == null ||
            removeControls == null ||
            confirmRemoveButton == null ||
            confirmRemoveText == null)
        {
            Debug.LogError(
                "RunDeckUI is missing references. Check the Inspector."
            );

            return false;
        }

        return true;
    }

    private void RefreshCards()
    {
        if (cardPrefab == null || cardPrefab.LibraryTemplate == null)
        {
            Debug.LogError(
                "CardView prefab or its Library Template is missing."
            );

            return;
        }
        for (int i = cardContent.childCount - 1; i >= 0; i--)
        {
            GameObject oldCard = cardContent.GetChild(i).gameObject;
            oldCard.SetActive(false);
            Destroy(oldCard);
        }

        var deck = RunSystem.Instance.Deck;

        //if (deck.Count == 0)
        //{
        //    messageText.text = "Your deck is empty.";
        //    return;
        //}

        for (int i = 0; i < deck.Count; i++)
        {
            CardData cardData = deck[i];

            if (cardData == null)
            {
                continue;
            }

            int cardIndex = i;

            LibraryCardUI cardUI = Instantiate(
                cardPrefab.LibraryTemplate,
                cardContent
            );

            cardUI.gameObject.SetActive(true);

            bool isUpgraded = RunSystem.Instance.IsCardUpgraded(cardData);

            cardUI.Setup(cardData, isUpgraded);

            if (removeMode)
            {
                Button cardButton = cardUI.GetComponent<Button>();

                if (cardButton == null)
                {
                    cardButton = cardUI.gameObject.AddComponent<Button>();
                }

                cardButton.onClick.AddListener(
                    () => SelectCard(cardIndex, cardData)
                );
            }
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void SelectCard(int cardIndex, CardData cardData)
    {
        if (!removeMode)
        {
            return;
        }

        selectedCardIndex = cardIndex;
        selectedCard = cardData;

        //messageText.text =
        //    "Selected: " + cardData.name +
        //    "\nRemove for " + removePrice + " Gold?";

        confirmRemoveButton.interactable = true;
    }


    public void ConfirmRemoveCard()
    {
        if (!removeMode || selectedCardIndex < 0)
        {
            return;
        }

        RunSystem run = RunSystem.Instance;

        //if (run == null || run.Deck == null)
        //{
        //    messageText.text = "No active deck was found.";
        //    return;
        //}

        if (selectedCardIndex >= run.Deck.Count ||
            run.Deck[selectedCardIndex] != selectedCard)
        {
            selectedCardIndex = -1;
            selectedCard = null;
            confirmRemoveButton.interactable = false;

            //messageText.text = "Deck changed. Please select a card again.";
            RefreshCards();
            return;
        }

        //if (run.Gold < removePrice)
        //{
        //    messageText.text =
        //        "Not enough gold. You need " +
        //        removePrice + " Gold.";

        //    return;
        //}

        string removedCardName = selectedCard.name;

        bool success = run.RemoveCardFromDeck(
            selectedCardIndex,
            removePrice
        );

        //if (!success)
        //{
        //    messageText.text = "Could not remove this card.";
        //    return;
        //}

        selectedCardIndex = -1;
        selectedCard = null;
        confirmRemoveButton.interactable = false;

        //messageText.text =
        //    "Removed: " + removedCardName +
        //    "\nPaid: " + removePrice + " Gold.";

        RefreshCards();
    }
}