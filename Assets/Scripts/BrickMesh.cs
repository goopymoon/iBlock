using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

[Serializable]
public class BrickMesh
{
    public string name { get; set; }

    public List<Vector3> vertices { get; set; }
    public List<int> triangles { get; set; }
    public List<short> colorIndices { get; set; }
    public List<BrickMesh> children { get; set; }

    public bool invertNext { get; set; }
    public short brickColor { get; private set; }
    private Matrix4x4 localTr;
    private BrickMesh studMesh = null;

    public const byte VERTEX_CNT_PER_STUD = 112;
    private const int VERTEX_CNT_LIMTI_PER_MESH = 65000;

    public string brickInfo()
    {
        return string.Format("{0}: {1}", brickColor.ToString(), name);
    }

    public BrickMesh(string meshName)
    {
        name = meshName;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        colorIndices = new List<short>();
        children = new List<BrickMesh>();

        invertNext = false;
        brickColor = LdConstant.LD_COLOR_MAIN;
        localTr = Matrix4x4.identity;
    }

    public BrickMesh(BrickMesh rhs)
    {
        name = rhs.name;

        vertices = rhs.vertices;
        triangles = rhs.triangles;
        colorIndices = rhs.colorIndices;
        studMesh = rhs.studMesh;
        children = rhs.children;

        invertNext = rhs.invertNext;
        brickColor = rhs.brickColor;
        localTr = rhs.localTr;
    }

    public void AddChildBrick(bool invert, short color, Matrix4x4 trMatrix, BrickMesh child)
    {
        child.invertNext = invert;
        child.brickColor = color;
        child.localTr = trMatrix;

        children.Add(child);
    }

    public void MergeChildBrick(bool invert, short color, Matrix4x4 trMatrix, BrickMesh child, bool isStud)
    {
        if (isStud)
        {
            if (studMesh == null)
                studMesh = new BrickMesh("stud");
            studMesh.MergeChildBrick(invert, color, trMatrix, child);
        }
        else
        {
            MergeChildBrick(invert, color, trMatrix, child);
            if (child.studMesh != null)
            {
                if (studMesh == null)
                    studMesh = new BrickMesh("stud");
                studMesh.MergeChildBrick(invert, color, trMatrix, child.studMesh);
            }
        }
    }

    public void MergeChildBrick(bool invert, short color, Matrix4x4 trMatrix, BrickMesh child)
    {
        int vtCnt = vertices.Count;

        for (int i = 0; i < child.vertices.Count; ++i)
        {
            vertices.Add(trMatrix.MultiplyPoint3x4(child.vertices[i]));
        }

        for (int i = 0; i < child.triangles.Count; i += 3)
        {
            if (invert)
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

        for (int i = 0; i < child.colorIndices.Count; ++i)
        {
            colorIndices.Add(LdConstant.GetEffectiveColorIndex(child.colorIndices[i], color));
        }
    }

    public void Optimize(float angle = BrickMeshOptimizer.SMOOTH_ANGLE_THRESHOLD_FOR_OPTIMIZE)
    {
        BrickMeshOptimizer.Optimize(this, angle);
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

    private void GetTriangles(bool invert, ref List<int> tris, int offset = 0)
    {
        bool invertFlag = invert ^ invertNext;
        invertFlag ^= (localTr.determinant < 0);

        if (!invertFlag)
        {
            for (int i = 0; i < triangles.Count; i++)
                tris.Add(offset + triangles[i]);
        }
        else
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                tris.Add(offset + triangles[i]);
                tris.Add(offset + triangles[i + 2]);
                tris.Add(offset + triangles[i + 1]);
            }
        }
    }

    private void GetColors(LdColorTable colorTable, short parentColor, ref List<Color32> colList)
    {
        for (int i = 0; i < colorIndices.Count; ++i)
        {
            short colorIndex = LdConstant.GetEffectiveColorIndex(colorIndices[i], parentColor);
            colList.Add(colorTable.GetColor(colorIndex));
        }
    }

    public void GetMeshInfo(LdColorTable colorTable, bool invert, short parentBrickColor, 
        ref List<Vector3> vts, ref List<int> tris, ref List<Color32> colors, bool optimizeStud, int maxStudCnt)
    {
        short effectiveParentColor = LdConstant.GetEffectiveColorIndex(brickColor, parentBrickColor);

        vts.Clear();
        tris.Clear();
        colors.Clear();

        GetVertices(ref vts);
        GetTriangles(invert, ref tris);
        GetColors(colorTable, effectiveParentColor, ref colors);

        if (studMesh != null)
        {
            bool drawStud = (vertices.Count + studMesh.vertices.Count > VERTEX_CNT_LIMTI_PER_MESH) ? false : true;
            if (drawStud)
            {
                if (optimizeStud && studMesh.vertices.Count > VERTEX_CNT_PER_STUD * maxStudCnt)
                    drawStud = false;
            }

            if (drawStud)
            {
                studMesh.GetVertices(ref vts);
                studMesh.GetTriangles(invert, ref tris, vertices.Count);
                studMesh.GetColors(colorTable, effectiveParentColor, ref colors);
            }
        }
    }
}
