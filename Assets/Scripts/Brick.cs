using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class Brick : MonoBehaviour {

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

    public void CreateMesh(LdColorTable colorTable, BrickMesh brickMesh, bool invertNext, 
        short parentBrickColor, bool optimizeStud, int maxStudCnt = 6)
    {
        List<Vector3> vts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color32> colors = new List<Color32>();

        brickMesh.GetMeshInfo(colorTable, invertNext, parentBrickColor, 
            ref vts, ref tris, ref colors, optimizeStud, maxStudCnt);

        Mesh mesh = new Mesh();

        mesh.vertices = vts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.colors32 = colors.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        AddCollider();

        TransformModel(brickMesh);
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
