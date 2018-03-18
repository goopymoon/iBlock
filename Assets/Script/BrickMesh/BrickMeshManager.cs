using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickMeshManager : MonoBehaviour
{
    public Dictionary<string, StudInfo.eStudType> studPool;

    private Dictionary<string, BrickMeshInfo> infoPool;
    private Dictionary<string, BrickMesh> brickPool;

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

    public bool RegisterBrickMeshInfo(BrickMeshInfo info)
    {
        if (infoPool.ContainsKey(info.Name))
        {
            Debug.Log(string.Format("Cannot register duplicated brickMeshInfo: {0}", info.Name));
            return false;
        }

        info.Optimize();
        infoPool.Add(info.Name, info);

        //Debug.Log(string.Format("Registered BrickMeshInfo: {0}", info.Name));
        return true;
    }

    public bool RegisterBrickMesh(string name, BrickMesh brick)
    {
        if (brickPool.ContainsKey(name))
        {
            return false;
        }
        brickPool.Add(name, brick);
        return true;
    }

    public BrickMesh GetBrickMesh(string name)
    {
        BrickMesh brick;

        if (brickPool.TryGetValue(name, out brick))
        {
            return brick;
        }

        return null;
    }

    public BrickMeshInfo GetBrickMeshInfo(string name)
    {
        BrickMeshInfo val;
        if (infoPool.TryGetValue(name, out val))
        {
            return val;
        }

        return null;
    }

    public bool RemoveBrickMeshInfo(string name)
    {
        return infoPool.Remove(name);
    }

    public void RemoveBrickMesh(BrickMesh brickMesh)
    {
        brickPool.Remove(brickMesh.Name);
    }

    public void DumpBrickMeshInfo()
    {
        Debug.Log(string.Format("BrickMeshInfo Pool size is {0}", infoPool.Count));
        //foreach(KeyValuePair<uint, BrickMeshInfo> entry in infoPool)
        //{
        //    Debug.Log(string.Format("{0} {1}", entry.Key, entry.Value.Name));
        //}
    }

    public void DumpBrickMesh()
    {
        Debug.Log(string.Format("BrickMesh Pool size is {0}", brickPool.Count));
        //foreach(KeyValuePair<uint, BrickMesh> entry in brickPool)
        //{
        //    Debug.Log(string.Format("{0} {1}", entry.Key, entry.Value.Name));
        //}
    }

    public bool TryGetStudType(string name, out StudInfo.eStudType studType)
    {
        studType = StudInfo.eStudType.ST_NA;

        return studPool.TryGetValue(name, out studType);
    }

    public void Initialize()
    {
        infoPool = new Dictionary<string, BrickMeshInfo>();
        brickPool = new Dictionary<string, BrickMesh>();

        // stud list
        studPool = new Dictionary<string, StudInfo.eStudType>();
        // convex stud
        studPool.Add("stud.dat", StudInfo.eStudType.ST_CONVEX);
        studPool.Add("stud3.dat", StudInfo.eStudType.ST_CONVEX);
        studPool.Add("stud3a.dat", StudInfo.eStudType.ST_CONVEX);
        // concave stud
        studPool.Add("stud2.dat", StudInfo.eStudType.ST_CONCAVE);
        studPool.Add("stud2a.dat", StudInfo.eStudType.ST_CONCAVE);
        studPool.Add("stud4.dat", StudInfo.eStudType.ST_CONCAVE);
        studPool.Add("stud4a.dat", StudInfo.eStudType.ST_CONCAVE);
    }
}
