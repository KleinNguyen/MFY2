using System.Collections.Generic;
using UnityEngine;

public class Card
{
    private readonly CardData data;

    private bool upgradedForCombat;
    public bool HasTemporarySly { get; private set; }
    public bool IsUpgraded
    {
        get
        {
            if (upgradedForCombat)
            {
                return true;
            }

            if (RunSystem.Instance == null)
            {
                return false;
            }

            return RunSystem.Instance.IsCardUpgraded(data);
        }
    }

    public string Title
    {
        get
        {
            if (IsUpgraded) return data.name + "+";

            return data.name;
        }
    }

    public string Description
    {
        get
        {
            string cardDescription = IsUpgraded
                ? data.UpgradedDescription
                : data.Description;

            if (HasTemporarySly && !data.IsSly)
            {
                return cardDescription + "\nSly (this turn).";
            }

            return cardDescription;
        }
    }

    public int Mana
    {
        get
        {
            if (IsUpgraded) return data.UpgradedMana;

            return data.Mana;
        }
    }

    public Effect ManualTargetEffect
    {
        get
        {
            if (IsUpgraded) return data.UpgradedManualTargetEffect;

            return data.ManualTargetEffect;
        }
    }

    public List<AutoTargetEffect> OtherEffects
    {
        get
        {
            if (IsUpgraded) return data.UpgradedOtherEffects;

            return data.OtherEffects;
        }
    }
    public bool CanUpgradeForCombat()
    {
        return data.CanUpgrade && !IsUpgraded;
    }

    public void UpgradeForCombat()
    {
        if (!CanUpgradeForCombat())
        {
            return;
        }

        upgradedForCombat = true;
    }
    public Sprite Image => data.CardImage;

    public bool ExhaustOnPlay => data.ExhaustOnPlay;
    public bool IsSly => data.IsSly || HasTemporarySly;

    public void GrantTemporarySly()
    {
        HasTemporarySly = true;
    }

    public void ClearTemporarySly()
    {
        HasTemporarySly = false;
    }

    public Card CreateCombatCopy()
    {
        Card copy = new(data);
        copy.upgradedForCombat = IsUpgraded;
        return copy;
    }

    public Card(CardData cardData)
    {
        data = cardData;
    }
}
