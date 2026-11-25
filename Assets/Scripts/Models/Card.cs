using System.Collections.Generic;
using UnityEngine;

public class Card
{
    private readonly CardData data;
    private readonly Sprite junkImage;

    private bool upgradedForCombat;

    public bool IsUnplayable { get; }

    public bool IsUpgraded
    {
        get
        {
            if (IsUnplayable)
            {
                return false;
            }

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
            if (IsUnplayable)
            {
                return "Junk";
            }

            if (IsUpgraded)
            {
                return data.name + "+";
            }

            return data.name;
        }
    }

    public int Mana
    {
        get
        {
            if (IsUnplayable)
            {
                return 0;
            }

            if (IsUpgraded)
            {
                return data.UpgradedMana;
            }

            return data.Mana;
        }
    }

    public Effect ManualTargetEffect
    {
        get
        {
            if (IsUnplayable)
            {
                return null;
            }

            if (IsUpgraded)
            {
                return data.UpgradedManualTargetEffect;
            }

            return data.ManualTargetEffect;
        }
    }

    public List<AutoTargetEffect> OtherEffects
    {
        get
        {
            if (IsUnplayable)
            {
                return null;
            }

            if (IsUpgraded)
            {
                return data.UpgradedOtherEffects;
            }

            return data.OtherEffects;
        }
    }

    public string Description
    {
        get
        {
            if (IsUnplayable)
            {
                return "Unplayable.\nRemoved after this battle.";
            }

            if (IsUpgraded)
            {
                return data.UpgradedDescription;
            }

            return data.Description;
        }
    }

    public Sprite Image
    {
        get
        {
            if (IsUnplayable)
            {
                return junkImage;
            }

            return data.CardImage;
        }
    }

    public Vector2 TitleTextOffset
    {
        get
        {
            if (IsUnplayable)
            {
                return Vector2.zero;
            }

            return data.TitleTextOffset;
        }
    }

    public Vector2 DescriptionTextOffset
    {
        get
        {
            if (IsUnplayable)
            {
                return Vector2.zero;
            }

            return data.DescriptionTextOffset;
        }
    }

    public Vector2 ManaTextOffset
    {
        get
        {
            if (IsUnplayable)
            {
                return Vector2.zero;
            }

            return data.ManaTextOffset;
        }
    }

    public Vector2 CardVisualScale
    {
        get
        {
            if (IsUnplayable)
            {
                return Vector2.one;
            }

            return data.CardVisualScale;
        }
    }

    public bool IsAttack
    {
        get
        {
            if (IsUnplayable)
            {
                return false;
            }

            return data.IsAttack;
        }
    }

    public bool ExhaustOnPlay
    {
        get
        {
            if (IsUnplayable)
            {
                return false;
            }

            return data.ExhaustOnPlay;
        }
    }

    public Card(CardData cardData)
    {
        data = cardData;
    }

    private Card(Sprite image)
    {
        IsUnplayable = true;
        junkImage = image;
    }

    public static Card CreateJunk(Sprite image)
    {
        return new Card(image);
    }

    public Card CreateCombatCopy()
    {
        if (IsUnplayable)
        {
            return CreateJunk(junkImage);
        }

        Card copy = new Card(data);
        copy.upgradedForCombat = IsUpgraded;

        return copy;
    }

    public bool CanUpgradeForCombat()
    {
        if (IsUnplayable)
        {
            return false;
        }

        if (!data.CanUpgrade)
        {
            return false;
        }

        return !IsUpgraded;
    }

    public bool IsSameType(Card other)
    {
        if (other == null || IsUnplayable || other.IsUnplayable)
        {
            return false;
        }

        if (data == null || other.data == null)
        {
            return false;
        }

        if (data == other.data)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(data.SaveId) &&
               data.SaveId == other.data.SaveId;
    }

    public void UpgradeForCombat()
    {
        if (CanUpgradeForCombat())
        {
            upgradedForCombat = true;
        }
    }
}