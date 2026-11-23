using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class EnemyView : CombatantView
{
    private static readonly Vector2 NormalSpriteLimit = new(2f, 2.4f);
    private static readonly Vector2 EliteSpriteLimit = new(3.3f, 3.8f);
    private static readonly Vector2 BossSpriteLimit = new(4.2f, 5.2f);
    private const float UiFootGap = 0.05f;
    private const float UiHeadGap = 0.05f;
    private const float AttackIconGap = 0.04f;
    private const float AttackIconHeightRatio = 0.12f;
    private const float AttackIconMaxHeight = 0.32f;
    private const float AttackIconScaleMultiplier = 1.2f;
    private const float AttackFrameHorizontalPadding = 0.08f;
    private const float AttackFrameVerticalPadding = 0.04f;
    private const float DamageArrowGap = 0.06f;
    private const float DamageArrowHeightRatio = 1.1f;
    private const float DamageArrowMinimumHeight = 0.22f;
    private static readonly Color DamageUpColor = new(0.25f, 0.95f, 0.32f, 1f);
    private static readonly Color DamageDownColor = new(1f, 0.22f, 0.18f, 1f);

    [SerializeField] private TMP_Text attackText;
    [SerializeField] private Sprite attackIconSprite;

    private readonly List<EnemyMoveData> moves = new();
    private readonly List<StatusEffectType> allowedDebuffs = new();
    private Renderer attackTextRenderer;
    private SpriteRenderer attackIcon;
    private RectTransform attackFrameRect;
    private Image attackFrameImage;
    private Transform damageArrowTransform;
    private EnemyDamageArrowGraphic damageArrow;
    private Vector3 attackIntentAnchorLocal;
    private bool attackIntentAnchorInitialized;
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
        if (attackText != null) attackText.color = Color.red;
        attackTextRenderer = attackText != null ? attackText.GetComponent<Renderer>() : null;

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
        SetupAttackIcon();
        InitializeIdleAnimation();
        CacheAttackIntentAnchor();
        SetupAttackFrame();
        SetupDamageArrow();
        UpdateIntentText();
        UpdateAttackTextAnchorAboveSprite();
        UpdateUIAnchorBelowSprite(UiFootGap);
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
            string attack = GetAttackPreviewDamage().ToString();
            if (CurrentHitCount > 1) attack += " x " + CurrentHitCount;
            lines.Add(attack);
        }

        if (CurrentMoveType == EnemyMoveType.Defend)
            lines.Add("DEFEND");

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
        attackText.ForceMeshUpdate();
    }

    private void Update()
    {
        UpdateIdleAnimation();
    }

    private void LateUpdate()
    {
        UpdateIntentText();
        UpdateDamageArrow();
        UpdateAttackTextAnchorAboveSprite();
        UpdateUIAnchorBelowSprite(UiFootGap);
    }

    private void UpdateAttackTextAnchorAboveSprite()
    {
        if (attackText == null || !attackIntentAnchorInitialized) return;

        Bounds textBounds = attackTextRenderer != null
            ? attackTextRenderer.bounds
            : new Bounds(attackText.transform.position, Vector3.zero);
        bool showAttackIcon = CurrentMoveAttacks && attackIcon != null;
        if (attackIcon != null) attackIcon.enabled = showAttackIcon;
        bool showDamageArrow = CurrentMoveAttacks && damageArrow != null && damageArrow.Renderer.enabled;

        float iconHalfHeight = showAttackIcon ? attackIcon.bounds.extents.y : 0f;
        float textHalfHeight = textBounds.extents.y;
        float textWidth = textBounds.size.x;
        float iconWidth = showAttackIcon ? attackIcon.bounds.size.x : 0f;
        float arrowHeight = showDamageArrow
            ? Mathf.Max(textBounds.size.y * DamageArrowHeightRatio, DamageArrowMinimumHeight)
            : 0f;
        float arrowWidth = showDamageArrow ? arrowHeight * 0.72f : 0f;
        float iconTextGap = showAttackIcon ? AttackIconGap : 0f;
        float textArrowGap = showDamageArrow ? DamageArrowGap : 0f;
        float contentHeight = Mathf.Max(iconHalfHeight, textHalfHeight) * 2f;
        float frameHeight = contentHeight + AttackFrameVerticalPadding * 2f;
        if (showDamageArrow) frameHeight = Mathf.Max(frameHeight, arrowHeight + AttackFrameVerticalPadding * 2f);
        Vector3 anchor = transform.TransformPoint(attackIntentAnchorLocal);
        float labelY = anchor.y + UiHeadGap + frameHeight * 0.5f;
        Vector3 textPosition = attackText.transform.position;
        float groupWidth = textWidth + iconWidth + arrowWidth + iconTextGap + textArrowGap;
        float groupLeft = anchor.x - groupWidth * 0.5f;

        if (showAttackIcon)
        {
            Vector3 iconPosition = attackIcon.transform.position;
            attackIcon.transform.position = new Vector3(
                groupLeft + iconWidth * 0.5f,
                labelY,
                iconPosition.z
            );
        }

        float textCenterX = groupLeft + (showAttackIcon ? iconWidth + iconTextGap : 0f) + textWidth * 0.5f;
        attackText.transform.position = new Vector3(
            textCenterX,
            labelY,
            textPosition.z
        );

        if (damageArrowTransform != null)
        {
            if (showDamageArrow)
            {
                Vector3 parentScale = transform.lossyScale;
                damageArrowTransform.localScale = new Vector3(
                    arrowWidth / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
                    arrowHeight / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
                    1f
                );
                damageArrowTransform.position = new Vector3(
                    textCenterX + textWidth * 0.5f + textArrowGap + arrowWidth * 0.5f,
                    labelY,
                    textPosition.z - 0.01f
                );
            }
        }

        bool showFrame = !string.IsNullOrEmpty(attackText.text);
        if (attackFrameRect != null)
        {
            attackFrameRect.gameObject.SetActive(showFrame);
            if (showFrame)
            {
                Vector3 parentScale = attackFrameRect.parent.lossyScale;
                float frameWidth = groupWidth + AttackFrameHorizontalPadding * 2f;
                attackFrameRect.sizeDelta = new Vector2(
                    frameWidth / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
                    frameHeight / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f)
                );
                attackFrameRect.position = new Vector3(anchor.x, labelY, textPosition.z);
            }
        }
    }

    private void SetupDamageArrow()
    {
        if (attackText == null) return;

        GameObject arrowObject = new("DamageModifierArrow");
        arrowObject.layer = attackText.gameObject.layer;
        arrowObject.transform.SetParent(transform, false);
        damageArrowTransform = arrowObject.transform;
        damageArrow = arrowObject.AddComponent<EnemyDamageArrowGraphic>();
        damageArrow.Initialize(
            attackTextRenderer != null ? attackTextRenderer.sortingLayerID : 0,
            attackTextRenderer != null ? attackTextRenderer.sortingOrder + 2 : 2
        );
        damageArrow.Renderer.enabled = false;
    }

    private void UpdateDamageArrow()
    {
        if (damageArrow == null) return;

        int weakStacks = GetStatusEffectStacks(StatusEffectType.WEAK);
        HeroView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;
        bool isDamageReduced = weakStacks > 0;
        bool isDamageIncreased = hero != null &&
            hero.GetStatusEffectStacks(StatusEffectType.EXPOSED) > 0;
        bool showArrow = CurrentMoveAttacks && (isDamageReduced || isDamageIncreased);

        damageArrow.Renderer.enabled = showArrow;
        if (!showArrow) return;

        damageArrow.SetAppearance(
            isDamageIncreased,
            isDamageIncreased ? DamageUpColor : DamageDownColor
        );
    }

    private void CacheAttackIntentAnchor()
    {
        SpriteRenderer sprite = CombatantSpriteRenderer;
        if (sprite == null || sprite.sprite == null) return;

        Bounds bounds = sprite.bounds;
        Vector3 worldAnchor = new(bounds.center.x, bounds.max.y, bounds.center.z);
        attackIntentAnchorLocal = transform.InverseTransformPoint(worldAnchor);
        attackIntentAnchorInitialized = true;
    }

    private void SetupAttackIcon()
    {
        if (attackIconSprite == null)
        {
            Debug.LogError("Assign the attack icon sprite in the EnemyView prefab.", this);
            return;
        }

        if (attackIcon == null)
        {
            GameObject iconObject = new GameObject("AttackIcon", typeof(SpriteRenderer));
            iconObject.layer = gameObject.layer;
            iconObject.transform.SetParent(transform, false);
            attackIcon = iconObject.GetComponent<SpriteRenderer>();
        }

        attackIcon.sprite = attackIconSprite;
        attackIcon.enabled = false;

        if (attackTextRenderer != null)
        {
            attackIcon.sortingLayerID = attackTextRenderer.sortingLayerID;
            attackIcon.sortingOrder = attackTextRenderer.sortingOrder + 1;
        }

        float desiredHeight = Mathf.Min(
            CombatantSpriteRenderer.bounds.size.y * AttackIconHeightRatio,
            AttackIconMaxHeight
        );
        float parentScaleY = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f);
        float iconScale = desiredHeight / parentScaleY / attackIconSprite.bounds.size.y;
        attackIcon.transform.localScale = Vector3.one * iconScale * AttackIconScaleMultiplier;
    }

    private void SetupAttackFrame()
    {
        if (attackText == null || attackText.transform.parent == null) return;

        if (attackFrameRect == null)
        {
            GameObject frameObject = new GameObject(
                "AttackIntentFrame",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline)
            );
            frameObject.layer = attackText.gameObject.layer;
            frameObject.transform.SetParent(attackText.transform.parent, false);

            attackFrameRect = frameObject.GetComponent<RectTransform>();
            attackFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
            attackFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
            attackFrameRect.pivot = new Vector2(0.5f, 0.5f);
            attackFrameRect.SetAsFirstSibling();

            attackFrameImage = frameObject.GetComponent<Image>();
            attackFrameImage.color = new Color(0.07f, 0.05f, 0.04f, 0.92f);
            attackFrameImage.raycastTarget = false;

            Outline outline = frameObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.67f, 0.34f, 1f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);
        }

        if (attackTextRenderer != null)
        {
            attackTextRenderer.sortingOrder = 2;
        }

        if (attackIcon != null)
            attackIcon.sortingOrder = 3;
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