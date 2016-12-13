using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

[Serializable]
public class BrickMesh
{
    public string name { get; set; }
    public Matrix4x4 localTr { get; set; }
    public List<Vector3> vertices { get; set; }
    public List<int> triangles { get; set; }
    public List<Color32> colors { get; set; }
    public List<BrickMesh> children { get; set; }

    public BrickMesh(string meshName)
    {
        name = meshName;
        localTr = Matrix4x4.identity;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color32>();
        children = new List<BrickMesh>();
    }

    public BrickMesh(BrickMesh rhs)
    {
        name = rhs.name;
        localTr = rhs.localTr;

        vertices = new List<Vector3>(rhs.vertices);
        triangles = new List<int>(rhs.triangles);
        colors = new List<Color32>(rhs.colors);
        children = new List<BrickMesh>(rhs.children);
    }
}

public class Brick : MonoBehaviour {

    private void TransformModel(BrickMesh brickMesh)
    {
        Matrix4x4 tr = brickMesh.localTr;

        Vector3 localPosition;
        Quaternion localRotation;
        Vector3 localScale;

        MatrixUtil.DecomposeMatrix(tr, out localPosition, out localRotation, out localScale);

        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        transform.localScale = localScale;
    }

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
    }

    public void CreateMesh(BrickMesh brickMesh)
    {
        Mesh mesh = new Mesh();

        TransformModel(brickMesh);

        mesh.vertices = brickMesh.vertices.ToArray();
        mesh.triangles = brickMesh.triangles.ToArray();
        mesh.colors32 = brickMesh.colors.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
