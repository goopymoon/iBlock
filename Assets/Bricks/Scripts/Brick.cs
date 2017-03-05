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

    public GameObject prevBrick { get; set; }
    public GameObject nextBrick { get; set; }

    private TransformData originalTd;

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
    }

    public void RestoreTransform()
    {
        transform.Restore(originalTd);
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

    private void ChangeMaterial(BrickMaterial.MatType matType, int matIndexOffset, int matCnt)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Material[] customeMaterial = new Material[matCnt];
        for (int i = 0; i < matCnt; ++i)
        {
            customeMaterial[i] = BrickMaterial.Instance.GetMaterial(matType + matIndexOffset + i);
        }
        renderer.sharedMaterials = customeMaterial;
    }

    private void AddBoxCollider()
    {
        var boxCollider = GetComponent<Renderer>().gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
    }

    public bool CreateMesh(BrickMesh brickMesh, short parentBrickColor, bool invertNext)
    {
        TransformModel(brickMesh);

        if (brickMesh.vertices.Count == 0)
            return false;

        List<Vector3> vts = new List<Vector3>();
        List<Color32> colors = new List<Color32>();
        List<int> opaqueTris = new List<int>();
        List<int> transparentTris = new List<int>();
        Mesh mesh = new Mesh();
        int matIndexOffset = brickMesh.bfcEnabled ? 0 : (int)BrickMaterial.MatType.DS_OFFSET;

        brickMesh.GetRenderMeshInfo(parentBrickColor, invertNext,
            ref vts, ref colors, ref opaqueTris, ref transparentTris);

        mesh.vertices = vts.ToArray();
        mesh.colors32 = colors.ToArray();

        if (opaqueTris.Count > 0 && transparentTris.Count == 0)
        {
            mesh.SetTriangles(opaqueTris.ToArray(), 0);

            if (!brickMesh.bfcEnabled)
                ChangeMaterial(BrickMaterial.MatType.Opaque, matIndexOffset, 1);
        }
        else if (opaqueTris.Count == 0 && transparentTris.Count > 0)
        {
            mesh.SetTriangles(transparentTris.ToArray(), 0);

            ChangeMaterial(BrickMaterial.MatType.Transparent, matIndexOffset, 1);
        }
        else if (opaqueTris.Count > 0 && transparentTris.Count > 0)
        {
            mesh.subMeshCount = 2;
            mesh.SetTriangles(opaqueTris.ToArray(), 0);
            mesh.SetTriangles(transparentTris.ToArray(), 1);

            ChangeMaterial(BrickMaterial.MatType.Opaque, matIndexOffset, 2);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

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

    private void Awake()
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
