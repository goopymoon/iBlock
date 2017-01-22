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

    public void CreateMesh(bool isOpaque, LdColorTable colorTable, BrickMesh brickMesh, bool invertNext, 
        short parentBrickColor, bool optimizeStud, int maxStudCnt = 6)
    {
        TransformModel(brickMesh);

        if (brickMesh.vertices.Count == 0)
            return;

        List<Vector3> vts = new List<Vector3>();
        List<Color32> colors = new List<Color32>();
        List<int> tris = new List<int>();

        brickMesh.GetRenderMeshInfo(isOpaque, colorTable, invertNext, parentBrickColor, 
            ref vts, ref colors, ref tris, optimizeStud, maxStudCnt);

        Mesh mesh = new Mesh();

        mesh.vertices = vts.ToArray();
        mesh.colors32 = colors.ToArray();

        mesh.SetTriangles(tris.ToArray(), 0);

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
