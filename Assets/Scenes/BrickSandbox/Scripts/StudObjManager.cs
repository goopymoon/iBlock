using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StudObjManager : MonoBehaviour {

    public GameObject brickPrefab;

    public static StudObjManager Instance;
    public Dictionary<int, string> activePool;
    public Dictionary<string, Queue<GameObject>> idlePool;

    string GetStudKey(StudInfo studInfo, short parentBrickColor, bool invertNext)
    {
        short effectiveColor = LdConstant.GetEffectiveColorIndex(studInfo.ColorIndex, parentBrickColor);
        bool inverted = studInfo.Inverted ^ invertNext;

        return string.Format("{0}|{1}|{2}", studInfo.Name, effectiveColor, inverted);
    }

    public GameObject CreateStudMesh(StudInfo studInfo, Transform parent, 
        short parentBrickColor, bool invertNext)
    {
        string key = GetStudKey(studInfo, parentBrickColor, invertNext);
        GameObject go = PopStud(key);
        if (go == null)
        {
            go = (GameObject)Instantiate(brickPrefab);
            go.GetComponent<Brick>().CreateStudMesh(ref studInfo, parentBrickColor, invertNext);
        }
        else
        {
            go.GetComponent<Brick>().TransformModel(studInfo.Tr);
        }

        go.name = studInfo.Name;
        go.GetComponent<Brick>().SetParent(parent);
        go.SetActive(true);

        activePool.Add(go.GetInstanceID(), key);

        return go;
    }

    public void ClearPool()
    {
        ClearActivePool();
        ClearIdlePool();
    }

    void ClearActivePool()
    {
        activePool.Clear();
    }

    void ClearIdlePool()
    {
        foreach(var element in idlePool)
        {
            Queue<GameObject> idleObjs = element.Value;
            while(idleObjs.Count > 0)
            {
                GameObject go = idleObjs.Dequeue();
                Destroy(go);
            }
        }
        idlePool.Clear();
    }

    public void ReleaseStudMesh(GameObject go)
    {
        go.GetComponent<Brick>().SetParent(null);
        go.SetActive(false);

        string key;
        if (activePool.TryGetValue(go.GetInstanceID(), out key))
        {
            activePool.Remove(go.GetInstanceID());
            PushStud(key, go);
        }
    }

    private GameObject PopStud(string keyStr)
    {
        Queue<GameObject> idleObjs;
        if (idlePool.TryGetValue(keyStr, out idleObjs))
        {
            if (idleObjs.Count > 0)
            {
                GameObject go = idleObjs.Dequeue();
                return go;
            }
        }

        return null;
    }

    private void PushStud(string key, GameObject go)
    {
        Queue<GameObject> idleObjs;

        if (!idlePool.TryGetValue(key, out idleObjs))
        {
            idleObjs = new Queue<GameObject>();
            idleObjs.Enqueue(go);

            idlePool.Add(key, idleObjs);
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
        activePool = new Dictionary<int, string>();
    }
}
