using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CardexFlightTrailGraphic : MaskableGraphic
{
    private static readonly float[] CrossSection = { -1f, -0.42f, 0f, 0.42f, 1f };
    private static readonly float[] EdgeAlpha = { 0f, 0.5f, 1f, 0.5f, 0f };

    private Vector2[] points;
    private float opacity = 1f;

    public float Opacity
    {
        get => opacity;
        set
        {
            opacity = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    public void SetPath(Vector2[] path)
    {
        points = path;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        if (points == null || points.Length < 2) return;

        AddRibbon(vertexHelper, 44f, new Color(1f, 0.43f, 0.015f, 0.95f));
        AddRibbon(vertexHelper, 13f, new Color(1f, 0.94f, 0.55f, 1f));
    }

    private void AddRibbon(VertexHelper vertexHelper, float halfWidth, Color tint)
    {
        int firstVertex = vertexHelper.currentVertCount;
        int pointCount = points.Length;

        for (int i = 0; i < pointCount; i++)
        {
            Vector2 previous = points[Mathf.Max(0, i - 1)];
            Vector2 next = points[Mathf.Min(pointCount - 1, i + 1)];
            Vector2 tangent = next - previous;
            Vector2 normal = tangent.sqrMagnitude > 0.0001f
                ? new Vector2(-tangent.y, tangent.x).normalized
                : Vector2.up;
            float alongTrail = (i + 1f) / pointCount;
            float taper = Mathf.Lerp(0.35f, 1f, alongTrail);

            for (int j = 0; j < CrossSection.Length; j++)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vertex.position = points[i] + normal * (CrossSection[j] * halfWidth * taper);
                vertex.color = new Color(
                    tint.r,
                    tint.g,
                    tint.b,
                    tint.a * EdgeAlpha[j] * alongTrail * opacity
                );
                vertexHelper.AddVert(vertex);
            }
        }

        for (int i = 0; i < pointCount - 1; i++)
        {
            for (int j = 0; j < CrossSection.Length - 1; j++)
            {
                int current = firstVertex + i * CrossSection.Length + j;
                int next = current + CrossSection.Length;
                vertexHelper.AddTriangle(current, next, current + 1);
                vertexHelper.AddTriangle(current + 1, next, next + 1);
            }
        }
    }
}
