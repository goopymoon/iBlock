﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class BrickGenerator : MonoBehaviour
{
    public GameObject opaquePrefab;
    public GameObject transparentPrefab;

    private LdColorTable colorTable;
    private const int MAX_STUD_CNT_PER_MESH = 6;

    private GameObject CreateMesh(BrickMesh brickMesh, Transform parent, bool optimizeStud, int maxStudCnt, 
        bool invertNext = false, short parentBrickColor = LdConstant.LD_COLOR_MAIN)
    {
        bool isOpaque = brickMesh.IsOpaque(colorTable, parentBrickColor);

        GameObject go = isOpaque ? (GameObject)Instantiate(opaquePrefab) : (GameObject)Instantiate(transparentPrefab);
        go.name = brickMesh.brickInfo();
        go.GetComponent<Brick>().SetParent(parent);
        go.GetComponent<Brick>().CreateMesh(isOpaque, colorTable, brickMesh, invertNext, parentBrickColor, optimizeStud, maxStudCnt);

        for (int i = 0; i < brickMesh.children.Count; ++i)
        {
            bool invertFlag = invertNext ^ brickMesh.invertNext;
            short accuColor = LdConstant.GetEffectiveColorIndex(brickMesh.brickColor, parentBrickColor);
            CreateMesh(brickMesh.children[i], go.transform, optimizeStud, maxStudCnt, invertFlag, accuColor);
        }

        return go;
    }

    GameObject LoadModel()
    {
        LdModelLoader modelLoader = new LdModelLoader();

        //var fileName = @"Creator/4349 - Bird.mpd";
        //var fileName = @"Modular buildings/10182 - Cafe Corner.mpd";
        var fileName = @"Friends/3931 - Emma's Splash Pool.mpd";
        //var fileName = @"Simpsons/71006_-_the_simpsons_house.mpd";
        //var fileName = @"73435.dat";

        BrickMesh brickMesh = new BrickMesh(fileName);
        if (!modelLoader.Load(fileName, ref brickMesh))
        {
            Debug.Log(string.Format("Cannot parse: {0}", fileName));
            return null;
        }

        bool optimizeStud = true;
        return CreateMesh(brickMesh, transform, optimizeStud, MAX_STUD_CNT_PER_MESH);
    }

    private void Awake()
    {
        colorTable = new LdColorTable();

        colorTable.Initialize();
    }

    // Use this for initialization
    void Start ()
    {
        LoadModel();
    }

    // Update is called once per frame
    void Update ()
    {
    }
}
