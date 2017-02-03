using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class BrickGenerator : MonoBehaviour
{
    public GameObject terrainMesh;
    public GameObject brickPrefab;
    public int MAX_STUD_CNT = 6;

    private GameObject CreateMesh(BrickMesh brickMesh, Transform parent, bool optimizeStud, int maxStudCnt, 
        bool invertNext = false, short parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        GameObject go = (GameObject)Instantiate(brickPrefab);

        go.name = brickMesh.brickInfo();
        go.GetComponent<Brick>().SetParent(parent);
        go.GetComponent<Brick>().CreateMesh(brickMesh, parentBrickColor, invertNext, optimizeStud, maxStudCnt);

        for (int i = 0; i < brickMesh.children.Count; ++i)
        {
            bool invertFlag = invertNext ^ brickMesh.invertNext;
            short accuColor = LdConstant.GetEffectiveColorIndex(brickMesh.brickColor, parentBrickColor);
            CreateMesh(brickMesh.children[i], go.transform, optimizeStud, maxStudCnt, invertFlag, accuColor);
        }

        return go;
    }

    void InitCameraZoomRange(GameObject go)
    {
        Bounds aabb = go.GetComponent<Brick>().AABB;
        var mCameraController = Camera.main.GetComponent<BoundBoxes_maxCamera>();

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

    GameObject LoadModel()
    {
        LdModelLoader modelLoader = new LdModelLoader();
        bool optimizeStud = true;

        //var fileName = @"Creator/4349 - Bird.mpd";
        //var fileName = @"Modular buildings/10182 - Cafe Corner.mpd";
        //var fileName = @"Friends/3931 - Emma's Splash Pool.mpd";
        var fileName = @"Simpsons/71006_-_the_simpsons_house.mpd";

        BrickMesh brickMesh = new BrickMesh(fileName);
        if (!modelLoader.Load(fileName, ref brickMesh))
        {
            Debug.Log(string.Format("Cannot parse: {0}", fileName));
            return null;
        }

        return CreateMesh(brickMesh, transform, optimizeStud, MAX_STUD_CNT);
    }

    void Awake()
    {
        LdColorTable.Instance.Initialize();
        BrickMaterial.Instance.Initialize();
    }

    // Use this for initialization
    void Start ()
    {
        var go = LoadModel();

        InitCameraZoomRange(go);
        SnapToTerrain(go);
    }

    // Update is called once per frame
    void Update ()
    {
    }
}
