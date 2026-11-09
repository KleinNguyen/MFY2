using UnityEngine;

public class HeroView : CombatantView
{
    private const float IdleFrameDuration = 0.4f;

    [SerializeField, Min(0.1f)] private float inGameSpriteScale = 1f;

    private Sprite[] idleFrames;
    private float idleFrameTimer;
    private int currentIdleFrame;

    public void Setup(HeroData heroData)
    {
        SetupBase(heroData.Health, heroData.Image);
        SetSpriteScale(inGameSpriteScale);

        idleFrames = heroData.IdleFrames;
        if (idleFrames != null && idleFrames.Length > 0 && idleFrames[0] != null)
        {
            currentIdleFrame = 0;
            ShowIdleFrame(currentIdleFrame);
        }

        idleFrameTimer = 0f;
        InitializeIdleAnimation();
    }

    private void Update()
    {
        UpdateIdleAnimation();

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
        if (frame == null)
        {
            return;
        }

        SetSpriteKeepingFeetPosition(frame);
    }
}
