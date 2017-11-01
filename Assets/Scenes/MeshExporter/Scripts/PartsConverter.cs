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

    string basePath;
    Dictionary<string, string> canonicalPathDic;

    public bool AddSearchPath(int subIndex, string filePath)
    {
        if (!Enumerable.Range(0, subPath.Length - 1).Contains<int>(subIndex))
            return false;

        string name = filePath.Substring(Path.Combine(basePath, subPath[subIndex]).Length + 1);
        string extension = Path.GetExtension(name);

        if (extension != ".dat")
            return false;

        if (canonicalPathDic.ContainsKey(name.ToLower()))
            return false;

        var searchPath = Path.Combine(subPath[subIndex], name);

        canonicalPathDic.Add(name.ToLower(), searchPath.ToLower());

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
    }

    bool ConvertMesh(string fname)
    {
        return true;
    }

    bool ConvertMeshes()
    {
        canonicalPathDic.Clear();

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

                ConvertMesh(partfname);
            }
        }

        return true;
    }

    bool ConvertAllParts()
    {
        PrepareConverting();
        return ConvertMeshes();
    }

    private void Awake()
    {
        basePath = Path.GetFullPath(Path.Combine(Application.dataPath, ldPartsPath));
        canonicalPathDic = new Dictionary<string, string>();
    }

    // Use this for initialization
    void Start () {
        ConvertAllParts();
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
