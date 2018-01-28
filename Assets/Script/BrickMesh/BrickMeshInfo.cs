using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Invariant geometry information of BrickMesh

public class BrickMeshInfo
{
    public string Name { get; private set; }
    public List<Vector3> Vertices { get; set; }
    public List<short> ColorIndices { get; set; }
    public List<int> Triangles { get; private set; }

    public BrickMeshInfo(string meshName)
    {
        Name = meshName;
        ColorIndices = new List<short>();
        Vertices = new List<Vector3>();
        Triangles = new List<int>();
    }

    public void PushTriangle(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, bool renderWinding)
    {
        int lastIndex = Vertices.Count;

        Vertices.Add(v1);
        Vertices.Add(v2);
        Vertices.Add(v3);

        // winding is for RHS so apply reverse for Unity
        if (renderWinding)
        {
            Triangles.Add(lastIndex + 0);
            Triangles.Add(lastIndex + 1);
            Triangles.Add(lastIndex + 2);
        }
        else
        {
            Triangles.Add(lastIndex + 0);
            Triangles.Add(lastIndex + 2);
            Triangles.Add(lastIndex + 1);
        }

        for (int i = 0; i < 3; ++i)
            ColorIndices.Add(vtColorIndex);
    }

    public void PushQuad(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, ref Vector3 v4, bool renderWinding)
    {
        int lastIndex = Vertices.Count;

        Vertices.Add(v1);
        Vertices.Add(v2);
        Vertices.Add(v3);
        Vertices.Add(v4);

        if (renderWinding)
        {
            Triangles.Add(lastIndex + 0);
            Triangles.Add(lastIndex + 1);
            Triangles.Add(lastIndex + 2);

            Triangles.Add(lastIndex + 0);
            Triangles.Add(lastIndex + 2);
            Triangles.Add(lastIndex + 3);
        }
        else
        {
            Triangles.Add(lastIndex + 0);
            Triangles.Add(lastIndex + 2);
            Triangles.Add(lastIndex + 1);

            Triangles.Add(lastIndex + 0);
            Triangles.Add(lastIndex + 3);
            Triangles.Add(lastIndex + 2);
        }

        for (int i = 0; i < 4; ++i)
            ColorIndices.Add(vtColorIndex);
    }

    public void Optimize(float angle = BrickMeshOptimizer.SMOOTH_ANGLE_THRESHOLD_FOR_OPTIMIZE)
    {
        BrickMeshOptimizer.Optimize(this, angle);
    }

    private void GetVertices(out Vector3[] vts)
    {
        vts = Vertices.ToArray();
    }

    private void GetColors(short parentColor, out Color32[] colList)
    {
        colList = new Color32[ColorIndices.Count];

        for (int i = 0; i < ColorIndices.Count; ++i)
        {
            short colorIndex = LdConstant.GetEffectiveColorIndex(ColorIndices[i], parentColor);
            colList[i] = LdColorTable.Instance.GetColor(colorIndex);
        }
    }

    private void GetTriangles(bool inverted,
    ref Color32[] colors, ref List<int> opaqueTris, ref List<int> transparentTris, int offset = 0)
    {
        for (int i = 0; i < Triangles.Count; i += 3)
        {
            int[] vtIndex = new int[3];
            int alphaCnt = 0;

            vtIndex[0] = offset + Triangles[i];
            if (!inverted)
            {
                vtIndex[1] = offset + Triangles[i + 1];
                vtIndex[2] = offset + Triangles[i + 2];
            }
            else
            {
                vtIndex[1] = offset + Triangles[i + 2];
                vtIndex[2] = offset + Triangles[i + 1];
            }

            for (int j = 0; j < 3; ++j)
            {
                if (colors[vtIndex[j]].a < 255)
                    alphaCnt++;
            }

            Debug.Assert(alphaCnt == 0 || alphaCnt == 3,
                string.Format("Alpha count is not zero or three: {0}", alphaCnt));

            if (alphaCnt == 0)
            {
                opaqueTris.Add(vtIndex[0]);
                opaqueTris.Add(vtIndex[1]);
                opaqueTris.Add(vtIndex[2]);
            }
            else if (alphaCnt == 3)
            {
                transparentTris.Add(vtIndex[0]);
                transparentTris.Add(vtIndex[1]);
                transparentTris.Add(vtIndex[2]);
            }
        }
    }

    public void MergeChildBrick(bool accInvertNext, short color, Matrix4x4 trMatrix, BrickMeshInfo childMesh)
    {
        int vtCnt = Vertices.Count;

        for (int i = 0; i < childMesh.Vertices.Count; ++i)
        {
            Vertices.Add(trMatrix.MultiplyPoint3x4(childMesh.Vertices[i]));
        }

        for (int i = 0; i < childMesh.ColorIndices.Count; ++i)
        {
            ColorIndices.Add(LdConstant.GetEffectiveColorIndex(childMesh.ColorIndices[i], color));
        }

        bool inverted = accInvertNext ^ (trMatrix.determinant < 0);

        for (int i = 0; i < childMesh.Triangles.Count; i += 3)
        {
            if (inverted)
            {
                Triangles.Add(vtCnt + childMesh.Triangles[i]);
                Triangles.Add(vtCnt + childMesh.Triangles[i + 2]);
                Triangles.Add(vtCnt + childMesh.Triangles[i + 1]);
            }
            else
            {
                Triangles.Add(vtCnt + childMesh.Triangles[i]);
                Triangles.Add(vtCnt + childMesh.Triangles[i + 1]);
                Triangles.Add(vtCnt + childMesh.Triangles[i + 2]);
            }
        }
    }

    public void GetRenderMeshInfo(short effectiveParentColor, bool inverted,
        out Vector3[] vts, out Color32[] colors, out int[] opaqueTris, out int[] transparentTris)
    {
        GetVertices(out vts);
        GetColors(effectiveParentColor, out colors);

        List<int> opaqueTriList = new List<int>();
        List<int> transparentTriList = new List<int>();
        GetTriangles(inverted, ref colors, ref opaqueTriList, ref transparentTriList);

        opaqueTris = opaqueTriList.ToArray();
        transparentTris = transparentTriList.ToArray();
    }
}
