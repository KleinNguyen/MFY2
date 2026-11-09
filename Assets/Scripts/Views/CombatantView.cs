using DG.Tweening;
using System.Collections.Generic;
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
    [SerializeField] private StatusEffecstUI statusEffectsUI;
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    protected SpriteRenderer CombatantSpriteRenderer => spriteRenderer;
    private Dictionary<StatusEffectType, int> statusEffects = new();
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
        if(currentArmor > 0)
        {
            if(currentArmor>= damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, remainingDamge);
                remainingDamge = 0;
            } else if(currentArmor < damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                remainingDamge -= currentArmor;
            }
        }
        if(remainingDamge > 0)
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
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] += stackCount;
        }
        else
        {
            statusEffects.Add(type, stackCount);
        }
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }
    public void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
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
