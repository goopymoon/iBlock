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

    private void AddCollider()
    {
        var renderer = GetComponent<Renderer>();
        BoxCollider BC = renderer.gameObject.AddComponent<BoxCollider>();

        BC.center = renderer.bounds.center;
        BC.size = renderer.bounds.size;
    }

    public void CreateMesh(LdColorTable colorTable, BrickMesh brickMesh, short parentBrickColor, bool invertNext, bool optimizeStud, int maxStudCnt)
    {
        TransformModel(brickMesh);

        if (brickMesh.vertices.Count == 0)
            return;

        List<Vector3> vts = new List<Vector3>();
        List<Color32> colors = new List<Color32>();
        List<int> opaqueTris = new List<int>();
        List<int> transparentTris = new List<int>();

        brickMesh.GetRenderMeshInfo(colorTable, parentBrickColor, invertNext,
            ref vts, ref colors, ref opaqueTris, ref transparentTris, optimizeStud, maxStudCnt);

        Mesh mesh = new Mesh();

        mesh.vertices = vts.ToArray();
        mesh.colors32 = colors.ToArray();

        if (opaqueTris.Count > 0)
        {
            mesh.SetTriangles(opaqueTris.ToArray(), 0);
            if (transparentTris.Count > 0)
            {
                mesh.subMeshCount = 2;

                Material[] customeMaterial = new Material[2];

                customeMaterial[0] = Resources.Load("Materials/OpaqueBrickColor", typeof(Material)) as Material;
                customeMaterial[1] = Resources.Load("Materials/TransparentBrickColor", typeof(Material)) as Material;
                MeshRenderer renderer = GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.materials = customeMaterial;

                mesh.SetTriangles(transparentTris.ToArray(), 1);
            }
        }
        else
        {
            if (transparentTris.Count > 0)
            {
                Material customeMaterial = Resources.Load("Materials/TransparentBrickColor", typeof(Material)) as Material;
                MeshRenderer renderer = GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.material = customeMaterial;

                mesh.SetTriangles(transparentTris.ToArray(), 0);
            }
        }
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        if (enableCollider)
            AddCollider();
    }

    void DebugNormal()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int normalLength = 2;

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3 norm = transform.TransformDirection(mesh.normals[i]);
            Vector3 vert = transform.TransformPoint(mesh.vertices[i]);
            Debug.DrawRay(vert, norm * normalLength, Color.red);
        }
    }

    // Use this for initialization
    void Start () {

    }
	
    // Update is called once per frame
    void Update () {
        //DebugNormal();
    }
}
