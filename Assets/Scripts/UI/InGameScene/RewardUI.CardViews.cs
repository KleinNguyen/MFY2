using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public partial class RewardUI
{
    private bool TryStartCardFlight(int choiceIndex)
    {
        LibraryCardUI card = choiceIndex >= 0 && choiceIndex < choiceViews.Count && choiceViews[choiceIndex] != null
            ? choiceViews[choiceIndex].GetComponentInChildren<LibraryCardUI>()
            : null;
        RectTransform cardRect = card != null ? card.GetComponent<RectTransform>() : null;

        if (cardFlightEffect == null || cardRect == null)
        {
            Debug.LogError("Cannot animate the reward card: the selected card is missing.", this);
            return false;
        }

        cardFlightAnimating = true;
        FinishSelection("Received: " + currentChoices[choiceIndex].name, choiceIndex);
        if (!cardFlightEffect.TryFly(cardRect, () =>
        {
            cardFlightAnimating = false;
            UpdateButtons();
        }))
        {
            cardFlightAnimating = false;
            UpdateButtons();
            return false;
        }

        UpdateButtons();
        return true;
    }

    private void FinishSelection(string message, int selectedIndex = -1)
    {
        selectionFinished = true;
        messageText.text = message;

        for (int i = 0; i < choiceViews.Count; i++)
        {
            if (choiceViews[i] != null) choiceViews[i].Finish(i == selectedIndex);
        }

        UpdateButtons();
    }

    // Interface
    private void RefreshRewardView(RunSystem run)
    {
        ClearCardViews();
        currentChoices.Clear();
        currentChoices.AddRange(run.RewardChoices);
        goldRewardText.text = "Gold reward: " + run.PendingRewardGold;

        for (int i = 0; i < currentChoices.Count; i++) CreateRewardCard(currentChoices[i], i);
        Canvas.ForceUpdateCanvases();
    }

    private void UpdateButtons()
    {
        RunSystem run = RunSystem.Instance;
        bool canChoose = CanClaimRewards(run);
        if (skipButton != null) skipButton.interactable = canChoose;
        if (wheelButton != null) wheelButton.interactable = canChoose && run.RewardWheelResult < 0;
        if (continueButton != null)
        {
            continueButton.interactable = run != null &&
                !run.IsRunFinished &&
                run.RewardClaimed &&
                !wheelOpen &&
                !cardFlightAnimating;
        }

        foreach (RewardCardHoverUI view in choiceViews)
        {
            if (view != null) view.GetComponent<Button>().interactable = canChoose;
        }
    }

    private void CreateRewardCard(CardData cardData, int choiceIndex)
    {
        GameObject slot = new(
            "RewardSlot_" + choiceIndex,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(RewardCardHoverUI)
        );
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
        cardUI.Setup(cardData, RunSystem.Instance != null && RunSystem.Instance.IsCardUpgraded(cardData));

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        cardUI.SetLayoutScale(Vector3.one);
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
}
