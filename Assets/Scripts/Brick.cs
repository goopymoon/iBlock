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

    public void CreateMesh(LdColorTable colorTable, BrickMesh brickMesh, bool invertNext, byte parentBrickColor)
    {
        byte effectiveParentColor = LdConstant.GetEffectiveColorIndex(brickMesh.brickColor, parentBrickColor);

        Color32[] colors = new Color32[brickMesh.colorIndices.Count];
        brickMesh.GetColors(colorTable, effectiveParentColor, ref colors);

        int[] tris = new int[brickMesh.triangles.Count];
        brickMesh.GetTriangles(invertNext, ref tris);

        Mesh mesh = new Mesh();

        mesh.vertices = brickMesh.vertices.ToArray();
        mesh.triangles = tris;
        mesh.colors32 = colors;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        //NormalSolver.RecalculateNormals(mesh, 30);

        TransformModel(brickMesh);

        GetComponent<MeshFilter>().mesh = mesh;
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
