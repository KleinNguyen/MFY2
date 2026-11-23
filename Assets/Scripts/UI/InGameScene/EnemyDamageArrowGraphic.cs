using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class EnemyDamageArrowGraphic : MaskableGraphic
{
    private bool pointsUp = true;

    public bool PointsUp
    {
        get => pointsUp;
        set
        {
            if (pointsUp == value) return;
            pointsUp = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float shaftLeft = Mathf.Lerp(rect.xMin, rect.xMax, 0.34f);
        float shaftRight = Mathf.Lerp(rect.xMin, rect.xMax, 0.66f);
        float shaftStart = Mathf.Lerp(rect.yMin, rect.yMax, 0.42f);
        float shaftEnd = Mathf.Lerp(rect.yMin, rect.yMax, 0.58f);

        if (pointsUp)
        {
            AddQuad(vertexHelper, shaftLeft, rect.yMin, shaftRight, shaftEnd);
            AddTriangle(
                vertexHelper,
                new Vector2(rect.xMin, shaftStart),
                new Vector2(rect.xMax, shaftStart),
                new Vector2(rect.center.x, rect.yMax)
            );
        }
        else
        {
            AddQuad(vertexHelper, shaftLeft, shaftStart, shaftRight, rect.yMax);
            AddTriangle(
                vertexHelper,
                new Vector2(rect.xMin, shaftEnd),
                new Vector2(rect.xMax, shaftEnd),
                new Vector2(rect.center.x, rect.yMin)
            );
        }
    }

    private void AddQuad(VertexHelper vertexHelper, float left, float bottom, float right, float top)
    {
        int firstVertex = vertexHelper.currentVertCount;
        AddVertex(vertexHelper, new Vector2(left, bottom));
        AddVertex(vertexHelper, new Vector2(left, top));
        AddVertex(vertexHelper, new Vector2(right, top));
        AddVertex(vertexHelper, new Vector2(right, bottom));
        vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
        vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
    }

    private void AddTriangle(VertexHelper vertexHelper, Vector2 first, Vector2 second, Vector2 third)
    {
        int firstVertex = vertexHelper.currentVertCount;
        AddVertex(vertexHelper, first);
        AddVertex(vertexHelper, second);
        AddVertex(vertexHelper, third);
        vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
    }

    private void AddVertex(VertexHelper vertexHelper, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vertexHelper.AddVert(vertex);
    }
}
