﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;

public class BrickGenerator : MonoBehaviour
{
    public GameObject terrainMesh;
    public GameObject brickPrefab;
    public string modelFileName;
    public bool usePartAsset = false;

    private GameObject modelObj;

    private IEnumerator CreateMesh(BrickMesh brickMesh, Transform parent, 
        bool invertNext = false, short parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        if (brickMesh == null || brickMesh.name == null)
            yield break;

        GameObject go = null;
        bool isMeshExist = false;

        if (usePartAsset && brickMesh.IsPartAsset)
        {
            string path = @"Parts/Prefabs/" + Path.GetFileNameWithoutExtension(brickMesh.name);
            GameObject partObj = Resources.Load(path) as GameObject;
            if (!partObj)
            {
                Debug.Log(string.Format("Cannot load {0}", path));
                yield break;
            }

            go = (GameObject)Instantiate(partObj);
            isMeshExist = go.GetComponent<Brick>().ReconstructMesh(go, brickMesh, parentBrickColor, invertNext);
        }
        else
        {
            go = (GameObject)Instantiate(brickPrefab);
            isMeshExist = go.GetComponent<Brick>().CreateMesh(brickMesh, parentBrickColor, invertNext);
        }

        if (modelObj == null)
            modelObj = go;

        go.name = brickMesh.name;
        go.GetComponent<Brick>().SetParent(parent);

        if (isMeshExist)
        {
            GetComponent<BrickController>().Register(go);
        }

        for (int i = 0; i < brickMesh.children.Count; ++i)
        {
            bool invertFlag = invertNext ^ brickMesh.invertNext;
            short accuColor = LdConstant.GetEffectiveColorIndex(brickMesh.brickColor, parentBrickColor);
            yield return StartCoroutine(CreateMesh(brickMesh.children[i], go.transform, invertFlag, accuColor));
        }
    }

    private void InitCameraZoomRange(GameObject go)
    {
        Bounds aabb = go.GetComponent<Brick>().AABB;
        var mCameraController = Camera.main.GetComponent<TrackballCamera>();

        //mCameraController.minDistance = Math.Max(aabb.extents.magnitude / 10, 1);
        //mCameraController.maxDistance = Math.Max(aabb.extents.magnitude * 2, 5);
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
        modelObj = null;

        if (!LdColorTable.Instance.IsInitialized)
        {
            yield return StartCoroutine(LdColorTable.Instance.Initialize());
        }

        yield return StartCoroutine(GetComponent<LdModelLoader>().Load(modelFileName, usePartAsset));

        StopWatch stopWatch = new StopWatch("Create Mesh");
        yield return StartCoroutine(CreateMesh(GetComponent<LdModelLoader>().model, transform));
        stopWatch.EndTick();

        if (modelObj == null)
            yield break;

        InitCameraZoomRange(modelObj);
        SnapToTerrain(modelObj);
    }

    private void Awake()
    {
        BrickMaterial.Instance.Initialize();
    }

    // Use this for initialization
    void Start ()
    {
        StartCoroutine(LoadModel());
    }

    // Update is called once per frame
    void Update()
    {
    }
}