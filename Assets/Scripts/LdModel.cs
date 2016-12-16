using UnityEngine;
using System.Collections;

using System;

public class LdModel : MonoBehaviour
{
    public GameObject brickPrefab;

    private LdColorTable colorTable;

    private void CreateMesh(BrickMesh brickMesh, Transform parent, byte parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        GameObject go = (GameObject)Instantiate(brickPrefab);

        go.name = brickMesh.brickInfo;
        go.GetComponent<Brick>().SetParent(parent);
        go.GetComponent<Brick>().CreateMesh(colorTable, brickMesh, parentBrickColor);

        for(int i = 0; i < brickMesh.children.Count; ++i)
        {
            CreateMesh(brickMesh.children[i], go.transform, brickMesh.brickColor);
        }
    }

    // Use this for initialization
    void Start ()
    {
        GameObject mainCam = GameObject.Find("Main Camera");
        colorTable = mainCam.GetComponent<LdColorTable>();
        LdModelLoader modelLoader = new LdModelLoader();

        var fileName = @"Creator/4349 - Bird.mpd";
        //var fileName = @"Modular buildings/10182 - Cafe Corner.mpd";
        //var fileName = @"3857.dat";

        BrickMesh brickMesh = new BrickMesh(fileName);
        if (!modelLoader.Load(fileName, ref brickMesh, false))
        {
            Debug.Log(string.Format("Cannot parse: {0}", fileName));
            return;
        }

        CreateMesh(brickMesh, transform);
    }

    // Update is called once per frame
    void Update ()
    {
	}
}
