using UnityEngine;
using System.Collections;

using System;

public class BrickGenerator : MonoBehaviour
{
    public GameObject prefab;

    private LdColorTable colorTable;
    private const int MAX_STUD_CNT_PER_MESH = 6;

    private void CreateMesh(BrickMesh brickMesh, Transform parent, int maxStudCnt, 
        bool invertNext = false, short parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        GameObject go = (GameObject)Instantiate(prefab);

        go.name = brickMesh.brickInfo();
        go.GetComponent<Brick>().SetParent(parent);
        go.GetComponent<Brick>().CreateMesh(colorTable, brickMesh, invertNext, parentBrickColor, maxStudCnt);

        for (int i = 0; i < brickMesh.children.Count; ++i)
        {
            bool invertFlag = invertNext ^ brickMesh.invertNext;
            CreateMesh(brickMesh.children[i], go.transform, maxStudCnt, invertFlag, brickMesh.brickColor);
        }
    }

    void LoadModel()
    {
        LdModelLoader modelLoader = new LdModelLoader();

        //var fileName = @"Creator/4349 - Bird.mpd";
        //var fileName = @"Modular buildings/10182 - Cafe Corner.mpd";
        var fileName = @"Friends/3931 - Emma's Splash Pool.mpd";
        //var fileName = @"Simpsons/71006_-_the_simpsons_house.mpd";
        //var fileName = @"3069b.dat";

        BrickMesh brickMesh = new BrickMesh(fileName);
        if (!modelLoader.Load(fileName, ref brickMesh))
        {
            Debug.Log(string.Format("Cannot parse: {0}", fileName));
            return;
        }

        CreateMesh(brickMesh, transform, MAX_STUD_CNT_PER_MESH);
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
