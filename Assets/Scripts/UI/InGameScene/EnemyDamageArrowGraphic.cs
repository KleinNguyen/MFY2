using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class EnemyDamageArrowGraphic : MonoBehaviour
{
    private MeshRenderer arrowRenderer;
    private Mesh arrowMesh;
    private Material arrowMaterial;
    private bool pointsUp;

    public MeshRenderer Renderer => arrowRenderer;

    public void Initialize(int sortingLayerId, int sortingOrder)
    {
        arrowRenderer = GetComponent<MeshRenderer>();
        arrowRenderer.sortingLayerID = sortingLayerId;
        arrowRenderer.sortingOrder = sortingOrder;
        arrowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        arrowRenderer.receiveShadows = false;

        Shader arrowShader = Shader.Find("Sprites/Default");
        if (arrowShader == null)
        {
            Debug.LogError("Cannot create the enemy damage arrow: Sprites/Default shader was not found.", this);
            return;
        }

        arrowMesh = new Mesh { name = "EnemyDamageArrowMesh" };
        arrowMesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = arrowMesh;

        arrowMaterial = new Material(arrowShader)
        {
            mainTexture = Texture2D.whiteTexture,
            color = Color.white
        };
        arrowRenderer.sharedMaterial = arrowMaterial;
        RebuildMesh(false);
    }

    public void SetAppearance(bool pointsUp, Color color)
    {
        if (arrowMaterial == null || arrowMesh == null) return;

        if (this.pointsUp != pointsUp)
            RebuildMesh(pointsUp);

        arrowMaterial.color = color;
    }

    private void RebuildMesh(bool pointsUp)
    {
        if (arrowMesh == null) return;

        float direction = pointsUp ? -1f : 1f;
        // Build the vertices for the requested arrow direction.
        arrowMesh.Clear();
        arrowMesh.vertices = new[]
        {
            new Vector3(-0.16f, -0.05f * direction, 0f),
            new Vector3(-0.16f, 0.5f * direction, 0f),
            new Vector3(0.16f, 0.5f * direction, 0f),
            new Vector3(0.16f, -0.05f * direction, 0f),
            new Vector3(-0.5f, 0.1f * direction, 0f),
            new Vector3(0.5f, 0.1f * direction, 0f),
            new Vector3(0f, -0.5f * direction, 0f)
        };
        arrowMesh.colors = new[]
        {
            Color.white, Color.white, Color.white, Color.white,
            Color.white, Color.white, Color.white
        };
        arrowMesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6 };
        arrowMesh.RecalculateBounds();
        this.pointsUp = pointsUp;
    }

    private void OnDestroy()
    {
        if (arrowMesh != null) Destroy(arrowMesh);
        if (arrowMaterial != null) Destroy(arrowMaterial);
    }
}
