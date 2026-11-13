using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestUI : MonoBehaviour
{
    private const float DefaultRestDuration = 3f;

    [SerializeField] private MapUI mapUI;
    [SerializeField] private ShowDeckUI showDeckUI;

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0f)] private float restDuration = DefaultRestDuration;
    [SerializeField] private GameObject confirmationDialog;
    [SerializeField] private GameObject blackoutOverlay;
    [SerializeField] private Image blackoutImage;

    private bool hasRested;
    private bool hasUpgraded;
    private bool isResting;
    private Coroutine restCoroutine;

    public void Setup()
    {
        hasRested = false;
        hasUpgraded = false;
        isResting = false;

        if (confirmationDialog == null || blackoutOverlay == null || blackoutImage == null)
        {
            Debug.LogError("Rest confirmation and blackout UI objects must be assigned in RestUI.", this);
            return;
        }

        confirmationDialog.SetActive(false);
        blackoutOverlay.SetActive(false);

        UpdateRestUI();
    }

    private void UpdateRestUI()
    {
        RunSystem run = RunSystem.Instance;

        if (run == null || run.HeroData == null) return;

        if (healthText != null) healthText.text = "HP: " + run.CurrentHealth + " / " + run.HeroData.Health;
        if (messageText != null) messageText.text = "Rest and upgrade a card.";
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

        if (isResting) return;
        if (RunSystem.Instance == null || RunSystem.Instance.HeroData == null) return;

        if (confirmationDialog == null)
        {
            Debug.LogError("Rest confirmation dialog is not assigned in RestUI.", this);
            return;
        }

        confirmationDialog.SetActive(true);
        confirmationDialog.transform.SetAsLastSibling();
    }

    public void ConfirmRest()
    {
        if (hasRested || isResting) return;

        confirmationDialog.SetActive(false);
        restCoroutine = StartCoroutine(RestRoutine());
    }

    public void CancelRest()
    {
        if (confirmationDialog != null) confirmationDialog.SetActive(false);
    }

    private IEnumerator RestRoutine()
    {
        isResting = true;
        blackoutImage.color = Color.black;
        blackoutOverlay.SetActive(true);
        blackoutOverlay.transform.SetAsLastSibling();
        SfxManager.PlaySfx(SfxId.Rest);

        yield return new WaitForSecondsRealtime(restDuration);

        RunSystem run = RunSystem.Instance;
        if (run == null || run.HeroData == null)
        {
            Debug.LogError("Cannot complete rest: RunSystem or HeroData is missing.", this);
            blackoutOverlay.SetActive(false);
            isResting = false;
            restCoroutine = null;
            yield break;
        }

        int healAmount = Mathf.CeilToInt(run.HeroData.Health * 0.3f);
        run.Heal(healAmount);
        hasRested = true;

        blackoutOverlay.SetActive(false);
        isResting = false;
        restCoroutine = null;
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

    private void OnDisable()
    {
        if (restCoroutine != null)
        {
            StopCoroutine(restCoroutine);
            restCoroutine = null;
        }

        isResting = false;
        if (confirmationDialog != null) confirmationDialog.SetActive(false);
        if (blackoutOverlay != null) blackoutOverlay.SetActive(false);
    }
}