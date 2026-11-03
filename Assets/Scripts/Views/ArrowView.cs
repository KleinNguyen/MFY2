using System.Collections.Generic;
using UnityEngine;

public class ArrowView : MonoBehaviour
{
    [SerializeField] private GameObject arrowHead;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Sprite[] arrowBodySprites;
    [SerializeField] private float arrowBodyWidth = 0.45f;
    [SerializeField] private float arrowHeadLength = 0.55f;
    [SerializeField] private float arrowHeadRotationOffset = 155f;

    private readonly List<SpriteRenderer> arrowBodyRenderers = new();
    private SpriteRenderer arrowHeadRenderer;
    private Vector3 startPosition;

    private void Awake()
    {
        arrowHeadRenderer = arrowHead.GetComponent<SpriteRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        if (arrowHeadRenderer != null && lineRenderer != null)
        {
            arrowHeadRenderer.sortingLayerID = lineRenderer.sortingLayerID;
            arrowHeadRenderer.sortingOrder = lineRenderer.sortingOrder + 1;
        }

        if (arrowBodySprites == null || arrowBodySprites.Length == 0)
        {
            Debug.LogError("ArrowView requires arrow body sprites.");
        }
    }

    private void Update()
    {
        Vector3 endPosition = MouseUtil.GetMousePositionInWorldSpace();
        UpdateArrow(endPosition);
    }

    public void SetupArrow(Vector3 startPosition)
    {
        this.startPosition = startPosition;
        UpdateArrow(MouseUtil.GetMousePositionInWorldSpace());
    }

    private void UpdateArrow(Vector3 endPosition)
    {
        Vector3 direction = endPosition - startPosition;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            SetBodyRenderersActive(0);
            return;
        }

        direction /= distance;
        float bodyLength = Mathf.Max(0f, distance - arrowHeadLength);
        UpdateArrowBody(direction, bodyLength);
        UpdateArrowHead(endPosition, direction);
    }

    private void UpdateArrowBody(Vector3 direction, float bodyLength)
    {
        if (bodyLength <= Mathf.Epsilon || arrowBodySprites == null || arrowBodySprites.Length == 0)
        {
            SetBodyRenderersActive(0);
            return;
        }

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        float consumedLength = 0f;
        int segmentCount = 0;

        while (consumedLength < bodyLength)
        {
            Sprite sprite = arrowBodySprites[segmentCount % arrowBodySprites.Length];
            if (sprite == null)
            {
                Debug.LogError("ArrowView has an unassigned arrow body sprite.");
                SetBodyRenderersActive(0);
                return;
            }

            float scale = arrowBodyWidth / sprite.bounds.size.x;
            float segmentLength = Mathf.Min(
                sprite.bounds.size.y * scale,
                bodyLength - consumedLength
            );

            consumedLength += segmentLength;
            segmentCount++;
        }

        EnsureBodyRendererCount(segmentCount);

        consumedLength = 0f;
        for (int i = 0; i < segmentCount; i++)
        {
            Sprite sprite = arrowBodySprites[i % arrowBodySprites.Length];
            float uniformScale = arrowBodyWidth / sprite.bounds.size.x;
            float segmentLength = Mathf.Min(
                sprite.bounds.size.y * uniformScale,
                bodyLength - consumedLength
            );
            Vector3 scale = new(
                uniformScale,
                segmentLength / sprite.bounds.size.y,
                1f
            );

            float segmentCenterOffset = consumedLength + segmentLength * 0.5f;
            Vector3 segmentCenter = startPosition + direction * segmentCenterOffset;
            Vector3 localCenterOffset = Vector3.Scale(sprite.bounds.center, scale);
            SpriteRenderer renderer = arrowBodyRenderers[i];

            renderer.transform.SetPositionAndRotation(
                segmentCenter - rotation * localCenterOffset,
                rotation
            );
            renderer.transform.localScale = scale;
            renderer.enabled = true;

            consumedLength += segmentLength;
        }

        SetBodyRenderersActive(segmentCount);
    }

    private void UpdateArrowHead(Vector3 endPosition, Vector3 direction)
    {
        if (arrowHeadRenderer == null || arrowHeadRenderer.sprite == null)
        {
            Debug.LogError("ArrowView Head requires a SpriteRenderer with a sprite.");
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            + arrowHeadRotationOffset;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector3 spriteSize = arrowHeadRenderer.sprite.bounds.size;
        Vector3 estimatedTipOffset = new(
            spriteSize.x * 0.35f,
            spriteSize.y * 0.02f,
            0f
        );
        Vector3 worldTipOffset = Vector3.Scale(
            estimatedTipOffset,
            arrowHeadRenderer.transform.lossyScale
        );

        arrowHead.transform.SetPositionAndRotation(
            endPosition - rotation * worldTipOffset,
            rotation
        );
    }

    private void EnsureBodyRendererCount(int count)
    {
        while (arrowBodyRenderers.Count < count)
        {
            GameObject segment = new("ArrowBodySegment");
            segment.transform.SetParent(transform, false);

            SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = lineRenderer.sortingLayerID;
            renderer.sortingOrder = lineRenderer.sortingOrder;
            renderer.sprite = arrowBodySprites[arrowBodyRenderers.Count % arrowBodySprites.Length];
            arrowBodyRenderers.Add(renderer);
        }
    }

    private void SetBodyRenderersActive(int activeCount)
    {
        for (int i = 0; i < arrowBodyRenderers.Count; i++)
        {
            arrowBodyRenderers[i].enabled = i < activeCount;
        }
    }
}
