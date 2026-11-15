using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyView : CombatantView
{
    private static readonly Vector2 NormalSpriteLimit = new(2f, 2.4f);
    private static readonly Vector2 EliteSpriteLimit = new(3.3f, 3.8f);
    private static readonly Vector2 BossSpriteLimit = new(4.2f, 5.2f);

    [SerializeField] private TMP_Text attackText;

    public int AttackPower { get; set; }
    public int ArmorPerTurn { get; private set; }
    public EnemyMoveData CurrentMove => moves.Count > 0 ? moves[moveIndex] : null;
    public IReadOnlyList<StatusEffectType> AllowedDebuffs => allowedDebuffs;

    private readonly List<EnemyMoveData> moves = new();
    private readonly List<StatusEffectType> allowedDebuffs = new();
    private int moveIndex;
    private string lastIntentText;

    public EnemyMoveType CurrentMoveType => CurrentMove != null ? CurrentMove.MoveType :
        ArmorPerTurn > 0 ? EnemyMoveType.AttackAndDefend : EnemyMoveType.Attack;

    public bool CurrentMoveAttacks => CurrentMoveType == EnemyMoveType.Attack ||
        CurrentMoveType == EnemyMoveType.AttackAndDefend;

    public int CurrentHitCount => CurrentMoveAttacks ? Mathf.Max(1, CurrentMove?.HitCount ?? 1) : 0;

    public int CurrentArmorGain => CurrentMoveType == EnemyMoveType.Defend ||
        CurrentMoveType == EnemyMoveType.AttackAndDefend
        ? Mathf.Max(0, CurrentMove?.ArmorGain ?? ArmorPerTurn) : 0;

    public int CurrentStrengthGain => CurrentMoveType == EnemyMoveType.Buff
        ? Mathf.Max(0, CurrentMove?.StrengthGain ?? 0) : 0;

    public bool CanApplyRandomDebuff => CurrentMoveAttacks && CurrentMove != null &&
        CurrentMove.ApplyRandomDebuff && CurrentMove.DebuffChancePercent > 0f && allowedDebuffs.Count > 0;

    public void Setup(EnemyData enemyData)
    {
        AttackPower = Mathf.Max(0, enemyData.AttackPower);
        ArmorPerTurn = Mathf.Max(0, enemyData.ArmorPerTurn);
        SetupBase(enemyData.Health, enemyData.Image);

        moves.Clear();
        if (enemyData.Moves != null)
        {
            foreach (EnemyMoveData move in enemyData.Moves)
            {
                if (move != null) moves.Add(move);
            }
        }

        moveIndex = 0;
        LoadDebuffPool();
        lastIntentText = null;
        FitSpriteToLimit(GetSpriteLimit(enemyData.Rank));
        InitializeIdleAnimation();

        if (enemyData.StartingArmor > 0)
            AddStatusEffect(StatusEffectType.ARMOR, enemyData.StartingArmor);

        UpdateIntentText();
    }

    public int GetAttackDamage()
    {
        if (!CurrentMoveAttacks) return 0;

        int baseDamage = CurrentMove != null ? CurrentMove.DamagePerHit : AttackPower;
        int damage = Mathf.Max(0, baseDamage + GetStatusEffectStacks(StatusEffectType.STRENGTH));
        return GetStatusEffectStacks(StatusEffectType.WEAK) > 0
            ? Mathf.FloorToInt(damage * 0.75f) : damage;
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

            if (isDebuff && !allowedDebuffs.Contains(effect)) allowedDebuffs.Add(effect);
        }
    }

    private void UpdateIntentText()
    {
        if (attackText == null) return;
        List<string> lines = new();

        if (CurrentMoveAttacks)
        {
            string attack = "ATK: " + GetAttackDamage();
            if (CurrentHitCount > 1) attack += " x " + CurrentHitCount;
            lines.Add(attack);
        }

        if (CurrentMoveType == EnemyMoveType.Defend || CurrentMoveType == EnemyMoveType.AttackAndDefend)
            lines.Add("ARMOR: +" + CurrentArmorGain);

        if (CurrentMoveType == EnemyMoveType.Buff)
            lines.Add("STR: +" + CurrentStrengthGain);

        if (CurrentMoveType == EnemyMoveType.Rest)
            lines.Add("REST");

        if (CanApplyRandomDebuff)
        {
            int stacks = Mathf.Max(1, CurrentMove.DebuffStacks);
            float chance = Mathf.Clamp(CurrentMove.DebuffChancePercent, 0f, 100f);
            lines.Add($"DEBUFF: ? x {stacks} ({chance:0.#}%)");
        }

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
