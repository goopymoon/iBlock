using UnityEngine;
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
        if (brickMesh == null)
            yield break;

        GameObject go = null;
        bool isMeshExist = false;

        if (usePartAsset && brickMesh.IsPartAsset)
        {
            string path = @"Parts/Prefabs/" + Path.GetFileNameWithoutExtension(brickMesh.Name);
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

        go.name = brickMesh.Name;
        go.GetComponent<Brick>().SetParent(parent);

        if (isMeshExist)
        {
            GetComponent<BrickController>().Register(go);
        }

        for (int i = 0; i < brickMesh.Children.Count; ++i)
        {
            bool accInvert = invertNext ^ brickMesh.InvertNext;
            short accuColor = LdConstant.GetEffectiveColorIndex(brickMesh.BrickColor, parentBrickColor);
            yield return StartCoroutine(CreateMesh(brickMesh.Children[i], go.transform, accInvert, accuColor));
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

        if (modelObj)
        {
            InitCameraZoomRange(modelObj);
            SnapToTerrain(modelObj);

            BrickMeshManager.Instance.DumpBrickMesh();
        }
    }

    private void Awake()
    {
        BrickMaterial.Instance.Initialize();
        BrickMeshManager.Instance.Initialize();
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
