using System.Collections.Generic;
using UnityEngine;

public class StatusEffectsUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectUIPrefab;

    [SerializeField] private Sprite armorSprite;
    [SerializeField] private Sprite poisonSprite;
    [SerializeField] private Sprite weakSprite;
    [SerializeField] private Sprite strengthSprite;
    [SerializeField] private Sprite exposedSprite;
    [SerializeField] private Sprite armorBreakSprite;
    [SerializeField] private Sprite comboSprite;

    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs =
        new Dictionary<StatusEffectType, StatusEffectUI>();

    public void UpdateStatusEffectUI(
        StatusEffectType statusEffectType,
        int stackCount
    )
    {
        if (stackCount <= 0)
        {
            if (
                statusEffectUIs.TryGetValue(
                    statusEffectType,
                    out StatusEffectUI statusEffectUI
                )
            )
            {
                statusEffectUI.gameObject.SetActive(false);
                Destroy(statusEffectUI.gameObject);

                statusEffectUIs.Remove(statusEffectType);
            }

            return;
        }

        if (!statusEffectUIs.ContainsKey(statusEffectType))
        {
            StatusEffectUI statusEffectUI = Instantiate(
                statusEffectUIPrefab,
                transform
            );

            statusEffectUIs.Add(
                statusEffectType,
                statusEffectUI
            );
        }

        Sprite sprite = GetSpriteByType(statusEffectType);

        statusEffectUIs[statusEffectType].Set(
            statusEffectType,
            sprite,
            stackCount
        );
    }

    private Sprite GetSpriteByType(StatusEffectType statusEffectType)
    {
        switch (statusEffectType)
        {
            case StatusEffectType.ARMOR:
                return armorSprite;

            case StatusEffectType.POISON:
                return poisonSprite;

            case StatusEffectType.WEAK:
                return weakSprite;

            case StatusEffectType.STRENGTH:
                return strengthSprite;

            case StatusEffectType.EXPOSED:
                return exposedSprite;

            case StatusEffectType.ARMOR_BREAK:
                return armorBreakSprite;

            case StatusEffectType.COMBO:
                return comboSprite;

            default:
                return null;
        }
    }
}