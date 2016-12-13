﻿using UnityEngine;
using System.Collections;

using System;

public class LdModel : MonoBehaviour
{
    public GameObject brickPrefab;

    private void CreateMesh(Transform parent, BrickMesh brickMesh)
    {
        GameObject go = (GameObject)Instantiate(brickPrefab);

        go.name = brickMesh.name;
        go.GetComponent<Brick>().SetParent(parent);
        go.GetComponent<Brick>().CreateMesh(brickMesh);

        foreach (var child in brickMesh.children)
        {
            CreateMesh(go.transform, child);
        }
    }

    // Use this for initialization
    void Start ()
    {
        GameObject colorTable = GameObject.Find("Main Camera");
        LdModelLoader modelLoader = new LdModelLoader(colorTable.GetComponent<LdColorTable>());

        var fileName = @"Creator/4349 - Bird.mpd";
        //var fileName = @"Modular buildings/10182 - Cafe Corner.mpd";
        //var fileName = @"3857.dat";

        BrickMesh brickMesh = new BrickMesh(fileName);
        if (!modelLoader.Load(fileName, ref brickMesh, false))
        {
            Console.WriteLine("Cannot parse: {0}", fileName);
            return;
        }

        CreateMesh(transform, brickMesh);
    }

    // Update is called once per frame
    void Update ()
    {
	}
}
