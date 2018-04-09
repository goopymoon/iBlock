#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;
using System.Linq;

public class LdModelLoader : ScriptableObject
{
    // parts list contains only recommended parts
    private Dictionary<string, string> partsListCache;
    // parts path contains paths of all parts
    private Dictionary<string, Queue<string>> partsPathCache;

    private Dictionary<string, LdFileParser.FileLines> fileCache;
    private LdFileParser ldFileParser;
    private HashSet<string> subFileNames;

    static System.Diagnostics.Stopwatch stopWatch;

    private string GetBasePartImportPath()
    {
        return Path.Combine(Path.Combine(Application.dataPath, "LdrawData"), LdConstant.LD_PARTS_PATH);
    }

    public bool LoadPartsListFile()
    {
        var filePath = Path.Combine(GetBasePartImportPath(), LdConstant.PARTS_LIST_FNAME);

        FileLoader fileLoader = new FileLoader();
        if (!fileLoader.LoadFile(filePath))
            return false;

        string[] readText;
        if (!fileLoader.GetSplitLines(out readText))
            return false;

        for (int i = 0; i<readText.Length; ++i)
        {
            string[] words = readText[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
                partsListCache.Add(words[0], "");
        }

        Debug.Log(string.Format("Parts list cache is ready: {0}", filePath));

        return true;
    }

    private bool LoadPartsPathFile()
    {
        string filePath = Path.Combine(LdConstant.PARTS_PATH, LdConstant.PARTS_PATH_LIST_FNAME);

        FileLoader fileLoader = new FileLoader();
        if (!fileLoader.LoadFile(filePath))
            return false;

        string[] readText;
        if (!fileLoader.GetSplitLines(out readText))
            return false;

        for (int i = 0; i < readText.Length; ++i)
        {
            string[] words = readText[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                Queue<string> val;
                if (!partsPathCache.TryGetValue(words[0], out val))
                {
                    Queue<string> pathQueue = new Queue<string>();
                    pathQueue.Enqueue(words[1]);
                    partsPathCache.Add(words[0], pathQueue);
                }
                else
                {
                    val.Enqueue(words[1]);
                }
            }
        }

        Debug.Log(string.Format("Parts path list is ready: {0}", filePath));

        return true;
    }

    private bool ExtractModelName(string line, ref string modelName)
    {
        string[] words = line.Split(' ');

        if (words.Length < 3)
            return false;

        if (!words[0].Equals(LdConstant.TAG_COMMENT, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!words[1].Equals(LdConstant.TAG_FILE, StringComparison.OrdinalIgnoreCase))
            return false;

        modelName = words[2];
        for (int i = 3; i < words.Length; ++i)
        {
            modelName += words[i];
        }

        modelName.Replace(@"\", @"/").Trim();

        return true;
    }

    void LoadCacheFiles(string fileName)
    {
        string cacheFileName = fileName.Replace(@"\", @"/").ToLower();

        LdFileParser.FileLines fileLines;
        HashSet<string> localSubFileNames = new HashSet<string>();

        if (fileCache.TryGetValue(cacheFileName, out fileLines))
        {
            if (fileLines.LoadCompleted)
                return;

            LdSubFileNameLoader.ExtractSubFileNames(fileLines.cache.ToArray(), ref localSubFileNames);
            fileCache[cacheFileName].LoadCompleted = true;
        }
        else
        {
            Queue<string> pathQueue;
            if (!partsPathCache.TryGetValue(cacheFileName, out pathQueue) || pathQueue.Count==0)
            {
                Debug.Log(string.Format("Parts path has no {0}", cacheFileName));
                return;
            }

            string val = pathQueue.Peek();
            string filePath = Path.Combine(GetBasePartImportPath(), val);

            FileLoader fileLoader = new FileLoader();
            if (!fileLoader.LoadFile(filePath))
                return;

            string[] readText;
            if (!fileLoader.GetSplitLines(out readText))
                return;

            LdSubFileNameLoader.ExtractSubFileNames(readText, ref localSubFileNames);
            fileCache.Add(cacheFileName, new LdFileParser.FileLines(val, readText));
        }
        if (localSubFileNames.Count > 0)
        {
            subFileNames.UnionWith(localSubFileNames);
        }
    }

    private string LoadLDRFiles(string[] readText)
    {
        string mainModelName = string.Empty;
        string modelName = string.Empty;
        string cacheModelName = string.Empty;

        fileCache.Clear();

        for (int i = 0; i < readText.Length; ++i)
        {
            if (ExtractModelName(readText[i], ref modelName))
            {
                cacheModelName = modelName.Replace(@"\", @"/").ToLower();

                if (mainModelName.Length == 0)
                    mainModelName = modelName;

                fileCache.Add(cacheModelName, new LdFileParser.FileLines());
                //Debug.Log(string.Format("Add ldr model into file cache: {0}", cacheModelName));
            }

            if (cacheModelName != null)
                fileCache[cacheModelName].cache.Add(readText[i]);
        }

        //Debug.Log(string.Format("File cache size after adding ldr files: {0}", fileCache.Count.ToString()));

        return mainModelName;
    }

    public bool LoadMPDFile(out string mainModelName)
    {
        mainModelName = string.Empty;

        string openDir = Path.Combine(Path.Combine(Application.dataPath, "LdrawData"), "LdModels");
        string filePath = EditorUtility.OpenFilePanel("Import Ldraw Model", openDir, "mpd");
        string fileName = Path.GetFileName(filePath);
        string ext = Path.GetExtension(fileName);
        if (!ext.Equals(LdConstant.TAG_MPD_FILE_EXT, StringComparison.OrdinalIgnoreCase))
            return false;

        FileLoader fileLoader = new FileLoader();
        if (!fileLoader.LoadFile(filePath))
            return false;

        string[] readText;
        if (!fileLoader.GetSplitLines(out readText))
            return false;

        stopWatch.Start();
        mainModelName = LoadLDRFiles(readText);
        stopWatch.Stop();
        Debug.Log("Load ldr files: " + stopWatch.ElapsedMilliseconds + " ms");

        stopWatch.Start();
        int cc = 0;
        if (mainModelName.Length > 0)
        {
            subFileNames = new HashSet<string> { mainModelName };

            while (subFileNames.Count > 0)
            {
                cc++;
                var fname = subFileNames.FirstOrDefault();
                if (fname != null)
                {
                    subFileNames.Remove(fname);
                }
                LoadCacheFiles(fname);
            }

            Debug.Log(string.Format("Loading finished: {0} file caches", fileCache.Count.ToString()));
        }
        stopWatch.Stop();

        Debug.Log(string.Format("Load subfile: loop cnt {0}, {1} msec", cc, stopWatch.ElapsedMilliseconds));

        return true;
    }

    public BrickMesh Load(bool usePartAsset)
    {
        string mainModelName;

        if (!LoadMPDFile(out mainModelName))
            return null;

        LdFileParser.FileLines val;
        string cacheModelName = mainModelName.Replace(@"\", @"/").ToLower();
        if (!fileCache.TryGetValue(cacheModelName, out val))
        {
            Debug.Log(string.Format("Cannot find file cache for {0}", mainModelName));
            return null;
        }

        BrickMesh model = null;
        if (!ldFileParser.Start(out model, mainModelName, partsListCache, fileCache, usePartAsset))
            return null;

        Debug.Log(string.Format("Parsing model finished: {0}", mainModelName));
        return model;
    }

    public bool Initialize()
    {
        // cache
        partsListCache = new Dictionary<string, string>();
        partsPathCache = new Dictionary<string, Queue<string>>();
        fileCache = new Dictionary<string, LdFileParser.FileLines>();
        // parser
        ldFileParser = new LdFileParser();
        // stop watch
        stopWatch = new System.Diagnostics.Stopwatch();

        if (!LoadPartsListFile())
            return false;

        return LoadPartsPathFile();
    }
}

#endif
