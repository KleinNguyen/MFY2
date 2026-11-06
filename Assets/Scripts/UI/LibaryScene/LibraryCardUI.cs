using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LibraryCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Image cardImage;

    public void Setup(CardData data, bool showRunUpgrade)
    {
        if (data == null)
        {
            Debug.LogError("Library card data is missing.");
            return;
        }

        if (showRunUpgrade)
        {
            Card card = new Card(data);

            titleText.text = card.Title;
            manaText.text = card.Mana.ToString();
            descriptionText.text = card.Description;
        }
        else
        {
            titleText.text = data.name;
            manaText.text = data.Mana.ToString();
            descriptionText.text = data.Description;
        }

        cardImage.sprite = data.CardImage;
        cardImage.enabled = data.CardImage != null;
    }
}