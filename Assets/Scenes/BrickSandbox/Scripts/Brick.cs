using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class Brick : MonoBehaviour
{
    class BoundCache
    {
        public Bounds aabb;
    }
    BoundCache boundCache = null;
    public Bounds AABB
    {
        get
        {
            if (boundCache == null)
            {
                boundCache = new BoundCache();
                boundCache.aabb = CalBounds();
            }
            return boundCache.aabb;
        }
    }

    public struct BrickMaterialInfo
    {
        public BrickMaterial.MatType type;
        public int offset;
        public int cnt;

        public BrickMaterialInfo(BrickMaterial.MatType type_, int offset_, int cnt_)
        {
            type = type_;
            offset = offset_;
            cnt = cnt_;
        }
    }

    public GameObject prevBrick { get; set; }
    public GameObject nextBrick { get; set; }

    private TransformData originalTd;
    private BrickMaterialInfo originalMat;

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
    }

    public void RestoreTransform()
    {
        transform.Restore(originalTd);
    }

    public void RestoreMaterial()
    {
        ChangeMaterial(originalMat);
    }

    public void ShowSilhouette()
    {
        BrickMaterialInfo silhouetteMat = new BrickMaterialInfo(BrickMaterial.MatType.Silhouette, 0, 1);
        ChangeMaterial(silhouetteMat);
    }

    public bool IsNearlyPositioned(Vector3 pos)
    {
        const float MatchThreshold = LdConstant.LDU_IN_MM;

        Vector3 deltaPos = pos - originalTd.position;
        deltaPos.y = 0;

        return (deltaPos.magnitude < MatchThreshold);
    }

    private void TransformModel(BrickMesh brickMesh)
    {
        Vector3 localPosition;
        Quaternion localRotation;
        Vector3 localScale;

        brickMesh.GetMatrixComponents(out localPosition, out localRotation, out localScale);

        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        transform.localScale = localScale;

        originalTd = transform.Clone();
    }

    private void ChangeMaterial(BrickMaterialInfo matInfo)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Material[] customeMaterial = new Material[matInfo.cnt];
        for (int i = 0; i < matInfo.cnt; ++i)
        {
            customeMaterial[i] = BrickMaterial.Instance.GetMaterial(matInfo.type + matInfo.offset + i);
        }
        renderer.sharedMaterials = customeMaterial;
    }

    private void AddBoxCollider()
    {
        var boxCollider = GetComponent<Renderer>().gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
    }

    public bool ReconstructMesh(GameObject go, BrickMesh brickMesh, short parentBrickColor, bool invertNext)
    {
        TransformModel(brickMesh);

        Mesh mesh = go.GetComponent<MeshFilter>().mesh;
        int matIndexOffset = brickMesh.BfcEnabled ? 0 : (int)BrickMaterial.MatType.DS_OFFSET;

        // The colors will be created.
        Color32[] colors;
        int[] tris;
        bool isColorModified;
        bool isMeshModified;
        bool isMatModified;
        BrickMaterial.MatType matType;

        brickMesh.GetRenderMeshReconstructInfo(parentBrickColor, invertNext, mesh, 
            out colors, out tris, out matType, out isColorModified, out isMeshModified, out isMatModified);

        // assign the array of colors to the Mesh.
        if (isColorModified)
        {
            mesh.colors32 = colors;
        }

        if (isMeshModified)
        {
            mesh.triangles = tris;
            mesh.RecalculateNormals();
        }

        if (isMatModified)
        {
            originalMat = new BrickMaterialInfo(matType, matIndexOffset, mesh.subMeshCount);
            ChangeMaterial(originalMat);
        }

        return true;
    }

    public bool CreateMesh(BrickMesh brickMesh, short parentBrickColor, bool invertNext)
    {
        TransformModel(brickMesh);

        if (brickMesh.Vertices.Count == 0)
            return false;

        Vector3[] vts;
        Color32[] colors;
        int[] opaqueTris;
        int[] transparentTris;
        Mesh mesh = new Mesh();
        int matIndexOffset = brickMesh.BfcEnabled ? 0 : (int)BrickMaterial.MatType.DS_OFFSET;

        brickMesh.GetRenderMeshInfo(parentBrickColor, invertNext,
            out vts, out colors, out opaqueTris, out transparentTris);

        mesh.vertices = vts;
        mesh.colors32 = colors;

        if (opaqueTris.Length > 0 && transparentTris.Length == 0)
        {
            mesh.SetTriangles(opaqueTris, 0);

            originalMat = new BrickMaterialInfo(BrickMaterial.MatType.Opaque, matIndexOffset, 1);
        }
        else if (opaqueTris.Length == 0 && transparentTris.Length > 0)
        {
            mesh.SetTriangles(transparentTris, 0);

            originalMat = new BrickMaterialInfo(BrickMaterial.MatType.Transparent, matIndexOffset, 1);
        }
        else if (opaqueTris.Length > 0 && transparentTris.Length > 0)
        {
            mesh.subMeshCount = 2;
            mesh.SetTriangles(opaqueTris, 0);
            mesh.SetTriangles(transparentTris, 1);

            originalMat = new BrickMaterialInfo(BrickMaterial.MatType.Opaque, matIndexOffset, 2);
        }
        else
        {
            return false;
        }

        ChangeMaterial(originalMat);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshFilter>().sharedMesh = mesh;

        AddBoxCollider();

        return true;
    }

    public Bounds CalBounds()
    {
        Bounds aabb = new Bounds();
        var renderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds localBound = renderers[i].bounds;
            if (i == 0)
                aabb = renderers[i].bounds;
            else
                aabb.Encapsulate(renderers[i].bounds);
        }

        return aabb;
    }

    void Awake()
    {
        prevBrick = null;
        nextBrick = null;
    }

    // Use this for initialization
    void Start () {
    }
	
    // Update is called once per frame
    void Update () {
    }
}
