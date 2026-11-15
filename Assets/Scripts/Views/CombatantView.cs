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

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;
    [SerializeField] private Sprite[] statusEffectOverlaySprites = new Sprite[8];
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    protected SpriteRenderer CombatantSpriteRenderer => spriteRenderer;
    private Dictionary<StatusEffectType, int> statusEffects = new();
    private GameObject statusEffectOverlayObject;
    private Sequence statusEffectOverlaySequence;
    private Vector3 idleBaseScale;
    private Vector3 idleAnchorInCombatantSpace;
    private bool idleAnimationInitialized;

    protected void SetupBase(int health, Sprite image)
    {
        MaxHealth = CurrentHealth = health;
        spriteRenderer.sprite = image;
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
            transform.InverseTransformPoint(spriteTransform.TransformPoint(GetSpriteBottomCenter(spriteRenderer.sprite)));
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
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] += stackCount;
        }
        else
        {
            statusEffects.Add(type, stackCount);
        }
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
        if (stackCount > 0)
        {
            ShowStatusEffectOverlay(type);
        }
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
