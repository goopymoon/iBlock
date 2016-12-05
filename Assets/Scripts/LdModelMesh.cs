using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

[Serializable]
public class BrickMesh
{
    public List<Vector3> vertices { get; set; }
    public List<int> triangles { get; set; }
    public List<Color32> colors { get; set; }

    public List<BrickMesh> children { get; set; }

    public string name { get; set; }

    public BrickMesh(string meshName)
    {
        name = meshName;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        colors = new List<Color32>();

        children = new List<BrickMesh>();
    }
}

public class LdModelMesh : MonoBehaviour {

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
    }

    public void CreateMesh(BrickMesh brickMesh)
    {
        Mesh mesh = new Mesh();

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
