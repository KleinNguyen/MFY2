using UnityEngine;
using TMPro;

public class EnemyView : CombatantView
{
    private const float IdleFrameDuration = 0.4f;
    private static readonly Vector2 NormalSpriteLimit = new(2f, 2.4f);
    private static readonly Vector2 EliteSpriteLimit = new(3.3f, 3.8f);
    private static readonly Vector2 BossSpriteLimit = new(4.2f, 5.2f);

    [SerializeField] private TMP_Text attackText;
    private Sprite[] idleFrames;
    private float idleFrameTimer;
    private int currentIdleFrame;

    public int AttackPower { get; set; }

    public void Setup(EnemyData enemyData)
    {
        AttackPower = enemyData.AttackPower;
        UpdateAttackText();
        SetupBase(enemyData.Health, enemyData.Image);

        idleFrames = enemyData.IdleFrames;
        if (idleFrames != null && idleFrames.Length > 0 && idleFrames[0] != null)
        {
            CombatantSpriteRenderer.sprite = idleFrames[0];
        }

        FitSpriteToLimit(GetSpriteLimit(enemyData.Rank));
        idleFrameTimer = 0f;
        currentIdleFrame = 0;

        if (idleFrames == null || idleFrames.Length == 0 || idleFrames[0] == null)
        {
            InitializeIdleAnimation();
            return;
        }

        ShowIdleFrame(currentIdleFrame);
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

    private Vector2 GetSpriteLimit(EnemyRank rank)
    {
        return rank switch
        {
            EnemyRank.Elite => EliteSpriteLimit,
            EnemyRank.Boss => BossSpriteLimit,
            _ => NormalSpriteLimit
        };
    }

    private void FitSpriteToLimit(Vector2 maxSize)
    {
        Vector3 spriteSize = CombatantSpriteRenderer.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        float scaleFactor = Mathf.Min(
            maxSize.x / spriteSize.x,
            maxSize.y / spriteSize.y
        );

        Transform spriteTransform = CombatantSpriteRenderer.transform;
        spriteTransform.localScale *= scaleFactor;
    }

    private void UpdateAttackText()
    {
        attackText.text = "ATK: " + AttackPower;
    }

}
