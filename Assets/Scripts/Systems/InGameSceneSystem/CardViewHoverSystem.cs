using UnityEngine;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;
    [SerializeField] private int hoverSortingOrder = 100;
    [SerializeField] private float hoverZ = -1f;

    private Canvas hoverCanvas;

    public void Show(Card card, Vector3 position)
    {
        if (cardViewHover == null || card == null)
        {
            return;
        }

        if (hoverCanvas == null)
        {
            hoverCanvas = cardViewHover.GetComponentInChildren<Canvas>(true);
        }

        if (hoverCanvas == null)
        {
            Debug.LogError("CardViewHover does not have a Canvas.");
            return;
        }
        hoverCanvas.overrideSorting = true;
        hoverCanvas.sortingOrder = hoverSortingOrder;

        position.z = hoverZ;

        cardViewHover.transform.SetPositionAndRotation(position, Quaternion.identity);
        cardViewHover.Setup(card);
        cardViewHover.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (cardViewHover == null)
        {
            return;
        }

        cardViewHover.gameObject.SetActive(false);
    }
}