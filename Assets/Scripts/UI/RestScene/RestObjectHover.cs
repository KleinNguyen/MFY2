using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RestObjectHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private GameObject highlight;
    [SerializeField] private string hintText = "Rest";
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Vector2 hintOffset = new(0f, 230f);
    [SerializeField] private Vector2 arrowSize = new(64f, 48f);

    private GameObject hintObject;

    private void Awake()
    {
        CreateHintObjects();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight != null) highlight.SetActive(true);
        if (hintObject != null) hintObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight != null) highlight.SetActive(false);
        if (hintObject != null) hintObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (highlight != null) highlight.SetActive(false);
        if (hintObject != null) hintObject.SetActive(false);
    }

    private void CreateHintObjects()
    {
        RectTransform objectRect = transform as RectTransform;
        if (objectRect == null)
        {
            Debug.LogError("RestObjectHover must be attached to a UI object.", this);
            return;
        }

        hintObject = new GameObject("HoverHint", typeof(RectTransform));
        RectTransform hintRect = (RectTransform)hintObject.transform;
        hintRect.SetParent(transform, false);
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = hintOffset;
        hintRect.sizeDelta = new Vector2(220f, 100f);

        GameObject textObject = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(hintRect, false);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 24f);
        textRect.sizeDelta = new Vector2(220f, 52f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = hintText;
        text.fontSize = 36f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        GameObject arrowObject = new GameObject("HintArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform arrowRect = (RectTransform)arrowObject.transform;
        arrowRect.SetParent(hintRect, false);
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(0f, -20f);
        arrowRect.sizeDelta = arrowSize;

        Image arrowImage = arrowObject.GetComponent<Image>();
        arrowImage.sprite = arrowSprite;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        hintObject.SetActive(false);
    }
}