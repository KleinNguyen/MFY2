using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationDialogUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton, noButton;

    private static ConfirmationDialogUI instance;
    private MonoBehaviour owner;
    private Action onConfirmed;
    private Func<bool> canConfirm;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;
    private bool IsConfigured =>
        panel != null &&
        panel != gameObject &&
        messageText != null &&
        yesButton != null &&
        noButton != null;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }

        instance = this;

        if (!IsConfigured)
        {
            Debug.LogError(
                "Assign the confirmation panel, text and buttons. Keep the controller outside the panel.",
                this
            );
            return;
        }

        yesButton.onClick.AddListener(Confirm);
        noButton.onClick.AddListener(Cancel);
        Cancel();
    }

    // Store the action until Yes is pressed.
    public static bool Ask(MonoBehaviour owner, string message, Action action, Func<bool> validate = null)
    {
        if (instance == null || !instance.isActiveAndEnabled || !instance.IsConfigured)
        {
            Debug.LogError("Add a configured, active ConfirmationCanvas to this scene.");
            return false;
        }

        if (IsOpen || owner == null || !owner.isActiveAndEnabled || action == null) return false;

        instance.owner = owner;
        instance.onConfirmed = action;
        instance.canConfirm = validate;
        instance.messageText.text = message;
        instance.isOpen = true;
        instance.panel.SetActive(true);
        instance.panel.transform.SetAsLastSibling();

        if (!instance.panel.activeInHierarchy)
        {
            instance.Cancel();
            Debug.LogError("The confirmation panel has an inactive parent.");
            return false;
        }

        if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
        if (ManualTargetSystem.Instance != null) ManualTargetSystem.Instance.CancelTargeting();

        instance.noButton.Select();
        return true;
    }

    private void Confirm()
    {
        if (!isOpen) return;

        Action action = onConfirmed;
        MonoBehaviour requestOwner = owner;
        Func<bool> validate = canConfirm;

        Cancel();

        if (requestOwner != null && requestOwner.isActiveAndEnabled && (validate == null || validate()))
            action?.Invoke();
    }

    private void Cancel()
    {
        isOpen = false;
        owner = null;
        onConfirmed = null;
        canConfirm = null;

        if (panel != null && panel != gameObject) panel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (isOpen && (owner == null || !owner.isActiveAndEnabled || !panel.activeInHierarchy))
            Cancel();
    }

    private void OnDisable() => Cancel();

    private void OnDestroy()
    {
        if (yesButton != null) yesButton.onClick.RemoveListener(Confirm);
        if (noButton != null) noButton.onClick.RemoveListener(Cancel);
        if (instance == this) instance = null;
    }
}