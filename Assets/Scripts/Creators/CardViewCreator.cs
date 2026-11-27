using DG.Tweening;
using UnityEngine;

public class CardViewCreator : Singleton<CardViewCreator>
{
    [SerializeField] private CardView cardViewPrefab;

    public CardView CreateCardView(Card card, Vector3 position, Quaternion rotation)
    {
        CardView cardView = Instantiate(cardViewPrefab, position, rotation);
        cardView.Setup(card);
        cardView.SetInteractionEnabled(false);
        cardView.transform.localScale = Vector3.zero;
        cardView.transform.DOScale(cardView.DisplayScale, 0.10f);
        return cardView;
    }
}
