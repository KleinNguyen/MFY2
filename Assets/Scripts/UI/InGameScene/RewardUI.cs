using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RewardUI : MonoBehaviour
{
    [SerializeField, Min(0)] private int goldReward = 20;
    [SerializeField] private List<CardData> rewardCards;

    [SerializeField] private TMP_Text goldRewardText;
    [SerializeField] private TMP_Text messageText;

    [Header("Reward Cards")]
    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardPrefab;
    [SerializeField, Range(1, 3)] private int choiceCount = 3;

    [Header("Buttons")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Button continueButton;

    private readonly List<CardData> currentChoices = new();
    private readonly List<Button> choiceButtons = new();

    private bool selectionFinished;

    public void Setup()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.HeroData == null)
        {
            Debug.LogError("No active run or hero was found.");
            return;
        }

        if (cardContent == null || cardPrefab == null || cardPrefab.LibraryTemplate == null || goldRewardText == null || messageText == null || skipButton == null || continueButton == null)
        {
            Debug.LogError("RewardUI is missing references. Check the Inspector.");
            return;
        }

        ClearCardViews();

        currentChoices.Clear();
        selectionFinished = false;

        goldRewardText.text = "Gold reward: " + goldReward;
        messageText.text = "Choose one card or skip.";

        continueButton.interactable = false;
        skipButton.interactable = true;

        if (run.RewardClaimed)
        {
            FinishSelection("Rewards already claimed.");
            return;
        }

        List<CardData> availableCards = new();

        if (rewardCards != null)
        {
            foreach (CardData card in rewardCards)
            {
                if (card == null) continue;
                if (card.Character != run.HeroData.Character) continue;
                if (card.Rarity == CardRarity.Basic) continue;
                if (availableCards.Contains(card)) continue;

                availableCards.Add(card);
            }
        }

        for (int i = 0; i < choiceCount; i++)
        {
            CardData card = CardRaritySelector.TakeRandomCard(availableCards);

            if (card == null) break;

            int choiceIndex = currentChoices.Count;

            currentChoices.Add(card);
            CreateRewardCard(card, choiceIndex);
        }

        if (currentChoices.Count == 0) messageText.text = "No reward cards available. Press Skip to claim the gold.";

        Canvas.ForceUpdateCanvases();
    }

    private void CreateRewardCard(CardData cardData, int choiceIndex)
    {
        LibraryCardUI cardUI = Instantiate(cardPrefab.LibraryTemplate, cardContent);

        cardUI.gameObject.SetActive(true);
        cardUI.Setup(cardData, false);

        Button button = cardUI.GetComponent<Button>();

        if (button == null) button = cardUI.gameObject.AddComponent<Button>();

        Graphic graphic = cardUI.GetComponent<Graphic>();

        if (graphic == null) graphic = cardUI.GetComponentInChildren<Image>(true);

        if (graphic != null)
        {
            graphic.raycastTarget = true;
            button.targetGraphic = graphic;
        }

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();

        if (canvasGroup == null) canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        button.transition = Selectable.Transition.None;
        button.interactable = true;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectCard(choiceIndex));

        choiceButtons.Add(button);
    }

    private void ClearCardViews()
    {
        choiceButtons.Clear();

        for (int i = cardContent.childCount - 1; i >= 0; i--)
        {
            GameObject oldCard = cardContent.GetChild(i).gameObject;

            oldCard.SetActive(false);
            Destroy(oldCard);
        }
    }

    public void SelectCard(int index)
    {
        RunSystem run = RunSystem.Instance;

        if (selectionFinished || run == null || run.RewardClaimed) return;
        if (index < 0 || index >= currentChoices.Count) return;

        CardData selectedCard = currentChoices[index];

        run.ClaimReward(goldReward, selectedCard);

        FinishSelection("Selected: " + selectedCard.name);
    }

    public void SkipCard()
    {
        RunSystem run = RunSystem.Instance;

        if (selectionFinished || run == null || run.RewardClaimed) return;

        run.ClaimReward(goldReward, null);

        FinishSelection("Card skipped. Gold received.");
    }

    private void FinishSelection(string message)
    {
        selectionFinished = true;
        messageText.text = message;

        foreach (Button button in choiceButtons)
        {
            if (button != null) button.interactable = false;
        }

        skipButton.interactable = false;
        continueButton.interactable = true;
    }

    public void ContinueToMap()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || !run.RewardClaimed) return;

        MapPoint selectedPoint = run.GetMapPoint(run.SelectedMapPointId);

        if (selectedPoint == null)
        {
            Debug.LogError("Selected map point is missing.");
            return;
        }

        bool defeatedBoss = selectedPoint.Type == MapPointType.Boss;

        run.CompleteStage();

        if (defeatedBoss)
        {
            bool movedToNextAct = run.MoveToNextAct();

            if (movedToNextAct)
            {
                SceneManager.LoadScene("MapScene");
                return;
            }

            Debug.Log("No next Act. Run complete.");
        }

        SceneManager.LoadScene("MapScene");
    }
}