using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyView : CombatantView
{
    private static readonly Vector2 NormalSpriteLimit = new(2f, 2.4f);
    private static readonly Vector2 EliteSpriteLimit = new(3.3f, 3.8f);
    private static readonly Vector2 BossSpriteLimit = new(4.2f, 5.2f);

    [SerializeField] private TMP_Text attackText;

    private readonly List<EnemyMoveData> moves = new();
    private readonly List<StatusEffectType> allowedDebuffs = new();
    private int moveIndex;
    private string lastIntentText;

    public EnemyMoveData CurrentMove => moves.Count > 0 ? moves[moveIndex] : null;
    public IReadOnlyList<StatusEffectType> AllowedDebuffs => allowedDebuffs;
    public EnemyMoveType CurrentMoveType => CurrentMove?.MoveType ?? EnemyMoveType.Rest;

    public bool CurrentMoveAttacks => CurrentMoveType == EnemyMoveType.Attack ||
        CurrentMoveType == EnemyMoveType.AttackAndDefend;

    public int CurrentHitCount => CurrentMoveAttacks ? Mathf.Max(1, CurrentMove.HitCount) : 0;

    public int CurrentArmorGain => CurrentMoveType == EnemyMoveType.Defend ||
        CurrentMoveType == EnemyMoveType.AttackAndDefend
        ? Mathf.Max(0, CurrentMove.ArmorGain) : 0;
    public bool CanApplyRandomDebuff => CurrentMoveAttacks && CurrentMove != null &&
        CurrentMove.ApplyRandomDebuff && CurrentMove.DebuffChancePercent > 0f && allowedDebuffs.Count > 0;

    public void Setup(EnemyData enemyData)
    {
        SetupBase(enemyData.Health, enemyData.Image);

        moves.Clear();
        if (enemyData.Moves != null)
        {
            foreach (EnemyMoveData move in enemyData.Moves)
            {
                if (move != null) moves.Add(move);
            }
        }

        if (moves.Count == 0)
            Debug.LogError($"Enemy '{enemyData.name}' has no Moves. Configure its Moves in EnemyData.", enemyData);

        moveIndex = 0;
        LoadDebuffPool();
        lastIntentText = null;
        FitSpriteToLimit(GetSpriteLimit(enemyData.Rank));
        InitializeIdleAnimation();
        UpdateIntentText();
    }

    public int GetAttackDamage()
    {
        if (!CurrentMoveAttacks || CurrentMove == null) return 0;

        int damage = Mathf.Max(0, CurrentMove.DamagePerHit);
        return GetStatusEffectStacks(StatusEffectType.WEAK) > 0 ? Mathf.FloorToInt(damage * 0.75f) : damage;
    }
    private int GetAttackPreviewDamage()
    {
        int damage = GetAttackDamage();
        HeroView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;

        if (hero != null && hero.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0)
            damage = Mathf.FloorToInt(damage * 1.5f);

        return damage;
    }

    public void AdvanceMove()
    {
        if (moves.Count > 0) moveIndex = (moveIndex + 1) % moves.Count;
        UpdateIntentText();
    }

    private void LoadDebuffPool()
    {
        allowedDebuffs.Clear();
        ActData act = RunSystem.Instance != null ? RunSystem.Instance.CurrentAct : null;
        if (act == null || act.EnemyDebuffPool == null) return;

        foreach (StatusEffectType effect in act.EnemyDebuffPool)
        {
            bool isDebuff = effect == StatusEffectType.WEAK || effect == StatusEffectType.POISON ||
                effect == StatusEffectType.EXPOSED || effect == StatusEffectType.ARMOR_BREAK;

            if (isDebuff && !allowedDebuffs.Contains(effect))
                allowedDebuffs.Add(effect);
        }
    }

    private void UpdateIntentText()
    {
        if (attackText == null) return;
        List<string> lines = new();

        if (CurrentMoveAttacks)
        {
            string attack = "ATK: " + GetAttackPreviewDamage();
            if (CurrentHitCount > 1) attack += " x " + CurrentHitCount;
            lines.Add(attack);
        }

        if (CurrentMoveType == EnemyMoveType.Defend)
            lines.Add("DEFEND");

        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);
        lines.Add("ARMOR: " + currentArmor);

        if (CurrentMoveType == EnemyMoveType.Rest)
            lines.Add("REST");

        //if (CanApplyRandomDebuff)
        //{
        //    int stacks = Mathf.Max(1, CurrentMove.DebuffStacks);
        //    float chance = Mathf.Clamp(CurrentMove.DebuffChancePercent, 0f, 100f);
        //    lines.Add($"DEBUFF: ? x {stacks} ({chance:0.#}%)");
        //}

        string intent = string.Join("\n", lines);
        if (lastIntentText == intent) return;

        lastIntentText = intent;
        attackText.text = intent;
    }

    private void Update()
    {
        UpdateIdleAnimation();
    }

    private void LateUpdate()
    {
        UpdateIntentText();
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

        float scaleFactor = Mathf.Min(maxSize.x / spriteSize.x, maxSize.y / spriteSize.y);
        CombatantSpriteRenderer.transform.localScale *= scaleFactor;
    }

    public Vector3 GetTargetArrowPosition()
    {
        SpriteRenderer enemySprite = CombatantSpriteRenderer;
        if (enemySprite == null) return transform.position;

        Bounds bounds = enemySprite.bounds;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }
}