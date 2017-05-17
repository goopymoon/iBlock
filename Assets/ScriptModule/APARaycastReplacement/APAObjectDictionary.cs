/* *****************************************************************************
 * 
 *								EDUCATION RESEARCH GROUP
 *							MORGRIDGE INSTITUTE FOR RESEARCH
 * 			
 * 				
 * Copyright (c) 2012 EDUCATION RESEARCH, MORGRIDGE INSTITUTE FOR RESEARCH
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated  * documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software.
 * 
 * Modified by Burp 2017.
 * 
 ******************************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class APAObjectDictionary : MonoBehaviour
{
    public static APAObjectDictionary singleton;

    private APAOctree octree;
    private int octreeDepth = 3;

    void Awake()
    {
        singleton = this;
    }

    void OnDestroy()
    {
		Debug.Log("Mem Before Clear: " + System.GC.GetTotalMemory(true) / 1024f / 1024f);
		octree.Clear();
		octree = null;
		Debug.Log("Mem After Clear: " + System.GC.GetTotalMemory(true) / 1024f / 1024f);
	}
		
	public void Init (Bounds bounds, List<GameObject> gameObjects)
	{
        octree = new APAOctree(bounds, octreeDepth);
        PopulateOctree(gameObjects);
    }

    void PopulateOctree(List<GameObject> gameObjects)
    {
        GameObject curGO;
        MeshFilter curMeshFilter;
        Triangle[] curTris;
        APAOctree finalNode;

        for (int i = 0; i < gameObjects.Count; i++)
        {
            curGO = gameObjects[i];
            if (curGO == null || curGO.name.Contains("Combined Mesh") || curGO.name == null) continue;

            curMeshFilter = curGO.GetComponent<MeshFilter>();
            if (!curMeshFilter) continue;

            curTris = new Triangle[] { };
            curTris = GetTriangles(curGO);

            for (int k = 0; k < curTris.Length; k++)
            {
                finalNode = octree.IndexTriangle(curTris[k]);
                finalNode.AddTriangle(curTris[k]);
            }
        }

        Debug.Log("Created Database");
        Debug.Log("Total Indexed Triangles: " + GetTriangleCount(octree));
    }

    int GetTriangleCount(APAOctree o)
    {
		int count = o.triangles.Count;
		foreach(APAOctree oct in o.m_children)
        {
			count += GetTriangleCount(oct) ;
		}

		return count;
	}

	Triangle[] GetTriangles(GameObject go)
    {
		Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
		int[] vIndex = mesh.triangles;
		Vector3[] verts = mesh.vertices;
		Vector2[] uvs = mesh.uv;

		List<Triangle> triangleList = new List<Triangle>();

		for (int i = 0; i < vIndex.Length; i +=3)
        {
			triangleList.Add(
                new Triangle(
                    verts[vIndex[i + 0]], 
                    verts[vIndex[i + 1]], 
                    verts[vIndex[i + 2]], 
                    go.transform));
		}
		return triangleList.ToArray();
	}
		
	void OnDrawGizmos()
    {
		DrawOctree(octree);
	}
	
	void DrawOctree(APAOctree oct)
    {
		Gizmos.DrawWireCube(oct.bounds.center, oct.bounds.size);

        foreach (APAOctree o in oct.m_children)
            DrawOctree(o);
    }

    public static APAOctree GetOctree()
    {
        return singleton.octree;
    }
}
