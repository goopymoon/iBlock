using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

[Serializable]
public class BrickMesh
{
    public string name { get; set; }
    public bool bfcEnabled { get; set; }
    public bool invertNext { get; set; }
    public short brickColor { get; private set; }
    public bool IsPartAsset { get; private set; }

    public List<Vector3> vertices { get; set; }
    public List<short> colorIndices { get; set; }
    public List<int> triangles { get; set; }
    public List<BrickMesh> children { get; set; }

    private Matrix4x4 localTr;

    public string brickInfo()
    {
        return string.Format("{0}:{1}: {2}", bfcEnabled, brickColor.ToString(), name);
    }

    public BrickMesh(string meshName, bool isAsset=false)
    {
        name = meshName;
        IsPartAsset = isAsset;

        bfcEnabled = false;
        invertNext = false;
        brickColor = LdConstant.LD_COLOR_MAIN;
        localTr = Matrix4x4.identity;

        vertices = new List<Vector3>();
        colorIndices = new List<short>();
        triangles = new List<int>();
        children = new List<BrickMesh>();
    }

    public BrickMesh(BrickMesh rhs)
    {
        name = rhs.name;
        IsPartAsset = rhs.IsPartAsset;

        bfcEnabled = rhs.bfcEnabled;
        invertNext = rhs.invertNext;
        brickColor = rhs.brickColor;
        localTr = rhs.localTr;

        vertices = rhs.vertices;
        colorIndices = rhs.colorIndices;
        triangles = rhs.triangles;
        children = rhs.children;
    }

    public void PushTriangle(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, bool renderWinding)
    {
        int lastIndex = vertices.Count;

        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        colorIndices.Add(vtColorIndex);
        colorIndices.Add(vtColorIndex);
        colorIndices.Add(vtColorIndex);

        // winding is for RHS so apply reverse for Unity
        if (renderWinding)
        {
            triangles.Add(lastIndex + 0);
            triangles.Add(lastIndex + 1);
            triangles.Add(lastIndex + 2);
        }
        else
        {
            triangles.Add(lastIndex + 0);
            triangles.Add(lastIndex + 2);
            triangles.Add(lastIndex + 1);
        }
    }

    public void PushQuad(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, ref Vector3 v4, bool renderWinding)
    {
        int lastIndex = vertices.Count;

        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);

        colorIndices.Add(vtColorIndex);
        colorIndices.Add(vtColorIndex);
        colorIndices.Add(vtColorIndex);
        colorIndices.Add(vtColorIndex);

        if (renderWinding)
        {
            triangles.Add(lastIndex + 0);
            triangles.Add(lastIndex + 1);
            triangles.Add(lastIndex + 2);

            triangles.Add(lastIndex + 0);
            triangles.Add(lastIndex + 2);
            triangles.Add(lastIndex + 3);
        }
        else
        {
            triangles.Add(lastIndex + 0);
            triangles.Add(lastIndex + 2);
            triangles.Add(lastIndex + 1);

            triangles.Add(lastIndex + 0);
            triangles.Add(lastIndex + 3);
            triangles.Add(lastIndex + 2);
        }
    }

    public void Optimize(float angle = BrickMeshOptimizer.SMOOTH_ANGLE_THRESHOLD_FOR_OPTIMIZE)
    {
        BrickMeshOptimizer.Optimize(this, angle);
    }

    public void AddChildBrick(BrickMesh child)
    {
        children.Add(child);
    }

    public void AddChildBrick(bool invert, short color, Matrix4x4 trMatrix, BrickMesh child)
    {
        child.invertNext = invert;
        child.brickColor = color;
        child.localTr = trMatrix;

        children.Add(child);
    }

    public void MergeChildBrick(bool invert, bool invertByMatrix, short color, Matrix4x4 trMatrix, BrickMesh child)
    {
        if (child.vertices.Count == 0 && child.children.Count == 0)
            return;

        bool inverted = invert ^ invertByMatrix;
        int vtCnt = vertices.Count;

        for (int i = 0; i < child.vertices.Count; ++i)
        {
            vertices.Add(trMatrix.MultiplyPoint3x4(child.vertices[i]));
        }

        for (int i = 0; i < child.colorIndices.Count; ++i)
        {
            colorIndices.Add(LdConstant.GetEffectiveColorIndex(child.colorIndices[i], color));
        }

        for (int i = 0; i < child.triangles.Count; i += 3)
        {
            if (inverted)
            {
                triangles.Add(vtCnt + child.triangles[i]);
                triangles.Add(vtCnt + child.triangles[i + 2]);
                triangles.Add(vtCnt + child.triangles[i + 1]);
            }
            else
            {
                triangles.Add(vtCnt + child.triangles[i]);
                triangles.Add(vtCnt + child.triangles[i + 1]);
                triangles.Add(vtCnt + child.triangles[i + 2]);
            }
        }
    }

    public void GetMatrixComponents(out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        MatrixUtil.DecomposeMatrix(localTr, out localPosition, out localRotation, out localScale);
    }

    private void GetVertices(ref List<Vector3> vts)
    {
        for (int i = 0; i < vertices.Count; ++i)
            vts.Add(vertices[i]);
    }

    private void GetColors(short parentColor, ref List<Color32> colList)
    {
        for (int i = 0; i < colorIndices.Count; ++i)
        {
            short colorIndex = LdConstant.GetEffectiveColorIndex(colorIndices[i], parentColor);
            colList.Add(LdColorTable.Instance.GetColor(colorIndex));
        }
    }

    private void GetTriangles(bool invert, List<Color32> colors, ref List<int> opaqueTris, ref List<int> transparentTris, int offset = 0)
    {
        bool invertFlag = invert ^ invertNext;

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int[] vtIndex = new int[3];
            int alphaCnt = 0;

            vtIndex[0] = offset + triangles[i];
            if (!invertFlag)
            {
                vtIndex[1] = offset + triangles[i + 1];
                vtIndex[2] = offset + triangles[i + 2];
            }
            else
            {
                vtIndex[1] = offset + triangles[i + 2];
                vtIndex[2] = offset + triangles[i + 1];
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

    public void GetRenderMeshInfo(short parentBrickColor, bool invert, 
        ref List<Vector3> vts, ref List<Color32> colors, ref List<int> opaqueTris, ref List<int> transparentTris)
    {
        short effectiveParentColor = LdConstant.GetEffectiveColorIndex(brickColor, parentBrickColor);

        vts.Clear();
        colors.Clear();
        opaqueTris.Clear();
        transparentTris.Clear();

        GetVertices(ref vts);
        GetColors(effectiveParentColor, ref colors);
        GetTriangles(invert, colors, ref opaqueTris, ref transparentTris);
    }
}
