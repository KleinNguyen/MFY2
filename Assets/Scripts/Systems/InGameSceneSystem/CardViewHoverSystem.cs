using UnityEngine;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;
    [SerializeField] private int hoverSortingOrder = 100;
    [SerializeField] private float hoverZ = -1f;

    private Canvas hoverCanvas;
    private Vector3 cardViewBaseScale;
    private bool cardViewBaseScaleInitialized;

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

        if (!cardViewBaseScaleInitialized)
        {
            cardViewBaseScale = cardViewHover.transform.localScale;
            cardViewBaseScaleInitialized = true;
        }

        cardViewHover.transform.SetPositionAndRotation(position, Quaternion.identity);
        cardViewHover.Setup(card);
        cardViewHover.transform.localScale = Vector3.Scale(
            cardViewBaseScale,
            cardViewHover.DisplayScale);
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