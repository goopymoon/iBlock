/* *****************************************************************************
 * 
 *								EDUCATION RESEARCH GROUP
 *							MORGRIDGE INSTITUTE FOR RESEARCH
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
 ******************************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class APAOctree
{
    public APAOctree parent;
    public List<APAOctree> m_children; 
    public Bounds bounds;
	public List<Triangle> triangles;

    public APAOctree()
    {
        this.parent = null;
        this.m_children = new List<APAOctree>();
		this.triangles = new List<Triangle>();
	}

    public APAOctree(Bounds parentBounds, int generations)
    {
        this.parent = null;
        this.m_children = new List<APAOctree>();
		this.triangles = new List<Triangle>();
        this.bounds = parentBounds;

        CreateChildren(this, generations);
	}

	protected void CreateChildren(APAOctree parent, int generations)
    {
		m_children = new List<APAOctree>();

		Vector3 c = parent.bounds.center;
		float u = parent.bounds.extents.x * 0.5f;
		float v = parent.bounds.extents.y * 0.5f;
		float w = parent.bounds.extents.z * 0.5f;
		Vector3 childrenSize = parent.bounds.extents;
		Vector3[] childrenCenters = {
			new Vector3(c.x + u, c.y + v, c.z + w),
			new Vector3(c.x + u, c.y + v, c.z - w),
			new Vector3(c.x + u, c.y - v, c.z + w),
			new Vector3(c.x + u, c.y - v, c.z - w),
			new Vector3(c.x - u, c.y + v, c.z + w),
			new Vector3(c.x - u, c.y + v, c.z - w),
			new Vector3(c.x - u, c.y - v, c.z + w),
			new Vector3(c.x - u, c.y - v, c.z - w)
		};
		
		for (int i = 0; i < childrenCenters.Length; i++)
        {
			APAOctree o = new APAOctree();
			o.parent = parent;
			o.bounds = new Bounds(childrenCenters[i], childrenSize);
			m_children.Add(o);

            if (generations > 0)
				o.CreateChildren(o, generations - 1);
		}
	}
	
	public APAOctree IndexTriangle(Triangle triangle)
    {
		return IndexTriangle(this, triangle);	
	}
	
	public APAOctree IndexTriangle(APAOctree parentNode, Triangle triangle)
    {
	    // Compute triangle bounds (not using the param version of Mathf.Min() to avoid array allocation)
	    float minX = Mathf.Min(triangle.pt0.x, Mathf.Min(triangle.pt1.x, triangle.pt2.x));
	    float minY = Mathf.Min(triangle.pt0.y, Mathf.Min(triangle.pt1.y, triangle.pt2.y));
	    float minZ = Mathf.Min(triangle.pt0.z, Mathf.Min(triangle.pt1.z, triangle.pt2.z));
	 
	    float maxX = Mathf.Max(triangle.pt0.x, Mathf.Max(triangle.pt1.x, triangle.pt2.x));
	    float maxY = Mathf.Max(triangle.pt0.y, Mathf.Max(triangle.pt1.y, triangle.pt2.y));
	    float maxZ = Mathf.Max(triangle.pt0.z, Mathf.Max(triangle.pt1.z, triangle.pt2.z));
	 
	    APAOctree finalNode = null;
	    APAOctree currentNode = parentNode;
	    while (currentNode != null && finalNode == null)
        {
	        float boundsCenterX = currentNode.bounds.center.x;
	        float boundsCenterY = currentNode.bounds.center.y;
	        float boundsCenterZ = currentNode.bounds.center.z;
	 
	        // Test if the triangle crosses any of the mid planes of the node
	        if ((minX < boundsCenterX && maxX >= boundsCenterX) 
                || (minY < boundsCenterY && maxY >= boundsCenterY) 
                || (minZ < boundsCenterZ && maxZ >= boundsCenterZ))
            {
	            // The triangle must be in the current node
	            finalNode = currentNode;
	        }
            else
            {
	            // The triangle can be inside one of our children, if we have any
	            if (currentNode.m_children != null && currentNode.m_children.Count > 0)
                {
	                // Figure out which child based on which side of each mid plane the triangle sits on
	                int childIndex = 0;
	                if (minX < boundsCenterX)
	                    childIndex |= 4;
	                if (minY < boundsCenterY)
	                    childIndex |= 2;
	                if (minZ < boundsCenterZ)
	                    childIndex |= 1;
	                // Continue iteration with the child node that contains the triangle
	                currentNode = currentNode.m_children[childIndex];
	            }
                else
                {
	                // Since we don't have children, even though the triangle *would* fit in one of our potential child,
	                // we're the node that has to own the triangle.
	                // Arguably, if you hit this code a lot, you could benefit from using more depth in your octree...
	                finalNode = currentNode;
	            }
	        }
	    }
	    return finalNode;
	}
	
	public bool AddTriangle(Triangle t)
    {
		triangles.Add(t);
		return true;
	}
	
	public bool ContainsTriangle(Triangle triangle)
    {
		return 	bounds.Contains(triangle.pt0) &&
				bounds.Contains(triangle.pt1) && 
				bounds.Contains(triangle.pt2);
	}
	
	public void Clear()
    {
		int total = ClearOctree(this);
		Debug.Log("Total Nodes Cleared: " + total);
	}
	
	protected int ClearOctree(APAOctree o)
    {
		int count = 0;
		for (int i = 0; i < o.m_children.Count; i++)
        {
			count += ClearOctree(o.m_children[i]);
		}

		o.triangles.Clear();
		o.triangles.TrimExcess();

		o.parent = null;
		o.m_children.Clear();
		o.m_children.TrimExcess();

		count ++;
		return count;
	}
}
