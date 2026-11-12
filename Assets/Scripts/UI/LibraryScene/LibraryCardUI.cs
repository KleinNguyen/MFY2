using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LibraryCardUI : MonoBehaviour
{
    private const float StandardCardWidth = 260f;
    private const float StandardCardHeight = 340f;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Image cardImage;
    private Vector2 titleBasePosition;
    private Vector2 descriptionBasePosition;
    private Vector2 manaBasePosition;
    private bool textBasePositionsInitialized;
    private Vector2 cardVisualScale = Vector2.one;
    private Vector3 prefabScale;
    private bool prefabScaleInitialized;

    private void Awake()
    {
        CachePrefabScale();
    }

    public void Setup(CardData data, bool showUpgrade)
    {
        if (data == null)
        {
            Debug.LogError("Library card data is missing.");
            return;
        }

        CachePrefabScale();
        bool useUpgrade = showUpgrade && data.CanUpgrade;

        if (useUpgrade)
        {
            titleText.text = data.name + "+";
            manaText.text = data.UpgradedMana.ToString();
            descriptionText.text = data.UpgradedDescription;
        }
        else
        {
            titleText.text = data.name;
            manaText.text = data.Mana.ToString();
            descriptionText.text = data.Description;
        }

        ApplyTextOffsets(
            data.TitleTextOffset,
            data.DescriptionTextOffset,
            data.ManaTextOffset);
        SetCardVisualScale(data.CardVisualScale);

        SetCardImage(data.CardImage);
    }
    public void Setup(Card card)
    {
        if (card == null)
        {
            Debug.LogError("Library card received a null Card.");
            return;
        }

        CachePrefabScale();
        titleText.text = card.Title;
        manaText.text = card.Mana.ToString();
        descriptionText.text = card.Description;
        ApplyTextOffsets(
            card.TitleTextOffset,
            card.DescriptionTextOffset,
            card.ManaTextOffset);
        SetCardVisualScale(card.CardVisualScale);

        SetCardImage(card.Image);
    }

    public void SetLayoutScale(Vector3 scale)
    {
        transform.localScale = Vector3.Scale(
            scale,
            new Vector3(cardVisualScale.x, cardVisualScale.y, 1f));
    }

    private void SetCardVisualScale(Vector2 scale)
    {
        cardVisualScale = scale;
        transform.localScale = Vector3.Scale(
            prefabScale,
            new Vector3(scale.x, scale.y, 1f));
    }

    private void CachePrefabScale()
    {
        if (prefabScaleInitialized)
        {
            return;
        }

        prefabScale = transform.localScale;
        prefabScaleInitialized = true;
    }

    private void SetCardImage(Sprite sprite)
    {
        cardImage.rectTransform.sizeDelta = new Vector2(StandardCardWidth, StandardCardHeight);
        cardImage.preserveAspect = false;
        cardImage.sprite = sprite;
        cardImage.enabled = sprite != null;
    }

    private void ApplyTextOffsets(Vector2 titleOffset, Vector2 descriptionOffset, Vector2 manaOffset)
    {
        if (!textBasePositionsInitialized)
        {
            titleBasePosition = titleText.rectTransform.anchoredPosition;
            descriptionBasePosition = descriptionText.rectTransform.anchoredPosition;
            manaBasePosition = manaText.rectTransform.anchoredPosition;
            textBasePositionsInitialized = true;
        }

        titleText.rectTransform.anchoredPosition = titleBasePosition + titleOffset;
        descriptionText.rectTransform.anchoredPosition = descriptionBasePosition + descriptionOffset;
        manaText.rectTransform.anchoredPosition = manaBasePosition + manaOffset;
    }
}