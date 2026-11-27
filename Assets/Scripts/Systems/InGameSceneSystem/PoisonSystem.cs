using System.Collections;
using UnityEngine;

public class PoisonSystem : MonoBehaviour
{
    [SerializeField] private GameObject poisonVFX;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<ApplyPoisonGA>(ApplyPoisonPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ApplyPoisonGA>();
    }

    private IEnumerator ApplyPoisonPerformer(ApplyPoisonGA action)
    {
        CombatantView target = action.Target;
        if (target == null || target.CurrentHealth <= 0) yield break;

        int poisonStacks = target.GetStatusEffectStacks(StatusEffectType.POISON);
        if (poisonStacks <= 0) yield break;

        SfxManager.PlayCombatEffectSfx(SfxId.PoisonApplied);

        if (poisonVFX != null)
        {
            Instantiate(poisonVFX, target.transform.position, Quaternion.identity);
        }

        // Each Poison stack deals 1 damage and is consumed in this activation.
        target.Damage(poisonStacks);
        target.RemoveStatusEffect(StatusEffectType.POISON, poisonStacks);

        yield return new WaitForSeconds(1f);
        if (target.CurrentHealth > 0) yield break;

        if (target is EnemyView enemy)
        {
            ActionSystem.Instance.AddReaction(new KillEnemyGA(enemy));
        }
        else if (target is HeroView)
        {
            if (RunSystem.Instance != null) RunSystem.Instance.SaveHealth(0);
            if (BattleSystem.Instance != null) BattleSystem.Instance.Defeat();
        }
    }
}