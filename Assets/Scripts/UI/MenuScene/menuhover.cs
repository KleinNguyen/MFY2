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
        // Thiết lập trạng thái ban đầu
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
        // Xác định vị trí chuột trên UI mỗi frame
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

                // Kiểm tra con trỏ có nằm trên nút hay không
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

        // Chỉ bật sáng nút đang được trỏ tới
        foreach (MenuItem item in items)
        {
            SetItemState(item, item == hoveredItem);
        }
    }

    private void SetItemState(MenuItem item, bool hovered)
    {
        if (item.highlight != null)
            item.highlight.SetActive(hovered);

        if (item.label != null)
        {
            item.label.color = hovered
                ? hoverTextColor
                : normalTextColor;
        }
    }
}