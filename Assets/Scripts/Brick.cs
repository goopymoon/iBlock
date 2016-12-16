using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

[Serializable]
public class BrickMesh
{
    public string name { get; set; }

    public byte brickColor { get; private set; }
    public string brickInfo { get { return string.Format("{0}: {1}", brickColor.ToString(), name); } }

    public List<Vector3> vertices { get; set; }
    public List<int> triangles { get; set; }
    public List<byte> colorIndices { get; set; }
    public List<BrickMesh> children { get; set; }

    private Matrix4x4 localTr;

    public BrickMesh(string meshName)
    {
        name = meshName;

        brickColor = LdConstant.LD_COLOR_MAIN;

        localTr = new Matrix4x4();
        localTr = Matrix4x4.identity;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        colorIndices = new List<byte>();
        children = new List<BrickMesh>();
    }

    public BrickMesh(BrickMesh rhs)
    {
        name = rhs.name;

        brickColor = rhs.brickColor;

        localTr = new Matrix4x4();
        localTr = rhs.localTr;

        vertices = new List<Vector3>(rhs.vertices);
        triangles = new List<int>(rhs.triangles);
        colorIndices = new List<byte>(rhs.colorIndices);
        children = new List<BrickMesh>(rhs.children);
    }

    public void AddChildBrick(byte colorIndex, Matrix4x4 trMatrix, BrickMesh child)
    {
        child.brickColor = colorIndex;
        child.localTr = trMatrix;

        children.Add(child);
    }

    public void GetMatrixComponents(out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        MatrixUtil.DecomposeMatrix(localTr, out localPosition, out localRotation, out localScale);
    }

    public void GetColors(LdColorTable colorTable, byte parentColor, ref List<Color32> colList)
    {
        for (int i = 0; i < colorIndices.Count; ++i)
        {
            byte colorIndex = (colorIndices[i] == LdConstant.LD_COLOR_MAIN) ? parentColor : colorIndices[i];
            colList.Add(colorTable.GetColor(colorIndex));
        }
    }
}

public class Brick : MonoBehaviour {

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

    public void SetParent(Transform parent)
    {
        transform.SetParent(parent, false);
    }

    public void CreateMesh(LdColorTable colorTable, BrickMesh brickMesh, byte parentBrickColor)
    {
        Mesh mesh = new Mesh();
        List<Color32> colors = new List<Color32>();

        TransformModel(brickMesh);
        byte effectiveParentColor = LdConstant.GetEffectiveColorIndex(brickMesh.brickColor, parentBrickColor);
        brickMesh.GetColors(colorTable, effectiveParentColor, ref colors);

        mesh.vertices = brickMesh.vertices.ToArray();
        mesh.triangles = brickMesh.triangles.ToArray();
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
