﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StudObjManager : MonoBehaviour {

    public GameObject brickPrefab;

    public static StudObjManager Instance;
    public Dictionary<string, Queue<GameObject>> idlePool;

    public void CreateStudMesh(StudInfo studInfo, Transform parent,
    short parentBrickColor = LdConstant.LD_COLOR_MAIN, bool invertNext = false)
    {
        GameObject go = PopStud(studInfo.Name);
        if (go == null)
        {
            go = (GameObject)Instantiate(brickPrefab);
            go.GetComponent<Brick>().CreateStudMesh(ref studInfo, parent, parentBrickColor, invertNext);
        }

        go.name = studInfo.Name;
        go.GetComponent<Brick>().SetParent(parent);
        go.SetActive(true);
    }

    public void ReleaseStudMesh(GameObject go)
    {
        go.GetComponent<Brick>().SetParent(null);
        go.SetActive(false);

        PushStud(go);
    }

    private GameObject PopStud(string name)
    {
        Queue<GameObject> idleObjs;
        if (idlePool.TryGetValue(name, out idleObjs))
        {
            if (idleObjs.Count > 0)
            {
                GameObject go = idleObjs.Dequeue();
            }
        }

        return null;
    }

    private void PushStud(GameObject go)
    {
        Queue<GameObject> idleObjs;
        if (!idlePool.TryGetValue(go.name, out idleObjs))
        {
            idleObjs = new Queue<GameObject>();
            idleObjs.Enqueue(go);

            idlePool.Add(go.name, idleObjs);
        }
        else
        {
            idleObjs.Enqueue(go);
        }
    }

    public void Awake()
    {
        Instance = this;
        idlePool = new Dictionary<string, Queue<GameObject>>();
    }
}
