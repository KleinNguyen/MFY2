using System.Collections.Generic;
using UnityEngine;

public class Card
{
    private readonly CardData data;
    private readonly Sprite junkImage;
    private bool upgradedForCombat;

    public bool IsUnplayable { get; }
    public bool HasTemporarySly { get; private set; }
    public bool IsUpgraded => !IsUnplayable && (upgradedForCombat || (RunSystem.Instance != null && RunSystem.Instance.IsCardUpgraded(data)));
    public string Title => IsUnplayable ? "Junk" : IsUpgraded ? data.name + "+" : data.name;
    public int Mana => IsUnplayable ? 0 : IsUpgraded ? data.UpgradedMana : data.Mana;
    public Effect ManualTargetEffect => IsUnplayable ? null : IsUpgraded ? data.UpgradedManualTargetEffect : data.ManualTargetEffect;
    public List<AutoTargetEffect> OtherEffects => IsUnplayable ? null : IsUpgraded ? data.UpgradedOtherEffects : data.OtherEffects;

    public Sprite Image => IsUnplayable ? junkImage : data.CardImage;
    public Vector2 TitleTextOffset => IsUnplayable ? Vector2.zero : data.TitleTextOffset;
    public Vector2 DescriptionTextOffset => IsUnplayable ? Vector2.zero : data.DescriptionTextOffset;
    public Vector2 ManaTextOffset => IsUnplayable ? Vector2.zero : data.ManaTextOffset;
    public Vector2 CardVisualScale => IsUnplayable ? Vector2.one : data.CardVisualScale;
    public bool IsAttack => !IsUnplayable && data.IsAttack;
    public bool ExhaustOnPlay => !IsUnplayable && data.ExhaustOnPlay;
    public bool IsSly => !IsUnplayable && (data.IsSly || HasTemporarySly);

    public string Description
    {
        get
        {
            if (IsUnplayable) return "Unplayable.\nRemoved after this battle.";
            string description = IsUpgraded ? data.UpgradedDescription : data.Description;
            return HasTemporarySly && !data.IsSly ? description + "\nSly (this turn)." : description;
        }
    }

    public Card(CardData cardData) => data = cardData;

    private Card(Sprite image)
    {
        IsUnplayable = true;
        junkImage = image;
    }

    public static Card CreateJunk(Sprite image) => new Card(image);
    public bool CanUpgradeForCombat() => !IsUnplayable && data.CanUpgrade && !IsUpgraded;

    public void UpgradeForCombat()
    {
        if (CanUpgradeForCombat()) upgradedForCombat = true;
    }

    public void GrantTemporarySly()
    {
        if (!IsUnplayable) HasTemporarySly = true;
    }

    public void ClearTemporarySly() => HasTemporarySly = false;

    public Card CreateCombatCopy()
    {
        if (IsUnplayable) return CreateJunk(junkImage);

        Card copy = new(data);
        copy.upgradedForCombat = IsUpgraded;
        return copy;
    }
}