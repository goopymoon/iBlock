using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Linq;

public class PartsConverter : MonoBehaviour {
    public readonly string ldPartsPath = "../ldraw/ldparts";
    public readonly string[] subPath = { "parts", "p" };
    public readonly string partsList = "parts.lst";

    public GameObject brickPrefab;

    private string basePath;
    private Dictionary<string, string> canonicalPathDic;
    private Dictionary<string, BrickMesh> partBricks;
    private LdPartsLoader ldPartsLoader;

    public bool AddSearchPath(int subIndex, string filePath)
    {
        if (!Enumerable.Range(0, subPath.Length).Contains<int>(subIndex))
            return false;

        string name = filePath.Substring(Path.Combine(basePath, subPath[subIndex]).Length + 1);
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

    void PrepareConverting()
    {
        string[] fileEntries;

        for (int i = 0; i < subPath.Length; ++i)
        {
            string partPath = Path.Combine(basePath, subPath[i]);

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
        string outFile = Path.Combine(basePath, "debug.txt");
        using (StreamWriter file = new StreamWriter(outFile))
        {
            foreach (KeyValuePair<string, string> entry in canonicalPathDic)
            {
                file.WriteLine("{0} {1}", entry.Key, entry.Value);
            }
        }
    }

    bool ConvertMesh(string fname)
    {
        BrickMesh brickMesh;

        if (!ldPartsLoader.LoadPart(out brickMesh, fname, basePath, canonicalPathDic))
            return false;

        GameObject go = (GameObject)Instantiate(brickPrefab);
 
        go.name = brickMesh.brickInfo();
        go.GetComponent<Brick>().SetParent(transform);
        if (!go.GetComponent<Brick>().CreateMesh(brickMesh, LdConstant.LD_COLOR_MAIN, false))
            return false;

        partBricks.Add(fname, brickMesh);
        return true;
    }

    bool ConvertMeshes()
    {
        string filePath = Path.Combine(basePath, partsList);

        if (!File.Exists(filePath))
        {
            Debug.Log(string.Format("File does not exists: {0}", filePath));
            return false;
        }

        string[] lines = System.IO.File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            string[] words = line.Split(null);

            if (words.Length > 0)
            {
                string partfname = words[0];
                string extension = Path.GetExtension(partfname);

                if (extension.ToLower() != ".dat")
                {
                    Debug.Log(string.Format("File extension must be .dat: {0}", partfname));
                    continue;
                }

                if (!ConvertMesh(partfname))
                {
                    Debug.Log(string.Format("Converting mesh failed: {0}", partfname));
                }

                // For debugging 
                // Convert only one brick and exit
                return true;
            }
        }

        return true;
    }

    bool ConvertAllParts()
    {
        bool ret = false;

        PrepareConverting();
        ret = ConvertMeshes();

        return ret;
    }

    private void Awake()
    {
        basePath = Path.GetFullPath(Path.Combine(Application.dataPath, ldPartsPath));
        canonicalPathDic = new Dictionary<string, string>();
        partBricks = new Dictionary<string, BrickMesh>();
        ldPartsLoader = new LdPartsLoader();

        ldPartsLoader.Initialize();
    }

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetButtonDown("Fire1"))
        {
            ConvertAllParts();
        }
    }
}
