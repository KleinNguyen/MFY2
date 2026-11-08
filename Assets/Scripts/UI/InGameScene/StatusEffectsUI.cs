using System.Collections.Generic;
using UnityEngine;

public class StatusEffecstUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectUIPrefab;
    [SerializeField] private Sprite armorSprite;
    [SerializeField] private Sprite poisonSprite;
    [SerializeField] private Sprite weakSprite;
    [SerializeField] private Sprite strengthSprite;
    [SerializeField] private Sprite dexteritySprite;
    [SerializeField] private Sprite exposedSprite;
    [SerializeField] private Sprite armorBreakSprite;
    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs = new();
    public void UpdateStatusEffectUI(StatusEffectType statusEffectType, int stackCount)
    {
        if (stackCount == 0)
        {
            if (statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = statusEffectUIs[statusEffectType];
                Destroy(statusEffectUI.gameObject);
                statusEffectUIs.Remove(statusEffectType);
            }
        }
        else
        {
            if (!statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = Instantiate(statusEffectUIPrefab, transform);
                statusEffectUIs.Add(statusEffectType, statusEffectUI);
            }
            Sprite sprite = GetSpriteByType(statusEffectType);
            statusEffectUIs[statusEffectType].Set(sprite, stackCount);
        }
    }
    private Sprite GetSpriteByType(StatusEffectType statusEffectType)
    {
        return statusEffectType switch
        {
            StatusEffectType.ARMOR => armorSprite,

            StatusEffectType.POISON => poisonSprite,

            StatusEffectType.WEAK => weakSprite,

            StatusEffectType.STRENGTH => strengthSprite,

            StatusEffectType.DEXTERITY => dexteritySprite,

            StatusEffectType.EXPOSED => exposedSprite,

            StatusEffectType.ARMOR_BREAK => armorBreakSprite,

            _ => null
        };
    }
}
