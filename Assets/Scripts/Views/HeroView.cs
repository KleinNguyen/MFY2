using UnityEngine;

public class HeroView : CombatantView
{
    [SerializeField, Min(0.1f)] private float inGameSpriteScale = 1f;

    public void Setup(HeroData heroData)
    {
        SetupBase(heroData.Health, heroData.Image);
        SetSpriteScale(inGameSpriteScale);
    }
}
