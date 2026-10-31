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
    private IEnumerator ApplyPoisonPerformer(ApplyPoisonGA applyPoisonGA)
    {
        CombatantView target = applyPoisonGA.Target;
        if (target == null || target.CurrentHealth <= 0) yield break;
        Instantiate(poisonVFX, target.transform.position, Quaternion.identity);
        target.Damage(applyPoisonGA.PoisonDamage);
        target.RemoveStatusEffect(StatusEffectType.POISON, 1);
        yield return new WaitForSeconds(1f);
        if(target.CurrentHealth <= 0 && target is EnemyView enemyView)
        {
            KillEnemyGA killEnemyGA = new(enemyView);
            ActionSystem.Instance.AddReaction(killEnemyGA);
        }
    }
}
