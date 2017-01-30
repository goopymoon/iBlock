using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class Brick : MonoBehaviour {

    public bool enableCollider = false;

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
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

    public void CreateMesh(BrickMesh brickMesh, short parentBrickColor, bool invertNext, bool optimizeStud, int maxStudCnt)
    {
        TransformModel(brickMesh);

        if (brickMesh.vertices.Count == 0)
            return;

        List<Vector3> vts = new List<Vector3>();
        List<Color32> colors = new List<Color32>();
        List<int> opaqueTris = new List<int>();
        List<int> transparentTris = new List<int>();

        brickMesh.GetRenderMeshInfo(parentBrickColor, invertNext,
            ref vts, ref colors, ref opaqueTris, ref transparentTris, optimizeStud, maxStudCnt);

        Mesh mesh = new Mesh();
        mesh.vertices = vts.ToArray();
        mesh.colors32 = colors.ToArray();

        int matIndexOffset = brickMesh.bfcEnabled ? 0 : (int)BrickMaterial.MatType.DS_OFFSET;
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

        if (enableCollider)
            GetComponent<Renderer>().gameObject.AddComponent<BoxCollider>();
    }

    // Use this for initialization
    void Start () {

    }
	
    // Update is called once per frame
    void Update () {
    }
}
