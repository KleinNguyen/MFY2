using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LibraryCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Image cardImage;

    public void Setup(CardData data, bool showUpgrade)
    {
        if (data == null)
        {
            Debug.LogError("Library card data is missing.");
            return;
        }

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

        cardImage.sprite = data.CardImage;
        cardImage.enabled = data.CardImage != null;
    }
}