using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class BrickGenerator : MonoBehaviour
{
    public GameObject terrainMesh;
    public GameObject brickPrefab;
    public string modelFileName = @"Creator/4349 - Bird.mpd";
    public bool optimizeStud = true;
    public int MAX_STUD_CNT = 6;

    GameObject headBrick = null;
    GameObject curBrick = null;
    GameObject lastBrick = null;

    private GameObject CreateMesh(BrickMesh brickMesh, Transform parent, bool optimizeStud, int maxStudCnt, 
        bool invertNext = false, short parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        GameObject go = (GameObject)Instantiate(brickPrefab);

        go.name = brickMesh.brickInfo();
        go.GetComponent<Brick>().SetParent(parent);
        if (go.GetComponent<Brick>().CreateMesh(brickMesh, parentBrickColor, invertNext, optimizeStud, maxStudCnt))
        {
            if (headBrick == null)
            {
                headBrick = go;
                curBrick = go;
            }
            else
            {
                curBrick.GetComponent<Brick>().nextBrick = go;
                go.GetComponent<Brick>().prevBrick = curBrick;
                curBrick = go;
                lastBrick = go;
            }
        }

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

    void Awake()
    {
        BrickMaterial.Instance.Initialize();
        LdColorTable.Instance.Initialize();
    }

    GameObject LoadModel()
    {
        //modelFileName = @"Modular buildings/10182 - Cafe Corner.mpd";
        //modelFileName = @"Friends/3931 - Emma's Splash Pool.mpd";
        //modelFileName = @"Simpsons/71006_-_the_simpsons_house.mpd";

        LdModelLoader modelLoader = new LdModelLoader();
        if (!modelLoader.Initialize())
        {
            Debug.Log(string.Format("Cannot initailize LdMoelLoader."));
            return null;
        }

        BrickMesh brickMesh = modelLoader.Load(modelFileName);
        if (brickMesh == null)
        {
            Debug.Log(string.Format("Cannot parse: {0}", modelFileName));
            return null;
        }

        return CreateMesh(brickMesh, transform, optimizeStud, MAX_STUD_CNT);
    }

    void SetVisibility(bool flag)
    {
        // toggles the visibility of this gameobject and all it's children
        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.enabled = flag;
        }
    }

    void SetOBBVisibility(bool flag)
    { 
        var obbs = gameObject.GetComponentsInChildren<BoundBoxes_BoundBox>();
        foreach(var element in obbs)
        {
            element.SelectBound(flag);
        }
    }

    // Use this for initialization
    void Start ()
    {
        var go = LoadModel();

        if (go != null)
        {
            InitCameraZoomRange(go);
            SnapToTerrain(go);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (curBrick == null)
                return;

            curBrick.GetComponent<Renderer>().enabled = true;
            curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(false);

            var nextBrick = curBrick.GetComponent<Brick>().nextBrick;
            if (nextBrick != null)
            {
                curBrick = nextBrick;
                curBrick.GetComponent<Renderer>().enabled = false;
                curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            if (curBrick == null)
                return;

            if (curBrick == lastBrick && !curBrick.GetComponent<BoundBoxes_BoundBox>().IsSelected())
            {
                curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
                return;
            }
            else
            {
                if (curBrick.GetComponent<Renderer>().enabled)
                    curBrick.GetComponent<Renderer>().enabled = false;

                if (curBrick != headBrick)
                {
                    curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(false);
                    var prevBrick = curBrick.GetComponent<Brick>().prevBrick;
                    if (prevBrick != null)
                    {
                        curBrick = prevBrick;
                        curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            SetVisibility(false);
            SetOBBVisibility(false);

            curBrick = headBrick;
            if (curBrick != null)
            {
                curBrick.GetComponent<Renderer>().enabled = false;
                curBrick.GetComponent<BoundBoxes_BoundBox>().SelectBound(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SetVisibility(true);
            SetOBBVisibility(false);

            curBrick = lastBrick;
        }
    }
}
