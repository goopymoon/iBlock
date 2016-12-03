using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class LdModelMesh : MonoBehaviour {

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
    }

    public void CreateMesh(List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors32 = colors.ToArray();

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
