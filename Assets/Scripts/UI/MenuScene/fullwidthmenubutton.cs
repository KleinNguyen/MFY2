using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class FullWidthMenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image buttonBackground;
    [SerializeField] private GameObject highlight;
    [SerializeField] private TMP_Text buttonText;

    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor =
        new Color32(208, 198, 198, 255);

    [SerializeField] private Color hoverTextColor =
        new Color32(103, 63, 63, 255);

    private void Awake()
    {
        // Làm trong suốt nền Button gốc
        if (buttonBackground != null)
        {
            Color c = buttonBackground.color;
            c.a = 0f;
            buttonBackground.color = c;
        }

        SetNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormal();
    }

    private void SetNormal()
    {
        if (highlight != null)
            highlight.SetActive(false);

        if (buttonText != null)
            buttonText.color = normalTextColor;
    }

    private void SetHover()
    {
        if (highlight != null)
            highlight.SetActive(true);

        if (buttonText != null)
            buttonText.color = hoverTextColor;
    }
}