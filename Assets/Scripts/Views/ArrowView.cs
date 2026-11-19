
using System.Collections.Generic;
using UnityEngine;

public class ArrowView : MonoBehaviour
{
    private const float ArrowBodyRotationOffset = 26.673f;
    private const float ArrowBodySegmentGap = 0.06f;
    private const float ArrowBodySpriteAxisAngle = 63.327f;

  private const float ArrowHeadToBodyRotationOffset = 166.673f;

    private static readonly Vector2 ArrowHeadTipPosition =
        new(0.5f, 0f);

    [SerializeField] private GameObject arrowHead;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Sprite[] arrowBodySprites;
    [SerializeField] private float arrowBodyWidth = 0.45f;
    [SerializeField] private float arrowHeadLength = 0.65f;

    private readonly List<SpriteRenderer> arrowBodyRenderers = new();

    private SpriteRenderer arrowHeadRenderer;
    private Vector3 startPosition;

    private void Awake()
    {
        arrowHeadRenderer = arrowHead.GetComponent<SpriteRenderer>();

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (arrowHeadRenderer != null && lineRenderer != null)
        {
            arrowHeadRenderer.sortingLayerID = lineRenderer.sortingLayerID;
            arrowHeadRenderer.sortingOrder = lineRenderer.sortingOrder + 1;
        }

        if (arrowBodySprites == null || arrowBodySprites.Length == 0)
            Debug.LogError("ArrowView requires arrow body sprites.");
    }

    private void Update()
    {
        UpdateArrow(MouseUtil.GetMousePositionInWorldSpace());
    }

    public void SetupArrow(Vector3 startPosition)
    {
        this.startPosition = startPosition;
        UpdateArrow(MouseUtil.GetMousePositionInWorldSpace());
    }

    private void UpdateArrow(Vector3 endPosition)
    {
        Vector3 arrowStartPosition = new(
            startPosition.x,
            startPosition.y,
            endPosition.z
        );

        Vector3 direction = endPosition - arrowStartPosition;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            SetBodyRenderersActive(0);
            return;
        }

        direction.Normalize();

        float bodyLength = Mathf.Max(0f, distance - arrowHeadLength);

        // Một hướng duy nhất cho toàn bộ mũi tên.
        Quaternion bodyRotation =
            Quaternion.FromToRotation(Vector3.up, direction) *
            Quaternion.Euler(0f, 0f, ArrowBodyRotationOffset);

        UpdateArrowBody(
            arrowStartPosition,
            direction,
            bodyLength,
            bodyRotation
        );

        UpdateArrowHead(endPosition, bodyRotation);
    }

    private void UpdateArrowBody(
        Vector3 arrowStartPosition,
        Vector3 direction,
        float bodyLength,
        Quaternion rotation
    )
    {
        if (bodyLength <= Mathf.Epsilon ||
            arrowBodySprites == null ||
            arrowBodySprites.Length == 0)
        {
            SetBodyRenderersActive(0);
            return;
        }

        Sprite sprite = arrowBodySprites[0];

        if (sprite == null ||
            sprite.bounds.size.x <= Mathf.Epsilon ||
            sprite.bounds.size.y <= Mathf.Epsilon ||
            arrowBodyWidth <= Mathf.Epsilon)
        {
            SetBodyRenderersActive(0);
            return;
        }

        // Giữ nguyên kích thước thân ban đầu.
        float uniformScale = arrowBodyWidth / sprite.bounds.size.x;
        float segmentLength = sprite.bounds.size.y * uniformScale;

        // Các đoạn tách nhau nhưng cùng nằm trên một đường thẳng.
        float segmentPitch = segmentLength + ArrowBodySegmentGap;

        int segmentCount = Mathf.CeilToInt(bodyLength / segmentPitch);

        EnsureBodyRendererCount(segmentCount);

        Vector3 scale = new(uniformScale, uniformScale, 1f);

        Vector3 localCenterOffset =
            Vector3.Scale(sprite.bounds.center, scale);

        for (int i = 0; i < segmentCount; i++)
        {
            float segmentStart = i * segmentPitch;

            // Không tính vị trí dựa trên đoạn trước.
            // Tất cả đều tính từ cùng một điểm bắt đầu.
            Vector3 segmentCenter =
                arrowStartPosition +
                direction * (segmentStart + segmentLength * 0.5f);

            SpriteRenderer renderer = arrowBodyRenderers[i];

            renderer.transform.SetPositionAndRotation(
                segmentCenter - rotation * localCenterOffset,
                rotation
            );

            renderer.transform.localScale = scale;
            renderer.enabled = true;
        }

        SetBodyRenderersActive(segmentCount);
    }

private void UpdateArrowHead(
    Vector3 endPosition,
    Quaternion bodyRotation
)
{
    if (arrowHeadRenderer == null ||
        arrowHeadRenderer.sprite == null)
        return;

    Quaternion rotation =
        bodyRotation *
        Quaternion.Euler(
            0f,
            0f,
            ArrowHeadToBodyRotationOffset
        );

    Bounds bounds = arrowHeadRenderer.sprite.bounds;

    Vector3 tipOffset =
        bounds.min +
        Vector3.Scale(
            bounds.size,
            new Vector3(0.5f, 0f, 0f)
        );

    Vector3 worldTipOffset =
        Vector3.Scale(
            tipOffset,
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

            SpriteRenderer renderer =
                segment.AddComponent<SpriteRenderer>();

            if (lineRenderer != null)
            {
                renderer.sortingLayerID = lineRenderer.sortingLayerID;
                renderer.sortingOrder = lineRenderer.sortingOrder;
            }

            renderer.sprite = arrowBodySprites[0];
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
