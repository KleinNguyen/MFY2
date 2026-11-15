using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    private const float StandardCardWidth = 260f;
    private const float StandardCardHeight = 340f;

    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text mana;
    [SerializeField] private LibraryCardUI libraryTemplate;

    public LibraryCardUI LibraryTemplate => libraryTemplate;
    [SerializeField] private GameObject wrapper;
    [SerializeField] private Image cardImage;
    [SerializeField] private LayerMask dropLayer;
    public Card Card { get; private set; }
    private bool selectedForExhaust;
    private int lastDisplayedManaCost = -1;
    private string lastDisplayedDescription;

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    private Vector2 titleBasePosition;
    private Vector2 descriptionBasePosition;
    private Vector2 manaBasePosition;
    private bool textBasePositionsInitialized;
    private Vector2 cardVisualScale = Vector2.one;
    public Vector3 DisplayScale => new(cardVisualScale.x, cardVisualScale.y, 1f);

    public void Setup(Card card)
    {
        if (card == null)
        {
            Debug.LogError("CardView received a null card.");
            return;
        }

        Card = card;

        title.text = card.Title;
        RefreshDescription();
        RefreshManaCost();
        ApplyTextOffsets(
            card.TitleTextOffset,
            card.DescriptionTextOffset,
            card.ManaTextOffset);
        cardVisualScale = card.CardVisualScale;

        if (cardImage == null)
        {
            Debug.LogError(
                "Card Image is not assigned on CardView."
            );

            return;
        }

        cardImage.rectTransform.sizeDelta = new Vector2(StandardCardWidth, StandardCardHeight);
        cardImage.preserveAspect = false;
        cardImage.sprite = card.Image;
        cardImage.enabled = card.Image != null;

        if (card.Image == null)
        {
            Debug.LogWarning(
                "Card Image is missing for card: " + card.Title
            );
        }
    }

    private void ApplyTextOffsets(Vector2 titleOffset, Vector2 descriptionOffset, Vector2 manaOffset)
    {
        if (!textBasePositionsInitialized)
        {
            titleBasePosition = title.rectTransform.anchoredPosition;
            descriptionBasePosition = description.rectTransform.anchoredPosition;
            manaBasePosition = mana.rectTransform.anchoredPosition;
            textBasePositionsInitialized = true;
        }

        title.rectTransform.anchoredPosition = titleBasePosition + titleOffset;
        description.rectTransform.anchoredPosition = descriptionBasePosition + descriptionOffset;
        mana.rectTransform.anchoredPosition = manaBasePosition + manaOffset;
    }

    private void LateUpdate()
    {
        RefreshManaCost();
        RefreshDescription();
    }

    private void RefreshDescription()
    {
        if (Card == null || description == null) return;

        string currentDescription = Card.Description ?? string.Empty;
        if (lastDisplayedDescription == currentDescription) return;

        lastDisplayedDescription = currentDescription;
        description.text = currentDescription;
    }

    private int GetManaCost()
    {
        return CardSystem.Instance != null
            ? CardSystem.Instance.GetManaCost(Card)
            : Card.Mana;
    }

    private void RefreshManaCost()
    {
        if (Card == null || mana == null) return;

        int manaCost = GetManaCost();
        if (lastDisplayedManaCost == manaCost) return;

        lastDisplayedManaCost = manaCost;
        mana.text = manaCost.ToString();
    }

    private void OnMouseEnter()
    {
        if (!InteractionSystem.Instance.PlayerCanHover()) return;
        wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        CardViewHoverSystem.Instance.Show(Card, pos);
    }
    private void OnMouseExit()
    {
        if (!InteractionSystem.Instance.PlayerCanHover()) return;
        wrapper.SetActive(true);
        CardViewHoverSystem.Instance.Hide();
    }
    private void OnMouseDown()
    {
        if (CardSystem.Instance != null && CardSystem.Instance.IsCardInHand(Card))
        {
            SfxManager.PlaySfx(SfxId.CardPlayed);
        }

        if (
            CardSystem.Instance != null &&
            CardSystem.Instance.IsChoosingCardToUpgrade
        )
        {
            selectedForExhaust = true;
            wrapper.SetActive(true);

            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }
            CardSystem.Instance.ChooseCardToUpgrade(this);

            return;
        }

        if (CardSystem.Instance != null && CardSystem.Instance.IsChoosingCardToExhaust)
        {
            selectedForExhaust = true;
            wrapper.SetActive(true);

            if (CardViewHoverSystem.Instance != null)
            {
                CardViewHoverSystem.Instance.Hide();
            }
            CardSystem.Instance.ChooseCardToExhaust(Card);

            return;
        }
        if (!InteractionSystem.Instance.PlayerCanInteract()) return;
        if (Card.ManualTargetEffect != null)
        {
            ManualTargetSystem.Instance.StartTargeting(transform.position);
        }
        else
        {
            InteractionSystem.Instance.PlayerIsDragging = true;
            wrapper.SetActive(true);
            CardViewHoverSystem.Instance.Hide();
            dragStartPosition = transform.position;
            dragStartRotation = transform.rotation;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
        }

    }
    private void OnMouseDrag()
    {
        if (selectedForExhaust) return;
        if (!InteractionSystem.Instance.PlayerCanInteract()) return;
        if (Card.ManualTargetEffect != null) return;
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }
    private void OnMouseUp()
    {
        if (selectedForExhaust)
        {
            selectedForExhaust = false;
            return;
        }
        if (!InteractionSystem.Instance.PlayerCanInteract()) return;
        if (Card.ManualTargetEffect != null)
        {
            EnemyView target = ManualTargetSystem.Instance.EndTargeting(MouseUtil.GetMousePositionInWorldSpace(-1));
            if (target != null && CardSystem.Instance.CanPlayCard(Card))
            {
                PlayCardGA playCardGA = new(Card, target);
                ActionSystem.Instance.Perform(playCardGA);
            }
        }
        else
        {
            if (CardSystem.Instance.CanPlayCard(Card) && Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, 10f, dropLayer))
            {
                PlayCardGA playCardGA = new(Card);
                ActionSystem.Instance.Perform(playCardGA);
            }
            else
            {
                transform.position = dragStartPosition;
                transform.rotation = dragStartRotation;
            }
            InteractionSystem.Instance.PlayerIsDragging = false;
        }
    }
}
