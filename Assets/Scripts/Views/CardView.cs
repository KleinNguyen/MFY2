using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text mana;
    [SerializeField] private LibraryCardUI libraryTemplate;

    public LibraryCardUI LibraryTemplate => libraryTemplate;
    [SerializeField] private GameObject wrapper;
    [SerializeField] private Image cardImage;
    [SerializeField] private LayerMask dropLayer;
    public Card Card { get; private set; }
    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    public void Setup(Card card)
    {
        if (card == null)
        {
            Debug.LogError("CardView received a null card.");
            return;
        }

        Card = card;

        title.text = card.Title;
        description.text = card.Description;
        mana.text = card.Mana.ToString();

        if (cardImage == null)
        {
            Debug.LogError(
                "Card Image is not assigned on CardView."
            );

            return;
        }

        cardImage.sprite = card.Image;

        cardImage.enabled = card.Image != null;

        if (card.Image == null)
        {
            Debug.LogWarning(
                "Card Image is missing for card: " + card.Title
            );
        }
    }
    private void OnMouseEnter()
    {
        if(!InteractionSystem.Instance.PlayerCanHover()) return;
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
        if(!InteractionSystem.Instance.PlayerCanInteract()) return;
        if(Card.ManualTargetEffect != null)
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
        if(!InteractionSystem.Instance.PlayerCanInteract()) return;
        if (Card.ManualTargetEffect != null) return;
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }
    private void OnMouseUp()
    {
        if(!InteractionSystem.Instance.PlayerCanInteract()) return;
        if(Card.ManualTargetEffect != null)
        {
            EnemyView target = ManualTargetSystem.Instance.EndTargeting(MouseUtil.GetMousePositionInWorldSpace(-1));
            if(target != null && ManaSystem.Instance.HasEnoughMana(Card.Mana))
            {
                PlayCardGA playCardGA = new(Card, target);
                ActionSystem.Instance.Perform(playCardGA);
            }
        }
        else
        {
            if (ManaSystem.Instance.HasEnoughMana(Card.Mana) && Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, 10f, dropLayer))
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
