using System.Collections;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.AttachPerformer<RemoveStatusEffectGA>(RemoveStatusEffectPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.DetachPerformer<RemoveStatusEffectGA>();
    }
    private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA addStatusEffectGA)
    {
        foreach (var target in addStatusEffectGA.Targets)
        {
            if (target == null || target.CurrentHealth <= 0)
            {
                continue;
            }

            target.AddStatusEffect(
                addStatusEffectGA.StatusEffectType,
                addStatusEffectGA.StackCount
            );

            yield return null;
        }
    }
    private IEnumerator RemoveStatusEffectPerformer(
    RemoveStatusEffectGA removeStatusEffectGA
)
    {
        foreach (CombatantView target in removeStatusEffectGA.Targets)
        {
            if (target == null)
            {
                continue;
            }

            target.RemoveStatusEffect(
                removeStatusEffectGA.StatusEffectType,
                removeStatusEffectGA.StackCount
            );

            yield return null;
        }
    }
}
