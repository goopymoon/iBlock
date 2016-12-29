using UnityEngine;
using System.Collections;

using System;

public class BrickGenerator : MonoBehaviour
{
    public GameObject prefab;

    private LdColorTable colorTable;

    private void CreateMesh(BrickMesh brickMesh, Transform parent, bool invertNext = false, byte parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        GameObject go = (GameObject)Instantiate(prefab);

        go.name = brickMesh.brickInfo();
        go.GetComponent<Brick>().SetParent(parent);
        go.GetComponent<Brick>().CreateMesh(colorTable, brickMesh, invertNext, parentBrickColor);

        for (int i = 0; i < brickMesh.children.Count; ++i)
        {
            bool invertFlag = invertNext ^ brickMesh.invertNext;
            CreateMesh(brickMesh.children[i], go.transform, invertFlag, brickMesh.brickColor);
        }
    }

    void LoadModel()
    {
        LdModelLoader modelLoader = new LdModelLoader();

        //var fileName = @"Creator/4349 - Bird.mpd";
        var fileName = @"Modular buildings/10182 - Cafe Corner.mpd";
        //var fileName = @"Friends/3931 - Emma's Splash Pool.mpd";
        //var fileName = @"3857.dat";
        //var fileName = @"3069b.dat";
        //var fileName = @"s/3069bs01.dat";

        BrickMesh brickMesh = new BrickMesh(fileName);
        if (!modelLoader.Load(fileName, ref brickMesh, true))
        {
            Debug.Log(string.Format("Cannot parse: {0}", fileName));
            return;
        }

        CreateMesh(brickMesh, transform);
    }

    // Use this for initialization
    void Start ()
    {
        GameObject mainCam = GameObject.Find("Main Camera");
        colorTable = mainCam.GetComponent<LdColorTable>();

        LoadModel();
    }

    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.B))
            LoadModel();
    }
}
