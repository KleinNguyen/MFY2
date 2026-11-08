using TMPro;
using UnityEngine;

public class RestUI : MonoBehaviour
{
    [SerializeField] private MapUI mapUI;
    [SerializeField] private ShowDeckUI showDeckUI;

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text messageText;

    private bool hasRested;
    private bool hasUpgraded;

    public void Setup()
    {
        hasRested = false;
        hasUpgraded = false;

        UpdateRestUI();
    }

    private void UpdateRestUI()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.HeroData == null) return;

        if (healthText != null) healthText.text = "HP: " + run.CurrentHealth + " / " + run.HeroData.Health;
        if (messageText != null) messageText.text = "Choose to rest or upgrade a card.";
    }

    private void SetMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }

    public void OpenUpgradePanel()
    {
        if (hasUpgraded)
        {
            SetMessage("You have already upgraded a card.");
            return;
        }

        if (showDeckUI == null)
        {
            Debug.LogError("Show Deck UI is not assigned in RestUI.");
            return;
        }

        bool opened = showDeckUI.OpenUpgradeCard(OnCardUpgraded, CanUpgradeAtRest);

        if (!opened) SetMessage("Could not open the upgrade screen.");
    }

    private bool CanUpgradeAtRest() => isActiveAndEnabled && !hasUpgraded;

    private void OnCardUpgraded(CardData cardData)
    {
        hasUpgraded = true;

        UpdateRestUI();
        SetMessage("Upgraded: " + cardData.name + "+");
    }

    public void CloseUpgradePanel()
    {
        if (showDeckUI != null) showDeckUI.CloseDeck();
    }

    public void Rest()
    {
        if (hasRested)
        {
            SetMessage("You have already rested.");
            return;
        }

        RunSystem run = RunSystem.Instance;

        if (run == null || run.HeroData == null) return;

        int healAmount = Mathf.CeilToInt(run.HeroData.Health * 0.3f);

        run.Heal(healAmount);
        hasRested = true;

        UpdateRestUI();
        SetMessage("Health restored.");
    }

    public void LeaveRest()
    {
        if (showDeckUI != null) showDeckUI.CloseDeck();
        if (RunSystem.Instance == null) return;

        RunSystem.Instance.CompleteMapPoint();

        gameObject.SetActive(false);

        if (mapUI != null) mapUI.UpdateMapUI();
    }
}