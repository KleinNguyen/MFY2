using System.Collections.Generic;
using UnityEngine;

public class Card
{
    private readonly CardData data;

    public bool IsUpgraded
    {
        get
        {
            if (RunSystem.Instance == null) return false;

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
            if (IsUpgraded) return data.UpgradedDescription;

            return data.Description;
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

    public CardType Type => data.Type;
    public bool ExhaustOnPlay => data.ExhaustOnPlay;
    public bool IsSly => data.IsSly;

    public Card(CardData cardData)
    {
        data = cardData;
    }
}