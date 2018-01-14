using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickMeshManager : MonoBehaviour
{
    private Dictionary<uint, BrickMesh> pool;
    private uint curId = 0;

    private static BrickMeshManager _instance;
    public static BrickMeshManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType(typeof(BrickMeshManager)) as BrickMeshManager;
                if (_instance == null)
                {
                    GameObject container = new GameObject();
                    container.name = "BrickMeshManager";
                    _instance = container.AddComponent(typeof(BrickMeshManager)) as BrickMeshManager;
                }
            }

            return _instance;
        }
    }

    public GameObjId Register(BrickMesh brick)
    {
        GameObjId nextId = new GameObjId(++curId);
        pool.Add(nextId.Val, brick);

        return nextId; ;
    }

    public BrickMesh Get(GameObjId id)
    {
        BrickMesh brick;

        if (pool.TryGetValue(id.Val, out brick))
        {
            return brick;
        }

        return null;
    }

    public bool Remove(GameObjId id)
    {
        return pool.Remove(id.Val);
    }

    public void Dump()
    {
        Debug.Log(string.Format("Pool size is {0}", pool.Count));
        //foreach(KeyValuePair<uint, BrickMesh> entry in pool)
        //{
        //    Debug.Log(string.Format("{0} {1}", entry.Key, entry.Value.Name));
        //}
    }

    public void Initialize()
    {
        pool = new Dictionary<uint, BrickMesh>();
    }
}
