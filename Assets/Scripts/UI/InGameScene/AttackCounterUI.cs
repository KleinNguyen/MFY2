using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class AttackCounterUI : MonoBehaviour
{
    private TMP_Text counterText;
    private int lastCount = -1;

    private void Awake()
    {
        counterText = GetComponent<TMP_Text>();
        counterText.raycastTarget = false;
        counterText.enabled = false;
    }

    private void LateUpdate()
    {
        HeroSystem hero = HeroSystem.Instance;
        CardSystem cards = CardSystem.Instance;
        bool visible = hero != null && hero.CurrentCharacter == CharacterType.Yellow &&
            cards != null && cards.Combo != null;

        counterText.enabled = visible;
        if (!visible) return;

        int count = cards.Combo.AttacksThisTurn;
        if (count == lastCount) return;

        lastCount = count;
        counterText.text = "ATK Card: " + count;
    }

    private void OnDisable()
    {
        if (counterText != null) counterText.enabled = false;
    }
}
