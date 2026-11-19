using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RewardUI : MonoBehaviour
{
    private const int WheelOutcomeCount = 3;
    private static readonly string[] RewardWheelLabels = { "X2 GOLD", "RARITY UP", "LOSE ALL", "X2 GOLD", "RARITY UP", "LOSE ALL" };

    [Header("Gold rewards")]
    [SerializeField, Min(0)] private int normalGoldReward = 25;
    [SerializeField, Min(0)] private int eliteGoldReward = 45;
    [SerializeField, Min(0)] private int bossGoldReward = 75;

    [Header("Reward cards")]
    [SerializeField] private List<CardData> rewardCards;
    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardPrefab;
    [SerializeField, Range(1, 3)] private int choiceCount = 3;

    [Header("Reward interface")]
    [SerializeField] private TMP_Text goldRewardText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private RunResultUI runResultUI;

    [Header("Lucky wheel")]
    [SerializeField] private LuckyWheelUI luckyWheel;
    [SerializeField] private Button wheelButton;

    [Header("Card hover")]
    [SerializeField, Min(1f)] private float hoverScale = 1.12f;
    [SerializeField, Min(0f)] private float hoverLift = 35f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.12f;

    private readonly List<CardData> currentChoices = new();
    private readonly List<RewardCardHoverUI> choiceViews = new();
    private CardexCardFlightEffect cardFlightEffect;
    private bool selectionFinished;
    private bool wheelOpen;
    private bool cardFlightAnimating;

    // Unity
    private void Awake()
    {
        if (wheelButton != null) wheelButton.onClick.AddListener(OpenRewardWheel);
        cardFlightEffect = GetComponent<CardexCardFlightEffect>();
        if (cardFlightEffect == null) cardFlightEffect = gameObject.AddComponent<CardexCardFlightEffect>();
    }

    private void OnDestroy()
    {
        if (wheelButton != null) wheelButton.onClick.RemoveListener(OpenRewardWheel);
    }

    private void OnDisable() => cardFlightAnimating = false;

    // Reward setup
    public void Setup()
    {
        if (wheelOpen) return;

        RunSystem run = RunSystem.Instance;
        if (!ValidateSetup(run)) return;

        selectionFinished = false;
        PrepareRewardChoices(run);
        RefreshRewardView(run);

        if (run.RewardClaimed)
        {
            bool lostRewards = run.RewardWheelResult >= 0 && run.RewardWheelResult % WheelOutcomeCount == 2;
            FinishSelection(lostRewards ? "All rewards lost. Press Continue." : "Rewards already claimed.");
            return;
        }

        messageText.text = run.RewardWheelResult >= 0 ? "Wheel already used. Choose one card or skip." : "Spin the wheel, choose one card or skip.";
        if (currentChoices.Count == 0) messageText.text = "No reward cards available. Press Skip to claim the gold.";
        UpdateButtons();
    }

    private bool ValidateSetup(RunSystem run)
    {
        if (run == null || run.HeroData == null || run.CurrentEncounter == null)
        {
            Debug.LogError("An active run, hero and encounter are required for rewards.", this);
            return false;
        }

        if (run.IsRunFinished) return false;

        if (cardContent == null || cardPrefab == null || cardPrefab.LibraryTemplate == null || goldRewardText == null || messageText == null || skipButton == null || continueButton == null || luckyWheel == null || !luckyWheel.IsConfigured || wheelButton == null)
        {
            Debug.LogError("RewardUI is missing references. Check the Inspector.", this);
            return false;
        }

        return true;
    }

    private void PrepareRewardChoices(RunSystem run)
    {
        if (run.RewardsPrepared || run.RewardClaimed) return;

        EncounterType type = run.CurrentEncounter.Type;
        List<CardData> availableCards = GetAvailableRewardCards(run.HeroData.Character);
        List<CardData> choices = new();

        for (int i = 0; i < Mathf.Clamp(choiceCount, 1, 3); i++)
        {
            CardData card = CardRaritySelector.TakeRewardCard(availableCards, type);
            if (card == null) break;
            choices.Add(card);
        }

        run.PrepareRewards(GetRewardGold(type), choices);
    }

    private List<CardData> GetAvailableRewardCards(CharacterType character)
    {
        List<CardData> availableCards = new();
        if (rewardCards == null) return availableCards;

        foreach (CardData card in rewardCards)
        {
            if (card == null || card.Character != character || card.Rarity == CardRarity.Basic || availableCards.Contains(card)) continue;
            availableCards.Add(card);
        }

        return availableCards;
    }

    private int GetRewardGold(EncounterType type)
    {
        return type switch
        {
            EncounterType.Elite => eliteGoldReward,
            EncounterType.Boss => bossGoldReward,
            _ => normalGoldReward
        };
    }

    // Claiming rewards
    public void SelectCard(int index)
    {
        RunSystem run = RunSystem.Instance;
        if (!CanClaimRewards(run) || index < 0 || index >= currentChoices.Count) return;

        CardData selectedCard = currentChoices[index];
        if (selectedCard == null) return;

        run.ClaimReward(run.PendingRewardGold, selectedCard);
        if (run.RewardClaimed)
        {
            if (!TryStartCardFlight(index))
            {
                FinishSelection("Received: " + selectedCard.name, index);
            }
        }
        else messageText.text = "Could not claim this reward.";
    }

    public void SkipCard()
    {
        RunSystem run = RunSystem.Instance;
        if (!CanClaimRewards(run)) return;

        run.ClaimReward(run.PendingRewardGold, null);
        if (run.RewardClaimed) FinishSelection("Card skipped. Gold received.");
        else messageText.text = "Could not claim this reward.";
    }

    private bool CanClaimRewards(RunSystem run)
    {
        return run != null && !run.IsRunFinished && run.RewardsPrepared && !run.RewardClaimed && !selectionFinished && !wheelOpen;
    }

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

    // Lucky wheel
    public void OpenRewardWheel()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.IsRunFinished || !run.RewardsPrepared || run.RewardClaimed ||
            selectionFinished || wheelOpen || run.RewardWheelResult >= 0) return;

        if (luckyWheel == null || !luckyWheel.IsConfigured) return;

        wheelOpen = true;
        UpdateButtons();

        if (!luckyWheel.Show(RewardWheelLabels, ApplyWheelResult, OnWheelClosed,
            canCancel: true, closeCaption: "Back to rewards", character: run.HeroData.Character))
        {
            OnWheelClosed();
        }
    }

    private void ApplyWheelResult(int result)
    {
        RunSystem run = RunSystem.Instance;
        if (run == null) return;

        int outcome = result % WheelOutcomeCount;
        int cardIndex = -1;
        CardData replacement = null;

        if (outcome == 1) TryFindRarityReplacement(out cardIndex, out replacement);
        if (!run.ApplyRewardWheel(result, cardIndex, replacement)) return;

        bool cardChanged = replacement != null && cardIndex >= 0 &&
            cardIndex < run.RewardChoices.Count && run.RewardChoices[cardIndex] == replacement;

        RefreshRewardView(run);

        string message = outcome switch
        {
            0 => "Gold reward doubled!",
            1 => cardChanged ? "Higher rarity reward: " + replacement.name : "No higher rarity card available. Rewards unchanged.",
            _ => "All rewards lost. Press Continue."
        };

        if (outcome == 2)
        {
            FinishSelection(message);
        }
        else
        {
            messageText.text = message;
            UpdateButtons();
        }
    }

    private void OnWheelClosed()
    {
        wheelOpen = false;
        UpdateButtons();
    }

    // Rarity replacement
    private bool TryFindRarityReplacement(out int cardIndex, out CardData replacement)
    {
        cardIndex = -1;
        replacement = null;
        List<int> eligibleIndices = new();

        for (int i = 0; i < currentChoices.Count; i++)
        {
            if (GetHigherRarityCards(currentChoices[i]).Count > 0) eligibleIndices.Add(i);
        }

        if (eligibleIndices.Count == 0) return false;
        cardIndex = eligibleIndices[Random.Range(0, eligibleIndices.Count)];
        List<CardData> candidates = GetHigherRarityCards(currentChoices[cardIndex]);
        replacement = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    private List<CardData> GetHigherRarityCards(CardData original)
    {
        List<CardData> candidates = new();
        if (original == null || rewardCards == null) return candidates;

        foreach (CardData card in rewardCards)
        {
            if (card == null || card.Character != original.Character || currentChoices.Contains(card) || candidates.Contains(card)) continue;
            if (IsHigherRarity(original.Rarity, card.Rarity)) candidates.Add(card);
        }

        return candidates;
    }

    private static bool IsHigherRarity(CardRarity original, CardRarity replacement)
    {
        return original switch
        {
            CardRarity.Common => replacement == CardRarity.Uncommon || replacement == CardRarity.Rare,
            CardRarity.Uncommon => replacement == CardRarity.Rare,
            _ => false
        };
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
        if (continueButton != null) continueButton.interactable = run != null && !run.IsRunFinished && run.RewardClaimed && !wheelOpen && !cardFlightAnimating;

        foreach (RewardCardHoverUI view in choiceViews)
        {
            if (view != null) view.GetComponent<Button>().interactable = canChoose;
        }
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

    // Stage transition
    public void ContinueToMap()
    {
        RunSystem run = RunSystem.Instance;
        if (wheelOpen || cardFlightAnimating || run == null || run.IsRunFinished || !run.RewardClaimed) return;

        MapPoint selectedPoint = run.GetMapPoint(run.SelectedMapPointId);
        if (selectedPoint == null)
        {
            Debug.LogError("Selected map point is missing.");
            return;
        }

        bool defeatedBoss = selectedPoint.Type == MapPointType.Boss;
        bool finishedRun = defeatedBoss && run.IsLastAct;

        if (finishedRun && runResultUI == null)
        {
            Debug.LogError("Assign Run Result UI in RewardUI.");
            return;
        }

        if (defeatedBoss && !finishedRun && !run.CanMoveToNextAct)
        {
            Debug.LogError("The next Act is missing in RunSystem.");
            return;
        }

        run.CompleteStage();

        if (finishedRun)
        {
            run.FinishRun(RunResult.Won);
            gameObject.SetActive(false);
            runResultUI.Show();
            return;
        }

        if (defeatedBoss && !run.MoveToNextAct())
        {
            Debug.LogError("Could not advance to the next Act.");
            return;
        }

        SceneManager.LoadScene("MapScene");
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Fill Reward Cards")]
    private void AutoFillRewardCards()
    {
        if (Application.isPlaying) return;

        List<CardData> cards = new();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Data/Cards" });

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null) cards.Add(card);
        }

        cards.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        UnityEditor.Undo.RecordObject(this, "Auto Fill Reward Cards");
        rewardCards = cards;

        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);

        UnityEditor.EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log($"Reward Cards loaded: {rewardCards.Count}", this);
    }
#endif
}