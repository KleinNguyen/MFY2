using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CardSystem
{
    private IEnumerator AnimateCardToPile(
        CardView cardView,
        Transform destination,
        float duration,
        float startDelay = 0f,
        System.Action onComplete = null
    )
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        if (
            cardView == null ||
            cardView.Card == null ||
            destination == null
        )
        {
            Debug.LogError(
                "Cannot animate a card to a missing pile or card view.",
                this
            );

            if (cardView != null)
            {
                Destroy(cardView.gameObject);
            }

            if (onComplete != null)
            {
                onComplete();
            }

            yield break;
        }

        Transform cardTransform = cardView.transform;

        Vector3 startPosition = cardTransform.position;
        Vector3 endPosition = destination.position;
        Vector3 startScale = cardTransform.localScale;

        Quaternion startRotation = cardTransform.rotation;
        Quaternion endRotation = destination.rotation;

        SortingGroup sortingGroup =
            cardView.GetComponent<SortingGroup>();

        int sortingLayerId = 0;
        int trailSortingOrder = -1;

        if (sortingGroup != null)
        {
            sortingLayerId = sortingGroup.sortingLayerID;
            trailSortingOrder = sortingGroup.sortingOrder - 1;
        }

        Shader trailShader = Shader.Find("Sprites/Default");

        Material trailMaterial = null;
        GameObject trailObject = null;

        ParticleSystem sparks = null;
        TrailRenderer glowTrail = null;
        TrailRenderer coreTrail = null;

        if (trailShader == null)
        {
            Debug.LogError(
                "Cannot create the card pile flight trail: Sprites/Default shader was not found.",
                this
            );
        }
        else
        {
            trailMaterial = new Material(trailShader);
            trailObject = new GameObject("CardPileFlightTrail");
            trailObject.transform.position = startPosition;

            glowTrail = CreateDiscardTrail(
                trailObject.transform,
                "Glow",
                trailMaterial,
                new Color(1f, 0.72f, 0.08f),
                0.28f,
                0.62f,
                sortingLayerId,
                trailSortingOrder
            );

            coreTrail = CreateDiscardTrail(
                trailObject.transform,
                "Core",
                trailMaterial,
                new Color(1f, 0.96f, 0.62f),
                0.075f,
                0.95f,
                sortingLayerId,
                trailSortingOrder
            );

            sparks = CreateDiscardTrailSparks(
                trailObject,
                trailMaterial,
                sortingLayerId,
                trailSortingOrder
            );
        }

        Tween flightTween = DOTween.To(
            () => 0f,
            progress =>
            {
                Vector3 position = Vector3.Lerp(
                    startPosition,
                    endPosition,
                    progress
                );

                position += Vector3.up *
                    (DiscardArcHeight * 4f * progress * (1f - progress));

                cardTransform.position = position;

                float scaleProgress;
                float scaleMultiplier;

                if (progress <= 0.5f)
                {
                    scaleProgress = Mathf.SmoothStep(
                        0f,
                        1f,
                        progress * 2f
                    );

                    scaleMultiplier = Mathf.Lerp(
                        1f,
                        1.45f,
                        scaleProgress
                    );
                }
                else
                {
                    scaleProgress = Mathf.SmoothStep(
                        0f,
                        1f,
                        (progress - 0.5f) * 2f
                    );

                    scaleMultiplier = Mathf.Lerp(
                        1.45f,
                        0.015f,
                        scaleProgress
                    );
                }

                cardTransform.localScale =
                    startScale * scaleMultiplier;

                Quaternion rotation = Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    progress
                );

                cardTransform.rotation =
                    rotation *
                    Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Sin(progress * Mathf.PI) * 10f
                    );

                if (trailObject != null)
                {
                    trailObject.transform.position = position;
                }
            },
            1f,
            duration
        ).SetEase(Ease.InOutSine);

        yield return flightTween.WaitForCompletion();

        if (glowTrail != null)
        {
            glowTrail.emitting = false;
            coreTrail.emitting = false;

            sparks.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );

            float cleanupDelay = Mathf.Max(
                DiscardTrailLifetime,
                DiscardSparkLifetime
            );

            Destroy(trailObject, cleanupDelay);
            Destroy(trailMaterial, cleanupDelay);
        }

        Destroy(cardView.gameObject);

        if (onComplete != null)
        {
            onComplete();
        }
    }

    private static TrailRenderer CreateDiscardTrail(
        Transform parent,
        string objectName,
        Material material,
        Color color,
        float width,
        float alpha,
        int sortingLayerId,
        int sortingOrder
    )
    {
        GameObject trailObject = new GameObject(objectName);
        trailObject.transform.SetParent(parent, false);

        TrailRenderer trail =
            trailObject.AddComponent<TrailRenderer>();

        trail.time = DiscardTrailLifetime;
        trail.startWidth = width;
        trail.endWidth = width * 0.04f;
        trail.minVertexDistance = 0.02f;
        trail.numCornerVertices = 5;
        trail.numCapVertices = 5;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.autodestruct = false;
        trail.sharedMaterial = material;
        trail.sortingLayerID = sortingLayerId;
        trail.sortingOrder = sortingOrder;

        trail.colorGradient = CreateDiscardTrailGradient(
            color,
            alpha
        );

        trail.Clear();

        return trail;
    }

    private static Gradient CreateDiscardTrailGradient(
        Color color,
        float alpha
    )
    {
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(alpha, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        return gradient;
    }

    private static ParticleSystem CreateDiscardTrailSparks(
        GameObject effectObject,
        Material material,
        int sortingLayerId,
        int sortingOrder
    )
    {
        ParticleSystem sparks =
            effectObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = sparks.main;

        main.loop = true;

        main.startLifetime = new ParticleSystem.MinMaxCurve(
            0.16f,
            DiscardSparkLifetime
        );

        main.startSpeed = new ParticleSystem.MinMaxCurve(
            0.15f,
            0.55f
        );

        main.startSize = new ParticleSystem.MinMaxCurve(
            0.025f,
            0.075f
        );

        main.startColor = new Color(1f, 0.9f, 0.35f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            sparks.colorOverLifetime;

        colorOverLifetime.enabled = true;

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            CreateDiscardTrailGradient(
                new Color(1f, 0.9f, 0.35f),
                1f
            )
        );

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        ParticleSystemRenderer renderer =
            sparks.GetComponent<ParticleSystemRenderer>();

        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;

        sparks.Play();

        return sparks;
    }
}
