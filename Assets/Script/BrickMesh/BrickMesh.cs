using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class BrickMesh
{
    public string Name { get; set; }
    public bool BfcEnabled { get; set; }
    public bool InvertNext { get; set; }
    public short BrickColor { get; private set; }
    public Matrix4x4 LocalTr { get; private set; }
    public List<BrickMesh> Children { get; set; }
    public BrickMeshInfo meshInfo { get; private set; }

    public BrickMesh(string meshName)
    {
        Name = meshName;
        BfcEnabled = false;
        InvertNext = false;
        BrickColor = LdConstant.LD_COLOR_MAIN;
        LocalTr = Matrix4x4.identity;
        meshInfo = BrickMeshManager.Instance.GetBrickMeshInfo(meshName);

        Children = new List<BrickMesh>();
    }

    private BrickMesh(BrickMesh rhs)
    {
        Name = rhs.Name;
        BfcEnabled = rhs.BfcEnabled;
        InvertNext = rhs.InvertNext;
        BrickColor = rhs.BrickColor;
        LocalTr = rhs.LocalTr;
        meshInfo = rhs.meshInfo;

        Children = new List<BrickMesh>();
        foreach (BrickMesh entry in rhs.Children)
        {
            Children.Add(new BrickMesh(entry));
        }
    }

    public static void Create(string meshFName, out BrickMesh brickMesh)
    {
        BrickMesh cache = BrickMeshManager.Instance.GetBrickMesh(meshFName);
        if (cache == null)
        {
            brickMesh = new BrickMesh(meshFName);
            BrickMeshManager.Instance.RegisterBrickMesh(meshFName, brickMesh);
        }
        else
        {
            brickMesh = new BrickMesh(cache);
        }
    }

    public bool IsRegisteredMeshInfo()
    {
        return (meshInfo != null);
    }

    public void CreateMeshInfo()
    {
        meshInfo = new BrickMeshInfo(Name);
    }

    public void RefreshMeshInfo()
    {
        if (meshInfo == null)
            meshInfo = BrickMeshManager.Instance.GetBrickMeshInfo(Name);
    }

    public bool PushTriangle(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, bool renderWinding)
    {
        if (meshInfo == null)
            return false;

        meshInfo.PushTriangle(vtColorIndex, ref v1, ref v2, ref v3, renderWinding);
        return true;
    }

    public bool PushQuad(short vtColorIndex, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, ref Vector3 v4, bool renderWinding)
    {
        if (meshInfo == null)
            return false;

        meshInfo.PushQuad(vtColorIndex, ref v1, ref v2, ref v3, ref v4, renderWinding);
        return true;
    }

    public bool FinalizeBrickMeshInfo(bool bfc)
    {
        if (meshInfo == null)
            return false;

        BfcEnabled = bfc;

        return BrickMeshManager.Instance.RegisterBrickMeshInfo(meshInfo);
    }

    public bool HasVertices()
    {
        return (meshInfo != null ? (meshInfo.Vertices.Count > 0) : false);
    }

    public bool AddChildBrick(BrickMesh child)
    {
        if (child == null)
        {
            Debug.Log(string.Format("child brick is not valid"));
            return false;
        }

        Children.Add(child);
        return true;
    }

    public void SetProperties(Matrix4x4 trMatrix, bool invert, short color)
    {
        LocalTr = trMatrix;
        InvertNext = invert;
        BrickColor = color;
    }


    public void MergeChildBrick(BrickMesh child)
    {
        if (meshInfo != null && child.meshInfo != null)
        {
            if (child.meshInfo.Vertices.Count != 0 || child.Children.Count != 0)
            {
                meshInfo.MergeChildBrick(child.InvertNext, child.BrickColor, child.LocalTr, child.meshInfo);
            }
        }

        BrickMeshManager.Instance.RemoveBrickMesh(child);
    }

    public static void GetMatrixComponents(Matrix4x4 tr, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        MatrixUtil.DecomposeMatrix(tr, out localPosition, out localRotation, out localScale);
    }

    public void GetMatrixComponents(out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        GetMatrixComponents(LocalTr, out localPosition, out localRotation, out localScale);
    }

    public void Optimize(float angle = BrickMeshOptimizer.SMOOTH_ANGLE_THRESHOLD_FOR_OPTIMIZE)
    {
        BrickMeshOptimizer.Optimize(meshInfo, angle);
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

    public void GetRenderMeshInfo(bool invert, short parentBrickColor, 
    out Vector3[] vts, out Color32[] colors, out int[] opaqueTris, out int[] transparentTris)
    {
        short effectiveParentColor = LdConstant.GetEffectiveColorIndex(BrickColor, parentBrickColor);

        if (meshInfo != null)
        {
            bool inverted = invert ^ InvertNext;
            meshInfo.GetRenderMeshInfo(effectiveParentColor, inverted, out vts, out colors, out opaqueTris, out transparentTris);
        }
        else
        {
            vts = null;
            colors = null;
            opaqueTris = null;
            transparentTris = null;
        }
    }

    public void GetRenderMeshReconstructInfo(bool invert, short parentBrickColor, Mesh mesh,
        out Color32[] colors, out int[] tris, out BrickMaterial.MatType matType, out bool isColorModified, out bool isMeshModified, out bool isMatModified)
    {
        short effectiveParentColor = LdConstant.GetEffectiveColorIndex(BrickColor, parentBrickColor);

        isColorModified = GetTobeChangeColors(effectiveParentColor, mesh, out colors);
        isMeshModified = GetTobeChangedTriangles(invert, mesh, ref colors, out tris);
        isMatModified = GetTobeChangedMatInfo(mesh, ref colors, out matType);
    }
}
