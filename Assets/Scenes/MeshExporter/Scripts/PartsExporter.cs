#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Linq;

public class PartsExporter : MonoBehaviour {
    // Import path. Order means priority of the same file
    public readonly string[] subPath = { "parts", "parts/s", "p/8", "p/48", "p" };
    public readonly string[] subBasePath = { "parts", "parts", "p", "p", "p" };
    // Export path
    public readonly string partMeshOutPath = "Assets/Resources/Parts/Meshes/";
    public readonly string partPrefabOutPath = "Assets/Resources/Parts/Prefabs/";

    public GameObject brickPrefab;
    public bool exportAll = false;

    private string baseImportPath;
    private Dictionary<string, Queue<string>> canonicalPathDic;
    private Queue<string> partNames;
    private LdPartsLoader ldPartsLoader;

    void AddSearchPath(string fname, string fpath)
    {
        Queue<string> val;
        if (canonicalPathDic.TryGetValue(fname, out val))
        {
            if (val.Contains(fpath))
            {
                Debug.Log(string.Format("Skip duplicated part {0} in {1}", fname, fpath));
                return;
            }
            val.Enqueue(fpath);
        }
        else
        {
            var pathQueue = new Queue<string>();
            pathQueue.Enqueue(fpath);
            canonicalPathDic.Add(fname, pathQueue);
        }
    }

    public void AddSearchPath(int i, string filePath)
    {
        string name = filePath.Substring(Path.Combine(baseImportPath, subBasePath[i]).Length + 1);
        string extension = Path.GetExtension(name);

        if (extension != ".dat")
            return;

        string fpath = Path.Combine(subBasePath[i], name).Replace(@"\", @"/").ToLower();

        // original path
        if (Path.GetDirectoryName(name).Length > 0)
        {
            string oriName = name.Replace(@"\", @"/").ToLower();
            AddSearchPath(oriName, fpath);
        }

        // degenerated path
        string fname = Path.GetFileName(name).ToLower();
        AddSearchPath(fname, fpath);
    }

    void PrepareCanonicalPaths()
    {
        string[] fileEntries;

        for (int i = 0; i < subPath.Length; ++i)
        {
            string partPath = Path.Combine(baseImportPath, subPath[i]);

            fileEntries = Directory.GetFiles(partPath);
            foreach (string fileName in fileEntries)
                AddSearchPath(i, fileName);
        }

        // Print for Debugging
        string outFile = Path.Combine(baseImportPath, LdConstant.PARTS_PATH_LIST_FNAME);
        using (StreamWriter file = new StreamWriter(outFile))
        {
            foreach (KeyValuePair<string, Queue<string>> entry in canonicalPathDic)
            {
                string pathStr = string.Join(" ", entry.Value.ToArray());
                file.WriteLine("{0} {1}", entry.Key, pathStr);
            }
        }
    }

    void PreparePartNames()
    {
        string filePath = Path.Combine(baseImportPath, LdConstant.PARTS_LIST_FNAME);

        if (!File.Exists(filePath))
        {
            Debug.Log(string.Format("File does not exists: {0}", filePath));
            return;
        }

        partNames = new Queue<string>(System.IO.File.ReadAllLines(filePath));
    }

    bool DoesAssetExist(string fname)
    {
        string meshName = Path.ChangeExtension(fname, ".asset");
        string prefabName = Path.ChangeExtension(fname, ".prefab");

        if (File.Exists(partMeshOutPath + meshName) && File.Exists(partPrefabOutPath + prefabName))
        {
            Debug.Log(string.Format("Skip {0}, {1}", meshName, prefabName));
            return true;
        }

        return false;
    }

    bool SaveAsset(string fname, GameObject go)
    {
        string meshName = Path.ChangeExtension(go.name, ".asset");
        string prefabName = Path.ChangeExtension(go.name, ".prefab");

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf)
        {
            AssetDatabase.CreateAsset(mf.sharedMesh, partMeshOutPath + meshName);

            var prefab = PrefabUtility.CreateEmptyPrefab(partPrefabOutPath + prefabName);
            PrefabUtility.ReplacePrefab(go, prefab, ReplacePrefabOptions.ConnectToPrefab);

            return true;
        }

        return false;
    }

/*
 * LDraw parts are measured in LDraw Units (LDU)
    1 brick width/depth = 20 LDU
    1 brick height = 24 LDU
    1 plate height = 8 LDU
    1 stud diameter = 12 LDU
    1 stud height = 4 LDU
   Real World Approximations
    1 LDU = 1/64 in
    1 LDU = 0.4 mm
*/
    void ExportMesh(string fname)
    {
        if (DoesAssetExist(fname))
            return;

        BrickMesh brickMesh;

        if (!ldPartsLoader.LoadPart(out brickMesh, fname, baseImportPath, canonicalPathDic))
            return;

        GameObject go = (GameObject)Instantiate(brickPrefab);
 
        go.name = brickMesh.name;
        go.GetComponent<Brick>().SetParent(transform);
        if (!go.GetComponent<Brick>().CreateMesh(brickMesh, LdConstant.LD_COLOR_MAIN, false))
        {
            Debug.Log(string.Format("Cannot create mesh: {0}", fname));
        }
        else
        {
            SaveAsset(fname, go);
        }

        Destroy(go);
    }

    bool ExportMeshes()
    {
        while (partNames.Any())
        {
            string[] words = partNames.Dequeue().Split(null);

            if (words.Length > 0)
            {
                string partfname = words[0];
                string extension = Path.GetExtension(partfname);

                if (extension.ToLower() != ".dat")
                {
                    Debug.Log(string.Format("File extension must be .dat: {0}", partfname));
                }
                else
                {
                    ExportMesh(partfname);

                    if (!exportAll)
                        return false;
                }
            }
        }

        return true;
    }

    private void Awake()
    {
        baseImportPath = Path.Combine(Application.streamingAssetsPath, LdConstant.LD_PARTS_PATH);

        canonicalPathDic = new Dictionary<string, Queue<string>>();
        ldPartsLoader = new LdPartsLoader();

        PrepareCanonicalPaths();
        PreparePartNames();

        ldPartsLoader.Initialize();
        BrickMaterial.Instance.Initialize();
    }

    IEnumerator StartConvert()
    {
        if (!LdColorTable.Instance.IsInitialized)
            yield return StartCoroutine(LdColorTable.Instance.Initialize());

        ExportMeshes();
    }

    // Use this for initialization
    void Start () {
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetButtonDown("Fire1"))
        {
            StartCoroutine("StartConvert");
        }
    }
}
#endif
