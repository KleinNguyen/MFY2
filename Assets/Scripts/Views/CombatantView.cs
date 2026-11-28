using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatantView : MonoBehaviour
{
    private const float IdleCycleDuration = 2.4f;
    private const float IdleSquashAmount = 0.045f;
    private const float IdleWidthAmount = 0.018f;
    private const float ArmorBreakArmorMultiplier = 0.75f;
    private bool armorBreakAppliedToCurrentArmor;

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;
    [SerializeField] private Sprite[] statusEffectOverlaySprites = new Sprite[8];
    private Canvas healthBarCanvas;
    private Canvas statusEffectsCanvas;
    private bool uiAnchorErrorLogged;
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    protected SpriteRenderer CombatantSpriteRenderer => spriteRenderer;
    private Dictionary<StatusEffectType, int> statusEffects = new();
    private GameObject statusEffectOverlayObject;
    private Sequence statusEffectOverlaySequence;
    private Vector3 idleBaseScale;
    private Vector3 idleAnchorInCombatantSpace;
    private bool idleAnimationInitialized;

    public void AlignSpriteBottomTo(CombatantView target)
    {
        if (target == null || spriteRenderer.sprite == null || target.spriteRenderer.sprite == null)
        {
            return;
        }

        float offset = target.spriteRenderer.bounds.min.y - spriteRenderer.bounds.min.y;
        transform.position += Vector3.up * offset;
    }

    protected void SetupBase(int health, Sprite image)
    {
        MaxHealth = CurrentHealth = health;
        spriteRenderer.sprite = image;
        healthBarCanvas = healthFillImage != null
            ? healthFillImage.GetComponentInParent<Canvas>()
            : null;
        statusEffectsCanvas = statusEffectsUI != null
            ? statusEffectsUI.GetComponentInParent<Canvas>()
            : null;
        uiAnchorErrorLogged = false;
        idleAnimationInitialized = false;
        UpdateHealthText();
    }

    protected virtual void OnDisable()
    {
        statusEffectOverlaySequence?.Kill();
        statusEffectOverlaySequence = null;

        if (statusEffectOverlayObject != null)
        {
            Destroy(statusEffectOverlayObject);
            statusEffectOverlayObject = null;
        }
    }

    protected void SetSpriteScale(float scale)
    {
        Vector3 currentScale = spriteRenderer.transform.localScale;
        spriteRenderer.transform.localScale = new Vector3(scale, scale, currentScale.z);
    }

    protected void InitializeIdleAnimation()
    {
        Transform spriteTransform = spriteRenderer.transform;
        idleBaseScale = spriteTransform.localScale;
        idleAnchorInCombatantSpace =
            transform.InverseTransformPoint(
                spriteTransform.TransformPoint(
                    GetSpriteBottomCenter(spriteRenderer.sprite)
                )
            );
        idleAnimationInitialized = true;
    }

    protected void UpdateIdleAnimation()
    {
        if (!idleAnimationInitialized || spriteRenderer.sprite == null)
        {
            return;
        }

        float phase = Time.unscaledTime * (2f * Mathf.PI / IdleCycleDuration);
        float crouch = (1f - Mathf.Cos(phase)) * 0.5f;
        Transform spriteTransform = spriteRenderer.transform;
        spriteTransform.localScale = new Vector3(
            idleBaseScale.x * (1f + IdleWidthAmount * crouch),
            idleBaseScale.y * (1f - IdleSquashAmount * crouch),
            idleBaseScale.z);

        Vector3 targetAnchorWorld = transform.TransformPoint(idleAnchorInCombatantSpace);
        Vector3 currentAnchorWorld =
            spriteTransform.TransformPoint(GetSpriteBottomCenter(spriteRenderer.sprite));
        spriteTransform.position += targetAnchorWorld - currentAnchorWorld;
    }

    protected void UpdateUIAnchorBelowSprite(float gap)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        RectTransform healthBarRect = healthBarCanvas != null
            ? healthBarCanvas.transform as RectTransform
            : null;
        RectTransform statusEffectsRect = statusEffectsCanvas != null
            ? statusEffectsCanvas.transform as RectTransform
            : null;
        if (healthBarRect == null || statusEffectsRect == null)
        {
            if (!uiAnchorErrorLogged)
            {
                Debug.LogError(
                    "Cannot anchor combatant UI: health bar or status effects Canvas is missing.",
                    this
                );
                uiAnchorErrorLogged = true;
            }

            return;
        }

        Vector3 spriteBottom = spriteRenderer.transform.TransformPoint(
            GetSpriteBottomCenter(spriteRenderer.sprite)
        );
        float healthBarHeight =
            healthBarRect.rect.height * Mathf.Abs(healthBarRect.lossyScale.y);
        float statusEffectsHeight =
            statusEffectsRect.rect.height * Mathf.Abs(statusEffectsRect.lossyScale.y);

        Vector3 healthBarPosition = new(
            spriteBottom.x,
            spriteBottom.y - gap - healthBarHeight * 0.5f,
            spriteBottom.z
        );
        healthBarRect.position = healthBarPosition;
        statusEffectsRect.position = new Vector3(
            spriteBottom.x,
            healthBarPosition.y - healthBarHeight * 0.5f - gap - statusEffectsHeight * 0.5f,
            spriteBottom.z
        );
    }

    private static Vector3 GetSpriteBottomCenter(Sprite sprite)
    {
        Vector2[] vertices = sprite.vertices;
        if (vertices.Length == 0)
        {
            Bounds bounds = sprite.bounds;
            return new Vector3(bounds.center.x, bounds.min.y);
        }

        float minX = vertices[0].x;
        float maxX = vertices[0].x;
        float minY = vertices[0].y;

        for (int i = 1; i < vertices.Length; i++)
        {
            Vector2 vertex = vertices[i];
            minX = Mathf.Min(minX, vertex.x);
            maxX = Mathf.Max(maxX, vertex.x);
            minY = Mathf.Min(minY, vertex.y);
        }

        return new Vector3((minX + maxX) * 0.5f, minY);
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = CurrentHealth + " / " + MaxHealth;
        }

        if (healthFillImage != null)
        {
            if (MaxHealth > 0)
            {
                healthFillImage.fillAmount =
                    (float)CurrentHealth / MaxHealth;
            }
            else
            {
                healthFillImage.fillAmount = 0f;
            }
        }
    }
    public void Damage(int damageAmount)
    {
        int remainingDamge = damageAmount;
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);
        if (currentArmor > 0)
        {
            if (currentArmor >= damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, remainingDamge);
                remainingDamge = 0;
            }
            else if (currentArmor < damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                remainingDamge -= currentArmor;
            }
        }
        if (remainingDamge > 0)
        {
            CurrentHealth -= remainingDamge;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }
        transform.DOShakePosition(0.2f, 0.5f);
        UpdateHealthText();
    }

    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (type == StatusEffectType.COMBO)
        {
            stackCount = Mathf.Max(0, stackCount);
            if (stackCount == 0) return;
        }
    
        if (type == StatusEffectType.ARMOR && stackCount > 0 && GetStatusEffectStacks(StatusEffectType.ARMOR) <= 0)
        {
            armorBreakAppliedToCurrentArmor = false;
        }
    
        statusEffects[type] = GetStatusEffectStacks(type) + stackCount;
    
        if (type == StatusEffectType.ARMOR && stackCount > 0 && GetStatusEffectStacks(StatusEffectType.ARMOR_BREAK) > 0)
        {
            ReduceCurrentArmorForArmorBreak();
            armorBreakAppliedToCurrentArmor = true;
        }
    
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    
        if (stackCount <= 0) return;
    
        if (type == StatusEffectType.ARMOR_BREAK)
        {
            ReduceCurrentArmorForArmorBreak();
            armorBreakAppliedToCurrentArmor = true;
        }
    
        ShowStatusEffectOverlay(type);
    }

    private void ReduceCurrentArmorForArmorBreak()
    {
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);
        if (currentArmor <= 0) return;

        int remainingArmor = Mathf.FloorToInt(currentArmor * ArmorBreakArmorMultiplier);
        RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor - remainingArmor);
    }

    public void TickArmorBreak()
    {
        if (CurrentHealth <= 0) return;

        if (GetStatusEffectStacks(StatusEffectType.ARMOR_BREAK) <= 0)
        {
            armorBreakAppliedToCurrentArmor = false;
            return;
        }

        // Avoid reducing the same armor again immediately after application.
        if (!armorBreakAppliedToCurrentArmor)
        {
            ReduceCurrentArmorForArmorBreak();
        }

        armorBreakAppliedToCurrentArmor = false;
        RemoveStatusEffect(StatusEffectType.ARMOR_BREAK, 1);
    }

    private void ShowStatusEffectOverlay(StatusEffectType type)
    {
        int spriteIndex = (int)type;
        if (type == StatusEffectType.COMBO &&
            (statusEffectOverlaySprites == null || spriteIndex >= statusEffectOverlaySprites.Length ||
             statusEffectOverlaySprites[spriteIndex] == null))
        {
            return;
        }
        if (
            statusEffectOverlaySprites == null ||
            spriteIndex < 0 ||
            spriteIndex >= statusEffectOverlaySprites.Length ||
            statusEffectOverlaySprites[spriteIndex] == null
        )
        {
            Debug.LogError(
                "No overlay sprite is assigned for status effect " + type + ".",
                this
            );
            return;
        }

        statusEffectOverlaySequence?.Kill();
        if (statusEffectOverlayObject != null)
        {
            Destroy(statusEffectOverlayObject);
        }

        Sprite overlaySprite = statusEffectOverlaySprites[spriteIndex];
        Bounds characterBounds = spriteRenderer.bounds;
        Bounds overlayBounds = overlaySprite.bounds;
        if (overlayBounds.size.x <= 0f || overlayBounds.size.y <= 0f)
        {
            Debug.LogError(
                "The overlay sprite for status effect " + type + " has invalid bounds.",
                this
            );
            return;
        }

        GameObject overlayObject = new("StatusEffectOverlay_" + type);
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.position = characterBounds.center;

        SpriteRenderer overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        overlayRenderer.sprite = overlaySprite;
        overlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        overlayRenderer.color = new Color(1f, 1f, 1f, 0f);

        float fitScale = Mathf.Max(
            characterBounds.size.x / overlayBounds.size.x,
            characterBounds.size.y / overlayBounds.size.y
        );
        Vector3 parentScale = transform.lossyScale;
        Vector3 targetScale = new(
            fitScale / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
            fitScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
            1f
        );
        overlayObject.transform.localScale = targetScale * 0.88f;
        statusEffectOverlayObject = overlayObject;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(overlayRenderer.DOFade(0.32f, 0.12f));
        sequence.Join(
            overlayObject.transform
                .DOScale(targetScale * 1.08f, 0.22f)
                .SetEase(Ease.OutQuad)
        );
        sequence.AppendInterval(0.6f);
        sequence.Append(overlayRenderer.DOFade(0f, 0.28f));
        sequence.OnComplete(() =>
        {
            if (overlayObject != null)
            {
                Destroy(overlayObject);
            }

            if (statusEffectOverlayObject == overlayObject)
            {
                statusEffectOverlayObject = null;
                statusEffectOverlaySequence = null;
            }
        });
        statusEffectOverlaySequence = sequence.SetLink(overlayObject);
    }

    public IEnumerator PlayHitOverlay(Sprite hitSprite)
    {
        if (hitSprite == null || spriteRenderer == null)
        {
            Debug.LogError("Cannot play hit overlay: sprite or target renderer is missing.", this);
            yield break;
        }

        Bounds targetBounds = spriteRenderer.bounds;
        Bounds hitBounds = hitSprite.bounds;
        if (
            hitBounds.size.x <= 0f ||
            hitBounds.size.y <= 0f ||
            targetBounds.size.x <= 0f ||
            targetBounds.size.y <= 0f
        )
        {
            Debug.LogError("Cannot play hit overlay: the sprite has invalid bounds.", this);
            yield break;
        }

        float worldScale = Mathf.Max(
            targetBounds.size.x / hitBounds.size.x,
            targetBounds.size.y / hitBounds.size.y
        ) * 1.05f;
        Vector3 parentScale = transform.lossyScale;
        Vector3 targetScale = new(
            worldScale / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
            worldScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
            1f
        );

        GameObject hitObject = new("HeroHitOverlay");
        hitObject.transform.SetParent(transform, false);
        hitObject.transform.position = targetBounds.center;
        hitObject.transform.localScale = targetScale * 0.85f;

        GameObject spriteObject = new("Sprite");
        spriteObject.transform.SetParent(hitObject.transform, false);
        spriteObject.transform.localPosition = -hitBounds.center;

        SpriteRenderer hitRenderer = spriteObject.AddComponent<SpriteRenderer>();
        hitRenderer.sprite = hitSprite;
        hitRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        hitRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        hitRenderer.color = new Color(1f, 1f, 1f, 0f);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(hitRenderer.DOFade(0.9f, 0.08f));
        sequence.Join(
            hitObject.transform
                .DOScale(targetScale, 0.08f)
                .SetEase(Ease.OutBack)
        );
        sequence.AppendInterval(0.12f);
        sequence.Append(hitRenderer.DOFade(0f, 0.3f));
        sequence.SetLink(hitObject);
        yield return sequence.WaitForCompletion();
        if (hitObject != null)
        {
            Destroy(hitObject);
        }
    }

    public void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
        if (type == StatusEffectType.COMBO)
        {
            stackCount = Mathf.Max(0, stackCount);
            if (stackCount == 0) return;
        }
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
            {
                statusEffects.Remove(type);
            }
        }
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }
    public int GetStatusEffectStacks(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }
    public void SetCurrentHealth(int health)
    {
        CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        UpdateHealthText();
    }
}
