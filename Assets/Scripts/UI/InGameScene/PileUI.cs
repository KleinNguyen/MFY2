using TMPro;
using UnityEngine;

public class PileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text drawPileText;
    [SerializeField] private TMP_Text discardPileText;

    public void UpdateDrawPileText(int drawPileCount)
    {
        drawPileText.text = drawPileCount.ToString();
    }
    public void UpdateDiscardPileText(int discardPileCount)
    {
        discardPileText.text = discardPileCount.ToString();
    }
}
