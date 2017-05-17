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
using System;

public class APARaycastHit : IComparable<APARaycastHit>
{	
	public float distance;
	public Transform transform;
	public Vector2 barycentricCoordinate;
	public Vector3 point;

    int IComparable<APARaycastHit>.CompareTo(APARaycastHit obj)
    {
        return distance > obj.distance ? 1 : distance == obj.distance ? 0 : -1;
    }

    public APARaycastHit()
    {
		this.distance = 0f;
		this.transform = null;
		this.barycentricCoordinate = Vector2.zero;
		this.point = Vector3.zero;
	}
	
	public APARaycastHit(Transform transform, float distance, Vector2 barycentricCoordinate)
    {
		this.distance = distance;
		this.transform = transform;
		this.barycentricCoordinate = barycentricCoordinate;
		this.point = Vector3.zero;
	}
}

public class APARaycast : MonoBehaviour
{	
	static Vector3 edge1 = new Vector3();
	static Vector3 edge2 = new Vector3();
	static Vector3 tVec = new Vector3();
	static Vector3 pVec = new Vector3();
	static Vector3 qVec = new Vector3();
	
	static float det = 0;
	static float invDet = 0;
	
	static float epsilon = 0.0000001f;
	static System.Diagnostics.Stopwatch stopWatch;

    public static string intersectionErrorType = "";

    public static bool Raycast (Ray ray, out APARaycastHit hit)
	{
        hit = new APARaycastHit();
		List<APARaycastHit> hits = new List<APARaycastHit>();

        hits = INTERNAL_RaycastAll(ray);
        if (hits.Count > 0)
        {
            hit = hits[0];
            return true;
        }

        return false;
	}
	
	static List<APARaycastHit> INTERNAL_RaycastAll(Ray ray)
	{
		stopWatch = new System.Diagnostics.Stopwatch();
		stopWatch.Start();

		List<APARaycastHit> hits = new List<APARaycastHit>();

        APAOctree octree = APAObjectDictionary.GetOctree();
        if (octree.bounds.IntersectRay(ray))
        {
            hits = RecurseOctreeBounds(octree, ray);
        }

        hits.Sort();

		stopWatch.Stop();
		Debug.Log("Search Time: " + stopWatch.ElapsedMilliseconds + " ms");

		return hits;
	}
	
    static List<APARaycastHit> RecurseOctreeBounds(APAOctree octree, Ray ray)
    {
        List<APARaycastHit> hits = new List<APARaycastHit>();
        float dist = 0f;
        Vector2 baryCoord = new Vector2();

        if (octree.bounds.IntersectRay(ray))
        {
            for (int i = 0; i < octree.triangles.Count; i++)
            {
                if (TestIntersection(octree.triangles[i], ray, out dist, out baryCoord))
                    hits.Add(BuildRaycastHit(octree.triangles[i], dist, baryCoord));
            }
        }

        for (int i = 0; i < octree.m_children.Count; i++)
            hits.AddRange(RecurseOctreeBounds(octree.m_children[i], ray));

        return hits;
    }

    static APARaycastHit BuildRaycastHit(Triangle hitTriangle, float distance, Vector2 barycentricCoordinate)
    {
		APARaycastHit returnedHit = new APARaycastHit(hitTriangle.trans, distance, barycentricCoordinate);
		returnedHit.point = hitTriangle.pt0 
            + ((hitTriangle.pt1 - hitTriangle.pt0) * barycentricCoordinate.x) 
            + ((hitTriangle.pt2 - hitTriangle.pt0) * barycentricCoordinate.y);
		
		return returnedHit;		
	}
	
	/// <summary>
	/// Tests the intersection.
	/// Implementation of the Moller/Trumbore intersection algorithm 
	/// </summary>
	/// <returns>
	/// Bool if the ray does intersect
	/// out dist - the distance along the ray at the intersection point
	/// out hitPoint - 
	/// </returns>
	/// <param name='triangle'>
	/// If set to <c>true</c> triangle.
	/// </param>
	/// <param name='ray'>
	/// If set to <c>true</c> ray.
	/// </param>
	/// <param name='dist'>
	/// If set to <c>true</c> dist.
	/// </param>
	/// <param name='baryCoord'>
	/// If set to <c>true</c> barycentric coordinate of the intersection point.
	/// </param>
	/// http://www.cs.virginia.edu/~gfx/Courses/2003/ImageSynthesis/papers/Acceleration/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf
	static bool TestIntersection(Triangle triangle, Ray ray, out float dist, out Vector2 baryCoord)
    {
		baryCoord = Vector2.zero;
		dist = Mathf.Infinity;
		edge1 = triangle.pt1 - triangle.pt0;
		edge2 = triangle.pt2 - triangle.pt0;
		
		pVec = Vector3.Cross(ray.direction, edge2);
		det = Vector3.Dot(edge1, pVec);
		if (det < epsilon)
        {
			intersectionErrorType = "Failed Epsilon";
			return false;	
		}
		tVec = ray.origin - triangle.pt0;
		var u = Vector3.Dot (tVec, pVec);
		if (u < 0 || u > det)
        {
			intersectionErrorType = "Failed Dot1";
			return false;	
		}
		qVec = Vector3.Cross (tVec, edge1);
		var v = Vector3.Dot (ray.direction, qVec);
		if (v < 0 || u + v > det)
        {
			intersectionErrorType = "Failed Dot2";
			return false;	
		}
		dist = Vector3.Dot(edge2, qVec);
		invDet = 1 / det;
		dist *= invDet;
		baryCoord.x = u * invDet;
		baryCoord.y = v * invDet;
		return true;
	}	
}
