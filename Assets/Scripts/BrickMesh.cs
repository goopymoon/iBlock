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
    public List<byte> colorIndices { get; set; }
    public List<BrickMesh> children { get; set; }

    public bool invertNext { get; set; }
    public byte brickColor { get; private set; }
    private Matrix4x4 localTr;

    public string brickInfo()
    {
        return string.Format("{0}: {1}", brickColor.ToString(), name);
    }

    public BrickMesh(string meshName)
    {
        name = meshName;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        colorIndices = new List<byte>();
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
        children = rhs.children;

        invertNext = rhs.invertNext;
        brickColor = rhs.brickColor;
        localTr = rhs.localTr;
    }

    public void AddChildBrick(bool invert, byte color, Matrix4x4 trMatrix, BrickMesh child)
    {
        child.invertNext = invert;
        child.brickColor = color;
        child.localTr = trMatrix;

        children.Add(child);
    }

    public void GetMatrixComponents(out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        MatrixUtil.DecomposeMatrix(localTr, out localPosition, out localRotation, out localScale);
    }

    public void GetTriangles(bool invert, ref int[] tris)
    {
        bool invertFlag = invert ^ invertNext ^ (localTr.determinant < 0);
        if (!invertFlag)
        {
            for (int i = 0; i < triangles.Count; i++)
                tris[i] = triangles[i];
        }
        else
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                tris[i] = triangles[i];
                tris[i + 1] = triangles[i + 2];
                tris[i + 2] = triangles[i + 1];
            }
        }
    }

    public void GetColors(LdColorTable colorTable, byte parentColor, ref Color32[] colList)
    {
        for (int i = 0; i < colorIndices.Count; ++i)
        {
            byte colorIndex = (colorIndices[i] == LdConstant.LD_COLOR_MAIN) ? parentColor : colorIndices[i];
            colList[i] = colorTable.GetColor(colorIndex);
        }
    }
}
