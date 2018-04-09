#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Linq;

public class PartsExporterScript : ScriptableObject
{
    // Import path. Order means priority of the same file
    public readonly string[] subPath = { "parts", "parts/s", "p/8", "p/48", "p" };
    public readonly string[] subBasePath = { "parts", "parts", "p", "p", "p" };
    // Export path
    public readonly string partsMeshOutPath = "Assets/Parts/Meshes/";
    public readonly string partsPrefabOutPath = "Assets/Parts/Prefabs/";
    public string BaseImportPath { get; private set; }

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
        string name = filePath.Substring(Path.Combine(BaseImportPath, subBasePath[i]).Length + 1);
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
            string partPath = Path.Combine(BaseImportPath, subPath[i]);

            fileEntries = Directory.GetFiles(partPath);
            foreach (string fileName in fileEntries)
                AddSearchPath(i, fileName);
        }

        // Save partspath.lst file
        string outFile = Path.Combine(LdConstant.PARTS_PATH, LdConstant.PARTS_PATH_LIST_FNAME);
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
        string filePath = Path.Combine(BaseImportPath, LdConstant.PARTS_LIST_FNAME);

        if (!File.Exists(filePath))
        {
            Debug.Log(string.Format("File does not exists: {0}", filePath));
            return;
        }

        partNames = new Queue<string>(System.IO.File.ReadAllLines(filePath));
    }

    public bool DoesAssetExist(string fname)
    {
        string meshName = Path.ChangeExtension(fname, ".asset");
        string prefabName = Path.ChangeExtension(fname, ".prefab");

        if (File.Exists(partsMeshOutPath + meshName) && File.Exists(partsPrefabOutPath + prefabName))
        {
            Debug.Log(string.Format("Skip {0}, {1}", meshName, prefabName));
            return true;
        }

        return false;
    }

    public bool SaveAsset(string fname, GameObject go)
    {
        string meshName = Path.ChangeExtension(go.name, ".asset");
        string prefabName = Path.ChangeExtension(go.name, ".prefab");

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf)
        {
            AssetDatabase.CreateAsset(mf.sharedMesh, partsMeshOutPath + meshName);

            var prefab = PrefabUtility.CreateEmptyPrefab(partsPrefabOutPath + prefabName);
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
    public void ExportMesh(string fname)
    {
        if (DoesAssetExist(fname))
            return;

        BrickMesh brickMesh;

        if (!ldPartsLoader.LoadPart(out brickMesh, fname, BaseImportPath, canonicalPathDic))
            return;

        UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Brick.prefab", typeof(GameObject));
        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
        go.name = brickMesh.Name;

        if (!go.GetComponent<Brick>().CreateMesh(brickMesh, false, LdConstant.LD_COLOR_MAIN))
        {
            Debug.Log(string.Format("Cannot create mesh: {0}", fname));
        }
        else
        {
            SaveAsset(fname, go);
        }

        DestroyImmediate(go);
    }

    public bool ExportMeshes()
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
                }
            }
        }

        return true;
    }

    public void Initialize()
    {
        string folderPath = Path.Combine(Application.dataPath, "LdrawData");
        BaseImportPath = Path.Combine(folderPath, LdConstant.LD_PARTS_PATH);

        canonicalPathDic = new Dictionary<string, Queue<string>>();
        ldPartsLoader = new LdPartsLoader();

        PrepareCanonicalPaths();
        PreparePartNames();

        ldPartsLoader.Initialize();
    }
}

public class PartsExporter : ScriptableObject
{
    public static bool IsInitialized { get; private set; }

    static PartsExporter()
    {
        PartsExporter.IsInitialized = false;
    }

    static void DoExport(bool exportAll)
    {
        if (!IsInitialized)
        {
            if (!LdColorTable.Instance.Initialize())
                return;

            BrickMeshManager.Instance.Initialize();
            BrickMaterial.Instance.Initialize();

            IsInitialized = true;
        }

        PartsExporterScript export = ScriptableObject.CreateInstance<PartsExporterScript>();
        export.Initialize();
        if (exportAll)
        {
            export.ExportMeshes();
        }
        else
        {
            string openDir = Path.Combine(export.BaseImportPath, "parts");
            string filePath = EditorUtility.OpenFilePanel("Export prefab file", openDir, "dat");
            string fileName = Path.GetFileName(filePath);
            export.ExportMesh(fileName);
        }
    }

    [MenuItem("Ldraw/Export Prefab (All)")]
    static void DoExportAll()
    {
        DoExport(true);
    }

    [MenuItem("Ldraw/Export Prefab (Single)")]
    static void DoExportSingle()
    {
        DoExport(false);
    }
}

#endif
