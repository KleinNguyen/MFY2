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
        Instantiate(poisonVFX, target.transform.position, Quaternion.identity);
        target.Damage(applyPoisonGA.PoisonDamage);
        target.RemoveStatusEffect(StatusEffectType.POISON, 1);
        yield return new WaitForSeconds(1f);
    }
}
