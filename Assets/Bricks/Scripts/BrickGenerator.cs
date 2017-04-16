using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class BrickGenerator : MonoBehaviour
{
    public GameObject terrainMesh;
    public GameObject brickPrefab;
    public string modelFileName = @"Creator/4349 - Bird.mpd";

    private GameObject CreateMesh(BrickMesh brickMesh, Transform parent, 
        bool invertNext = false, short parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        GameObject go = (GameObject)Instantiate(brickPrefab);

        go.name = brickMesh.brickInfo();
        go.GetComponent<Brick>().SetParent(parent);
        if (go.GetComponent<Brick>().CreateMesh(brickMesh, parentBrickColor, invertNext))
        {
            GetComponent<BrickController>().Register(go);
        }

        for (int i = 0; i < brickMesh.children.Count; ++i)
        {
            bool invertFlag = invertNext ^ brickMesh.invertNext;
            short accuColor = LdConstant.GetEffectiveColorIndex(brickMesh.brickColor, parentBrickColor);
            CreateMesh(brickMesh.children[i], go.transform, invertFlag, accuColor);
        }

        return go;
    }

    private void InitCameraZoomRange(GameObject go)
    {
        Bounds aabb = go.GetComponent<Brick>().AABB;
        var mCameraController = Camera.main.GetComponent<TrackballCamera>();

        mCameraController.minDistance = Math.Max(aabb.extents.magnitude / 10, 1);
        mCameraController.maxDistance = Math.Max(aabb.extents.magnitude * 2, 5);
    }

    private void SnapToTerrain(GameObject go)
    {
        Bounds aabb = go.GetComponent<Brick>().AABB;
        var collider = terrainMesh.GetComponent<Collider>();

        if (collider)
        {
            RaycastHit hit;
            Ray ray = new Ray(aabb.center, -transform.up);
            if (collider.Raycast(ray, out hit, Mathf.Infinity))
            {
                float rayDistance = aabb.center.y - hit.point.y;
                Vector3 shift = transform.up * (aabb.extents.y - rayDistance);
                transform.Translate(shift);
            }
        }
    }

    IEnumerator LoadModel()
    {
        BrickMaterial.Instance.Initialize();

        yield return StartCoroutine(LdColorTable.Instance.Initialize());

        yield return StartCoroutine(GetComponent<LdModelLoader>().LoadPartsPathFile());
        yield return StartCoroutine(GetComponent<LdModelLoader>().Load(modelFileName));

        var go = CreateMesh(GetComponent<LdModelLoader>().model, transform);

        InitCameraZoomRange(go);
        SnapToTerrain(go);
    }

    // Use this for initialization
    void Start ()
    {
        StartCoroutine("LoadModel");
    }

    // Update is called once per frame
    void Update()
    {
    }
}
