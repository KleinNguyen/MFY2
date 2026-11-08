using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform cardContent;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private ScrollRect scrollRect;

    public bool IsOpen { get; private set; }

    private Action<Card> onCardSelected;

    private void Awake()
    {
        Close();
    }

    private void OnDisable()
    {
        Close();
    }

    public bool Open(List<Card> cards, string message, Action<Card> onSelected)
    {
        if (IsOpen)
        {
            Debug.LogWarning("A card selection is already open.");
            return false;
        }

        if (panel == null || cardContent == null || cardViewPrefab == null || cardViewPrefab.LibraryTemplate == null)
        {
            Debug.LogError("CardSelectionUI is missing references in the Inspector.");
            return false;
        }

        if (cards == null || cards.Count == 0)
        {
            return false;
        }

        ClearCards();

        onCardSelected = onSelected;
        IsOpen = true;

        panel.SetActive(true);

        if (titleText != null)
        {
            titleText.text = message;
        }

        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            Card selectedCard = card;

            LibraryCardUI cardUI = Instantiate(cardViewPrefab.LibraryTemplate, cardContent);
            cardUI.gameObject.SetActive(true);
            cardUI.Setup(selectedCard);

            Button button = cardUI.GetComponent<Button>();

            if (button == null)
            {
                button = cardUI.gameObject.AddComponent<Button>();
            }

            Graphic graphic = cardUI.GetComponent<Graphic>();

            if (graphic == null)
            {
                graphic = cardUI.GetComponentInChildren<Image>();
            }

            if (graphic != null)
            {
                graphic.raycastTarget = true;
                button.targetGraphic = graphic;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectCard(selectedCard));
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        return true;
    }

    private void SelectCard(Card card)
    {
        if (!IsOpen)
        {
            return;
        }

        Action<Card> callback = onCardSelected;

        Close();

        callback?.Invoke(card);
    }

    public void Close()
    {
        IsOpen = false;
        onCardSelected = null;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void ClearCards()
    {
        for (int i = cardContent.childCount - 1; i >= 0; i--)
        {
            GameObject oldCard = cardContent.GetChild(i).gameObject;

            oldCard.SetActive(false);
            Destroy(oldCard);
        }
    }
}