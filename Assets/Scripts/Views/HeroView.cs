using UnityEngine;

public class HeroView : CombatantView
{
    private const float IdleFrameDuration = 0.7f;

    [SerializeField, Min(0.1f)] private float inGameSpriteScale = 1f;

    private Sprite[] idleFrames;
    private float idleFrameTimer;
    private int currentIdleFrame;
    private float baseSpriteCenterX;
    private Vector3 baseSpritePosition;

    public void Setup(HeroData heroData)
    {
        SetupBase(heroData.Health, heroData.Image);
        SetSpriteScale(inGameSpriteScale);

        idleFrames = heroData.IdleFrames;
        if (idleFrames == null || idleFrames.Length < 2)
        {
            return;
        }

        currentIdleFrame = 0;
        idleFrameTimer = 0f;
        baseSpriteCenterX = idleFrames[0].bounds.center.x;
        baseSpritePosition = CombatantSpriteRenderer.transform.localPosition;
        ShowIdleFrame(currentIdleFrame);
    }

    private void Update()
    {
        if (idleFrames == null || idleFrames.Length < 2)
        {
            return;
        }

        idleFrameTimer += Time.unscaledDeltaTime;
        if (idleFrameTimer < IdleFrameDuration)
        {
            return;
        }

        int framesToAdvance = Mathf.FloorToInt(idleFrameTimer / IdleFrameDuration);
        idleFrameTimer %= IdleFrameDuration;
        currentIdleFrame = (currentIdleFrame + framesToAdvance) % idleFrames.Length;
        ShowIdleFrame(currentIdleFrame);
    }

    private void ShowIdleFrame(int frameIndex)
    {
        Sprite frame = idleFrames[frameIndex];
        CombatantSpriteRenderer.sprite = frame;

        Vector3 spritePosition = baseSpritePosition;
        spritePosition.x += (baseSpriteCenterX - frame.bounds.center.x) * inGameSpriteScale;
        CombatantSpriteRenderer.transform.localPosition = spritePosition;
    }
}
