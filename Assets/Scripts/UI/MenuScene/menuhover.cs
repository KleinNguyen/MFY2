using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuHoverManager : MonoBehaviour
{
    [System.Serializable]
    public class MenuItem
    {
        public Button button;
        public GameObject highlight;
        public TMP_Text label;
    }

    [Header("Menu Buttons")]
    public MenuItem[] items;

    [Header("Colors")]
    public Color normalTextColor =
        new Color32(208, 198, 198, 255);

    public Color hoverTextColor =
        new Color32(103, 63, 63, 255);

    private void Start()
    {
        // Set the initial visual state for each menu item.
        foreach (MenuItem item in items)
        {
            if (item.button != null)
            {
                item.button.transition =
                    Selectable.Transition.None;

                Image image = item.button.GetComponent<Image>();

                if (image != null)
                {
                    Color c = image.color;
                    c.a = 0f;
                    image.color = c;
                }
            }

            SetItemState(item, false);
        }
    }

    private void Update()
    {
        // Find the UI element currently under the pointer.
        if (EventSystem.current == null)
            return;

        PointerEventData pointer =
            new PointerEventData(EventSystem.current);

        pointer.position = Input.mousePosition;

        var results =
            new System.Collections.Generic.List<RaycastResult>();

        EventSystem.current.RaycastAll(pointer, results);

        MenuItem hoveredItem = null;

        foreach (RaycastResult result in results)
        {
            foreach (MenuItem item in items)
            {
                if (item.button == null)
                    continue;

                // Treat child graphics as part of their parent button.
                if (result.gameObject == item.button.gameObject ||
                    result.gameObject.transform.IsChildOf(
                        item.button.transform))
                {
                    hoveredItem = item;
                    break;
                }
            }

            if (hoveredItem != null)
                break;
        }

        // Highlight only the item currently under the pointer.
        foreach (MenuItem item in items)
        {
            SetItemState(item, item == hoveredItem);
        }
    }

    private void SetItemState(MenuItem item, bool hovered)
    {
        if (item == null)
        {
            return;
        }

        bool canHighlight = false;

        if (item.button != null && item.button.isActiveAndEnabled)
        {
            canHighlight = item.button.IsInteractable() && hovered;
        }

        if (item.highlight != null)
        {
            item.highlight.SetActive(canHighlight);
        }

        if (item.label != null)
        {
            if (canHighlight)
            {
                item.label.color = hoverTextColor;
            }
            else
            {
                item.label.color = normalTextColor;
            }
        }
    }
}