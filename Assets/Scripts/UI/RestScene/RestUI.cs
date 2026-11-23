using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private MapUI mapUI;
    [SerializeField] private ShowDeckUI showDeckUI;
    [SerializeField] private TMP_Text healthText, messageText;

    [Header("Rest animation")]
    [SerializeField, Min(0f)] private float restDuration = 3f;
    [SerializeField] private GameObject blackoutOverlay;
    [SerializeField] private Image blackoutImage;

    private bool hasRested, hasUpgraded, isResting;
    private Coroutine restCoroutine;
    private TMP_Text sleepingText;

    private bool CanUseRest => isActiveAndEnabled && !isResting && RunSystem.Instance != null && !RunSystem.Instance.IsRunFinished;

    public void Setup()
    {
        hasRested = hasUpgraded = isResting = false;

        if (blackoutOverlay == null || blackoutImage == null)
        {
            Debug.LogError("Assign the rest blackout overlay and image.", this);
            return;
        }

        EnsureSleepingText();
        blackoutOverlay.SetActive(false);
        UpdateRestUI();
    }

    // Upgrade selection uses the shared deck confirmation.
    public void OpenUpgradePanel()
    {
        if (!CanUseRest || ConfirmationDialogUI.IsOpen) return;
        if (hasUpgraded) { SetMessage("You have already upgraded a card."); return; }
        if (showDeckUI == null) { Debug.LogError("Show Deck UI is not assigned in RestUI.", this); return; }

        if (!showDeckUI.OpenUpgradeCard(OnCardUpgraded, CanUpgradeAtRest))
            SetMessage("Could not open the upgrade screen.");
    }

    private bool CanUpgradeAtRest() => CanUseRest && !hasUpgraded;

    private void OnCardUpgraded(CardData card)
    {
        hasUpgraded = true;
        UpdateRestUI();
        SetMessage("Upgraded: " + card.name + "+");
    }

    public void CloseUpgradePanel()
    {
        if (!ConfirmationDialogUI.IsOpen && showDeckUI != null) showDeckUI.CloseDeck();
    }

    // Recover health only after confirmation.
    public void Rest()
    {
        if (!CanUseRest) return;
        if (hasRested) { SetMessage("You have already rested."); return; }

        RunSystem run = RunSystem.Instance;
        if (run.HeroData == null || blackoutOverlay == null || blackoutImage == null) return;

        int amount = Mathf.Max(0, Mathf.Min(Mathf.CeilToInt(run.HeroData.Health * 0.2f), run.HeroData.Health - run.CurrentHealth));

        ConfirmationDialogUI.Ask(this, $"Do you want to rest?\nRestore {amount} HP.", ExecuteRest,
            () => CanUseRest && !hasRested && RunSystem.Instance == run);
    }

    private void ExecuteRest()
    {
        if (!CanUseRest || hasRested) return;

        isResting = true;
        restCoroutine = StartCoroutine(RestRoutine());
    }

    private IEnumerator RestRoutine()
    {
        blackoutImage.color = Color.black;
        blackoutOverlay.SetActive(true);
        blackoutOverlay.transform.SetAsLastSibling();
        ShowSleepingText();
        SfxManager.PlaySfx(SfxId.Rest);

        float elapsed = 0f;
        while (elapsed < restDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (sleepingText != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 2f) * 0.04f;
                sleepingText.transform.localScale = Vector3.one * pulse;

                float phase = elapsed % 1.2f;
                sleepingText.text = phase < 0.3f ? "Z Z Z"
                    : phase < 0.6f ? "Z Z Z."
                    : phase < 0.9f ? "Z Z Z.."
                    : "Z Z Z...";
            }

            yield return null;
        }

        RunSystem run = RunSystem.Instance;

        if (run != null && run.HeroData != null && !run.IsRunFinished)
        {
            run.Heal(Mathf.CeilToInt(run.HeroData.Health * 0.2f));
            hasRested = true;
            UpdateRestUI();
            SetMessage("Health restored.");
        }

        HideSleepingText();
        blackoutOverlay.SetActive(false);
        isResting = false;
        restCoroutine = null;
    }

    private void EnsureSleepingText()
    {
        if (sleepingText != null) return;

        Transform existingText = blackoutOverlay.transform.Find("SleepingZText");
        if (existingText != null)
        {
            sleepingText = existingText.GetComponent<TMP_Text>();
        }

        if (sleepingText == null)
        {
            GameObject textObject = new GameObject(
                "SleepingZText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(blackoutOverlay.transform, false);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(900f, 200f);

            sleepingText = textObject.GetComponent<TextMeshProUGUI>();
            sleepingText.font = TMP_Settings.defaultFontAsset;
            if (sleepingText.font == null)
                sleepingText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        sleepingText.text = "Z Z Z...";
        sleepingText.fontSize = 120f;
        sleepingText.fontStyle = FontStyles.Bold;
        sleepingText.alignment = TextAlignmentOptions.Center;
        sleepingText.color = Color.white;
        sleepingText.raycastTarget = false;
        sleepingText.enableWordWrapping = false;
        sleepingText.gameObject.SetActive(false);
    }

    private void ShowSleepingText()
    {
        if (sleepingText == null) EnsureSleepingText();
        if (sleepingText == null) return;

        sleepingText.transform.localScale = Vector3.one;
        sleepingText.text = "Z Z Z...";
        sleepingText.gameObject.SetActive(true);
        sleepingText.transform.SetAsLastSibling();
    }

    private void HideSleepingText()
    {
        if (sleepingText != null)
        {
            sleepingText.transform.localScale = Vector3.one;
            sleepingText.gameObject.SetActive(false);
        }
    }

    // Mention unused actions before completing the map point.
    public void LeaveRest()
    {
        if (!CanUseRest) return;

        RunSystem run = RunSystem.Instance;
        string message = "Do you want to leave the rest site?";

        if (!hasRested && !hasUpgraded) message += "\nYou have not rested or upgraded a card.";
        else if (!hasRested) message += "\nYou have not rested yet.";
        else if (!hasUpgraded) message += "\nYou have not upgraded a card yet.";

        ConfirmationDialogUI.Ask(this, message, ExecuteLeaveRest,
            () => CanUseRest && RunSystem.Instance == run);
    }

    private void ExecuteLeaveRest()
    {
        if (!CanUseRest) return;

        if (showDeckUI != null) showDeckUI.CloseDeck();
        RunSystem.Instance.CompleteMapPoint();
        gameObject.SetActive(false);

        if (mapUI != null) mapUI.UpdateMapUI();
    }

    private void UpdateRestUI()
    {
        RunSystem run = RunSystem.Instance;
        if (run == null || run.HeroData == null) return;

        if (healthText != null) healthText.text = $"HP: {run.CurrentHealth} / {run.HeroData.Health}";
        SetMessage("Rest and upgrade a card.");
    }

    private void SetMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }

    private void OnDisable()
    {
        if (restCoroutine != null) StopCoroutine(restCoroutine);

        restCoroutine = null;
        isResting = false;

        HideSleepingText();
        if (blackoutOverlay != null) blackoutOverlay.SetActive(false);
    }
}