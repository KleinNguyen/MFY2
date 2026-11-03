using UnityEngine;
using UnityEngine.EventSystems;

public class ShopObjectHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private GameObject highlight;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight == null) return;

        highlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight == null) return;

        highlight.SetActive(false);
    }

    private void OnDisable()
    {
        if (highlight == null) return;

        highlight.SetActive(false);
    }
}