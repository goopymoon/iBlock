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

[System.Serializable]
public class Triangle : UnityEngine.Object
{
	public Vector3 pt0;
	public Vector3 pt1;
	public Vector3 pt2;
	
	public Transform trans;
	
	public Triangle (Vector3 pt0, Vector3 pt1, Vector3 pt2, Transform trans)
	{
		this.pt0 = pt0;
		this.pt1 = pt1;
		this.pt2 = pt2;
		this.trans = trans;

		UpdateVerts();
	}
	
	public void UpdateVerts()
    {
		pt0 = trans.TransformPoint(pt0);
		pt1 = trans.TransformPoint(pt1);
		pt2 = trans.TransformPoint(pt2);
	}
	

	
	

}
