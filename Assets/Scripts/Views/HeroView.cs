using System.Collections;
using DG.Tweening;
using UnityEngine;

public class HeroView : CombatantView
{
    [SerializeField, Min(0.1f)] private float inGameSpriteScale = 1f;

    private Sequence punchSequence;
    private Vector3 punchBasePosition;
    private Vector3 punchBaseScale;
    private Vector3 punchBaseEulerAngles;
    private bool punchAnimationPlaying;
    private bool punchPoseInitialized;

    public void Setup(HeroData heroData)
    {
        SetupBase(heroData.Health, heroData.Image);
        SetSpriteScale(inGameSpriteScale);
        InitializeIdleAnimation();

        Transform spriteTransform = CombatantSpriteRenderer.transform;
        punchBasePosition = spriteTransform.localPosition;
        punchBaseScale = spriteTransform.localScale;
        punchBaseEulerAngles = spriteTransform.localEulerAngles;
        punchPoseInitialized = true;
    }

    private void Update()
    {
        if (!punchAnimationPlaying)
        {
            UpdateIdleAnimation();
        }
    }

    protected override void OnDisable()
    {
        punchSequence?.Kill();
        punchSequence = null;
        punchAnimationPlaying = false;
        RestorePunchPose();
        base.OnDisable();
    }

    public IEnumerator PlayPunchAnimation()
    {
        Transform spriteTransform = CombatantSpriteRenderer.transform;
        punchAnimationPlaying = true;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(
            spriteTransform
                .DOLocalMove(
                    punchBasePosition + new Vector3(-0.2f, -0.1f, 0f),
                    0.12f
                )
                .SetEase(Ease.InQuad)
        );
        sequence.Join(
            spriteTransform.DOLocalRotate(
                punchBaseEulerAngles + new Vector3(0f, 0f, 12f),
                0.12f
            )
        );
        sequence.Append(
            spriteTransform
                .DOLocalMove(
                    punchBasePosition + new Vector3(0.9f, 0.12f, 0f),
                    0.08f
                )
                .SetEase(Ease.OutExpo)
        );
        sequence.Join(
            spriteTransform.DOLocalRotate(
                punchBaseEulerAngles + new Vector3(0f, 0f, -15f),
                0.08f
            )
        );
        sequence.Join(
            spriteTransform.DOScale(
                Vector3.Scale(punchBaseScale, new Vector3(1.12f, 0.88f, 1f)),
                0.08f
            )
        );
        sequence.AppendInterval(0.04f);
        sequence.Append(
            spriteTransform
                .DOLocalMove(
                    punchBasePosition + new Vector3(-0.12f, 0f, 0f),
                    0.07f
                )
                .SetEase(Ease.InQuad)
        );
        sequence.Join(
            spriteTransform.DOLocalRotate(
                punchBaseEulerAngles + new Vector3(0f, 0f, 7f),
                0.07f
            )
        );
        sequence.Append(
            spriteTransform
                .DOLocalMove(punchBasePosition, 0.2f)
                .SetEase(Ease.OutBack)
        );
        sequence.Join(
            spriteTransform.DOLocalRotate(punchBaseEulerAngles, 0.2f)
        );
        sequence.Join(spriteTransform.DOScale(punchBaseScale, 0.2f));

        punchSequence = sequence.SetLink(gameObject);
        yield return sequence.WaitForCompletion();

        punchSequence = null;
        punchAnimationPlaying = false;
        RestorePunchPose();
        InitializeIdleAnimation();
    }

    private void RestorePunchPose()
    {
        if (!punchPoseInitialized || CombatantSpriteRenderer == null)
        {
            return;
        }

        Transform spriteTransform = CombatantSpriteRenderer.transform;
        spriteTransform.localPosition = punchBasePosition;
        spriteTransform.localScale = punchBaseScale;
        spriteTransform.localEulerAngles = punchBaseEulerAngles;
    }
}
