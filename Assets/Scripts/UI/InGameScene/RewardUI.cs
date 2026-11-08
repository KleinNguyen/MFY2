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


    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardPrefab;
    [SerializeField, Range(1, 3)] private int choiceCount = 3;


    [SerializeField] private Button skipButton;
    [SerializeField] private Button continueButton;

    private readonly List<CardData> currentChoices = new();
    private readonly List<RewardCardHoverUI> choiceViews = new();
    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 35f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

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
        GameObject slot = new("RewardSlot_" + choiceIndex, typeof(RectTransform), typeof(Image), typeof(Button), typeof(RewardCardHoverUI));

        slot.layer = cardContent.gameObject.layer;
        slot.transform.SetParent(cardContent, false);

        Image slotImage = slot.GetComponent<Image>();
        slotImage.color = new Color(1f, 1f, 1f, 0f);
        slotImage.raycastTarget = true;

        Button button = slot.GetComponent<Button>();
        button.targetGraphic = slotImage;
        button.transition = Selectable.Transition.None;
        button.interactable = true;

        LibraryCardUI cardUI = Instantiate(cardPrefab.LibraryTemplate, slot.transform);

        cardUI.gameObject.SetActive(true);
        cardUI.Setup(cardData, false);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();

        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        cardRect.localScale = Vector3.one;
        cardRect.localRotation = Quaternion.identity;

        CanvasGroup canvasGroup = cardUI.GetComponent<CanvasGroup>();

        if (canvasGroup == null) canvasGroup = cardUI.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RewardCardHoverUI hoverUI = slot.GetComponent<RewardCardHoverUI>();
        hoverUI.Initialize(cardRect, hoverScale, hoverLift, hoverDuration);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectCard(choiceIndex));

        choiceViews.Add(hoverUI);
    }

    private void ClearCardViews()
    {
        choiceViews.Clear();

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

        FinishSelection("Received: " + selectedCard.name, index);
    }

    public void SkipCard()
    {
        RunSystem run = RunSystem.Instance;

        if (selectionFinished || run == null || run.RewardClaimed) return;

        run.ClaimReward(goldReward, null);

        FinishSelection("Card skipped. Gold received.");
    }

    private void FinishSelection(string message, int selectedIndex = -1)
    {
        selectionFinished = true;
        messageText.text = message;

        for (int i = 0; i < choiceViews.Count; i++)
        {
            if (choiceViews[i] == null) continue;

            choiceViews[i].Finish(i == selectedIndex);
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