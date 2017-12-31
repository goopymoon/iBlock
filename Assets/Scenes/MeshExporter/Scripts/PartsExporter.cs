#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Linq;

public class PartsExporter : MonoBehaviour {
    // Import path
    public readonly string[] subPath = { "parts", "p" };
    // Export path
    public readonly string partMeshOutPath = "Assets/Resources/Parts/Meshes/";
    public readonly string partPrefabOutPath = "Assets/Resources/Parts/Prefabs/";

    public GameObject brickPrefab;
    public bool exportAll = false;

    private string baseImportPath;
    private Dictionary<string, string> canonicalPathDic;
    private Queue<string> partNames;
    private LdPartsLoader ldPartsLoader;

    public bool AddSearchPath(int subIndex, string filePath)
    {
        if (!Enumerable.Range(0, subPath.Length).Contains<int>(subIndex))
            return false;

        string name = filePath.Substring(Path.Combine(baseImportPath, subPath[subIndex]).Length + 1);
        string extension = Path.GetExtension(name);

        if (extension != ".dat")
            return false;

        if (canonicalPathDic.ContainsKey(name.ToLower()))
            return false;

        var searchPath = Path.Combine(subPath[subIndex], name);

        string fname = name.Replace(@"\", @"/").ToLower();
        string fpath = searchPath.Replace(@"\", @"/").ToLower();
        canonicalPathDic.Add(fname, fpath);

        return true;
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

            string[] subdirectoryEntries = Directory.GetDirectories(partPath);
            foreach (string subdirectory in subdirectoryEntries)
            {
                fileEntries = Directory.GetFiles(subdirectory);
                foreach (string fileName in fileEntries)
                    AddSearchPath(i, fileName);
            }
        }

        // Print for Debugging
        string outFile = Path.Combine(baseImportPath, LdConstant.PARTS_PATH_LIST_FNAME);
        using (StreamWriter file = new StreamWriter(outFile))
        {
            foreach (KeyValuePair<string, string> entry in canonicalPathDic)
            {
                file.WriteLine("{0} {1}", entry.Key, entry.Value);
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
    bool ExportMesh(string fname)
    {
        if (DoesAssetExist(fname))
            return false;

        BrickMesh brickMesh;

        if (!ldPartsLoader.LoadPart(out brickMesh, fname, baseImportPath, canonicalPathDic))
            return false;

        GameObject go = (GameObject)Instantiate(brickPrefab);
 
        go.name = brickMesh.name;
        go.GetComponent<Brick>().SetParent(transform);
        if (!go.GetComponent<Brick>().CreateMesh(brickMesh, LdConstant.LD_COLOR_MAIN, false))
        {
            Debug.Log(string.Format("Cannot create mesh: {0}", fname));
            return false;
        }

        SaveAsset(fname, go);
        Destroy(go);

        return true;
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
        canonicalPathDic = new Dictionary<string, string>();
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
