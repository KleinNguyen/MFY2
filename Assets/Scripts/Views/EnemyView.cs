using UnityEngine;
using TMPro;

public class EnemyView : CombatantView
{
    private static readonly Vector2 NormalSpriteLimit = new(2f, 2.4f);
    private static readonly Vector2 EliteSpriteLimit = new(3.3f, 3.8f);
    private static readonly Vector2 BossSpriteLimit = new(4.2f, 5.2f);

    [SerializeField] private TMP_Text attackText;

    public int AttackPower { get; set; }
    public int ArmorPerTurn { get; private set; }
    private int lastDisplayedAttack = -1;

    public int GetAttackDamage()
    {
        int damage = Mathf.Max(0, AttackPower);
        return GetStatusEffectStacks(StatusEffectType.WEAK) > 0
            ? Mathf.FloorToInt(damage * 0.75f)
            : damage;
    }

    public void Setup(EnemyData enemyData)
    {
        AttackPower = enemyData.AttackPower;
        ArmorPerTurn = Mathf.Max(0, enemyData.ArmorPerTurn);
        SetupBase(enemyData.Health, enemyData.Image);

        lastDisplayedAttack = -1;
        UpdateAttackText();
        FitSpriteToLimit(GetSpriteLimit(enemyData.Rank));
        InitializeIdleAnimation();

        if (enemyData.StartingArmor > 0)
            AddStatusEffect(StatusEffectType.ARMOR, enemyData.StartingArmor);
    }

    private void Update()
    {
        UpdateIdleAnimation();
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
        if (attackText == null) return;

        int damage = GetAttackDamage();
        if (lastDisplayedAttack == damage) return;

        lastDisplayedAttack = damage;
        attackText.text = "ATK: " + damage;
    }
    private void LateUpdate()
    {
        UpdateAttackText();
    }
    public Vector3 GetTargetArrowPosition()
    {
        SpriteRenderer enemySprite = CombatantSpriteRenderer;
        if (enemySprite == null) return transform.position;

        Bounds bounds = enemySprite.bounds;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }
}
