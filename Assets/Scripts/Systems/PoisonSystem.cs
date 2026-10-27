using System.Collections;
using UnityEngine;

public class BurnSystem : MonoBehaviour
{
    [SerializeField] private GameObject poisonVFX;
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<ApplyPoisonGA>(ApplyBurnPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ApplyPoisonGA>();
    }
    private IEnumerator ApplyBurnPerformer(ApplyPoisonGA applyPoisonGA)
    {
        CombatantView target = applyPoisonGA.Target;
        Instantiate(poisonVFX, target.transform.position, Quaternion.identity);
        target.Damage(applyPoisonGA.PoisonDamage);
        target.RemoveStatusEffect(StatusEffectType.POISON, 1);
        yield return new WaitForSeconds(1f);
    }
}
