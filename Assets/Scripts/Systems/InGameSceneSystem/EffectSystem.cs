using System.Collections;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();
    }
    //performer
    private IEnumerator PerformEffectPerformer(
        PerformEffectGA performEffectGA
    )
    {
        CombatantView caster = HeroSystem.Instance.HeroView;

        GameAction effectAction = performEffectGA.Effect.GetGameAction(
            performEffectGA.Targets,
            caster
        );

        if (effectAction == null)
        {
            Debug.LogError("The card effect did not create a GameAction.");

            yield break;
        }
        effectAction.CardCaster = caster;

        ActionSystem.Instance.AddReaction(effectAction);

        yield return null;
    }
}
