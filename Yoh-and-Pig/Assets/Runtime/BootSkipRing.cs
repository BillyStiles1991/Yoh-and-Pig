using UnityEngine;
using UnityEngine.UI;

// Constant Pixel Size canvas: the stroke is measured in screen pixels.
public sealed class BootSkipRing : MaskableGraphic
{
    float progress;
    public float stroke = 3f;

    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (value == progress) return;
        progress = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        float outer = Mathf.Min(rect.width, rect.height) * .5f;
        float inner = Mathf.Max(0, outer - Mathf.Clamp(stroke, 2, 5));
        DrawArc(mesh, rect.center, inner, outer, 1f, new Color32(110, 110, 110, 230));
        DrawArc(mesh, rect.center, inner, outer, progress, new Color32(75, 234, 116, 255));
    }

    static void DrawArc(VertexHelper mesh, Vector2 centre, float inner, float outer, float fraction, Color32 tint)
    {
        if (fraction <= 0) return;
        int segments = Mathf.CeilToInt(96 * fraction);
        int start = mesh.currentVertCount;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (2f * Mathf.PI * fraction * i) / segments;
            Vector2 radial = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            mesh.AddVert(centre + radial * inner, tint, Vector2.zero);
            mesh.AddVert(centre + radial * outer, tint, Vector2.zero);
            if (i == 0) continue;
            int j = start + i * 2;
            mesh.AddTriangle(j-2,j-1,j);
            mesh.AddTriangle(j,j-1,j+1);
        }
    }
}
