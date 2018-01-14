using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

[Serializable]
public class BrickMesh
{
    public GameObjId Id { get; set; } 
    public string Name { get; set; }
    public bool BfcEnabled { get; set; }
    public bool InvertNext { get; set; }
    public short BrickColor { get; private set; }
    public bool IsPartAsset { get; private set; }

    public List<Vector3> Vertices { get; set; }
    public List<short> ColorIndices { get; set; }
    public List<int> Triangles { get; set; }
    public List<GameObjId> Children { get; set; }

    private Matrix4x4 localTr;

    public BrickMesh(string meshName, bool isAsset=false)
    {
        Id = BrickMeshManager.Instance.Register(this);

        Name = meshName;
        IsPartAsset = isAsset;

        BfcEnabled = false;
        InvertNext = false;
        BrickColor = LdConstant.LD_COLOR_MAIN;
        localTr = Matrix4x4.identity;

        Vertices = new List<Vector3>();
        ColorIndices = new List<short>();
        Triangles = new List<int>();
        Children = new List<GameObjId>();
    }

    public BrickMesh(BrickMesh rhs, bool isDuplicate = false)
    {
        Id = isDuplicate ? BrickMeshManager.Instance.Register(this) : rhs.Id;

        Name = rhs.Name;
        IsPartAsset = rhs.IsPartAsset;

        BfcEnabled = rhs.BfcEnabled;
        InvertNext = rhs.InvertNext;
        BrickColor = rhs.BrickColor;
        localTr = rhs.localTr;

        Vertices = rhs.Vertices;
        ColorIndices = rhs.ColorIndices;
        Triangles = rhs.Triangles;
        Children = rhs.Children;
    }

    public void PushTriangle(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, bool renderWinding)
    {
        int lastIndex = Vertices.Count;

        Vertices.Add(v1);
        Vertices.Add(v2);
        Vertices.Add(v3);

        ColorIndices.Add(vtColorIndex);
        ColorIndices.Add(vtColorIndex);
        ColorIndices.Add(vtColorIndex);

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
    }

    public void PushQuad(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, ref Vector3 v4, bool renderWinding)
    {
        int lastIndex = Vertices.Count;

        Vertices.Add(v1);
        Vertices.Add(v2);
        Vertices.Add(v3);
        Vertices.Add(v4);

        ColorIndices.Add(vtColorIndex);
        ColorIndices.Add(vtColorIndex);
        ColorIndices.Add(vtColorIndex);
        ColorIndices.Add(vtColorIndex);

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
    }

    public void Optimize(float angle = BrickMeshOptimizer.SMOOTH_ANGLE_THRESHOLD_FOR_OPTIMIZE)
    {
        BrickMeshOptimizer.Optimize(this, angle);
    }

    public bool AddChildBrick(GameObjId childId)
    {
        if (!childId.IsValid())
        {
            Debug.Log(string.Format("child brick id is not valid"));
            return false;
        }

        Children.Add(childId);
        return true;
    }

    public void SetProperties(bool invert, short color, Matrix4x4 trMatrix)
    {
        InvertNext = invert;
        BrickColor = color;
        localTr = trMatrix;
    }

    public void MergeChildBrick(bool invert, bool invertByMatrix, short color, Matrix4x4 trMatrix, BrickMesh child)
    {
        if (child.Vertices.Count != 0 || child.Children.Count != 0)
        {
            bool inverted = invert ^ invertByMatrix;
            int vtCnt = Vertices.Count;

            for (int i = 0; i < child.Vertices.Count; ++i)
            {
                Vertices.Add(trMatrix.MultiplyPoint3x4(child.Vertices[i]));
            }

            for (int i = 0; i < child.ColorIndices.Count; ++i)
            {
                ColorIndices.Add(LdConstant.GetEffectiveColorIndex(child.ColorIndices[i], color));
            }

            for (int i = 0; i < child.Triangles.Count; i += 3)
            {
                if (inverted)
                {
                    Triangles.Add(vtCnt + child.Triangles[i]);
                    Triangles.Add(vtCnt + child.Triangles[i + 2]);
                    Triangles.Add(vtCnt + child.Triangles[i + 1]);
                }
                else
                {
                    Triangles.Add(vtCnt + child.Triangles[i]);
                    Triangles.Add(vtCnt + child.Triangles[i + 1]);
                    Triangles.Add(vtCnt + child.Triangles[i + 2]);
                }
            }
        }

        BrickMeshManager.Instance.Remove(child.Id);
    }

    public void GetMatrixComponents(out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        MatrixUtil.DecomposeMatrix(localTr, out localPosition, out localRotation, out localScale);
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

    private void GetTriangles(bool invert,
    ref Color32[] colors, ref List<int> opaqueTris, ref List<int> transparentTris, int offset = 0)
    {
        bool invertFlag = invert ^ InvertNext;

        for (int i = 0; i < Triangles.Count; i += 3)
        {
            int[] vtIndex = new int[3];
            int alphaCnt = 0;

            vtIndex[0] = offset + Triangles[i];
            if (!invertFlag)
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

    public bool GetTobeChangeColors(short parentColor, Mesh mesh, out Color32[] colList)
    {
        bool isModified = false;

        colList = new Color32[mesh.colors.Length];

        for (int i = 0; i < mesh.colors.Length; ++i)
        {
            short restoreIndex = (short)LdColorTable.Instance.GetColorIndex(mesh.colors[i]);
            short colorIndex = LdConstant.GetEffectiveColorIndex(restoreIndex, parentColor);
            colList[i] = LdColorTable.Instance.GetColor(colorIndex);

            isModified |= (mesh.colors[i] != colList[i]);
        }

        return isModified;
    }

    public bool GetTobeChangedTriangles(bool invert, Mesh mesh, ref Color32[] colors, out int[] tris)
    {
        bool invertFlag = invert ^ InvertNext;

        if (invertFlag)
        {
            tris = new int[mesh.triangles.Length];

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                if (!invertFlag)
                {
                    tris[i] = mesh.triangles[i];
                    tris[i + 1] = mesh.triangles[i + 1];
                    tris[i + 2] = mesh.triangles[i + 2];
                }
                else
                {
                    tris[i] = mesh.triangles[i];
                    tris[i + 1] = mesh.triangles[i + 2];
                    tris[i + 2] = mesh.triangles[i + 1];
                }
            }
        }
        else
        {
            tris = null;
        }

        return invertFlag;
    }

    public bool GetTobeChangedMatInfo(Mesh mesh, ref Color32[] colors, out BrickMaterial.MatType matType)
    {
        bool isModified = false;

        matType = (colors.Length > 0 && colors[0].a < 255)
            ? BrickMaterial.MatType.Transparent : BrickMaterial.MatType.Opaque;

        if (mesh.subMeshCount == 1)
        {
            isModified = (mesh.colors32[0].a != colors[0].a);
        }
        else if (mesh.subMeshCount == 2)
        {
            for (int i = 0; i < mesh.colors32.Length; i++)
            {
                if (colors[i].a != mesh.colors32[i].a)
                {
                    isModified = true;
                }
            }
        }

        return isModified;
    }

    public void GetRenderMeshInfo(short parentBrickColor, bool invert, 
        out Vector3[] vts, out Color32[] colors, out int[] opaqueTris, out int[] transparentTris)
    {
        short effectiveParentColor = LdConstant.GetEffectiveColorIndex(BrickColor, parentBrickColor);

        GetVertices(out vts);
        GetColors(effectiveParentColor, out colors);

        List<int> opaqueTriList = new List<int>();
        List<int> transparentTriList = new List<int>();
        GetTriangles(invert, ref colors, ref opaqueTriList, ref transparentTriList);

        opaqueTris = opaqueTriList.ToArray();
        transparentTris = transparentTriList.ToArray();
    }

    public void GetRenderMeshReconstructInfo(short parentBrickColor, bool invert, Mesh mesh,
        out Color32[] colors, out int[] tris, out BrickMaterial.MatType matType, out bool isColorModified, out bool isMeshModified, out bool isMatModified)
    {
        short effectiveParentColor = LdConstant.GetEffectiveColorIndex(BrickColor, parentBrickColor);

        isColorModified = GetTobeChangeColors(effectiveParentColor, mesh, out colors);
        isMeshModified = GetTobeChangedTriangles(invert, mesh, ref colors, out tris);
        isMatModified = GetTobeChangedMatInfo(mesh, ref colors, out matType);
    }
}
