using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;
using System.Linq;

public class LdModelLoader : MonoBehaviour
{
	public BrickMesh model;

    // parts list contains only recommended parts
    private Dictionary<string, string> partsListCache;
    // parts path contains paths of all parts
    private Dictionary<string, Queue<string>> partsPathCache;

    private Dictionary<string, LdFileParser.FileLines> fileCache;
    private LdFileParser ldFileParser;

    private string mainModelName;
    private HashSet<string> subFileNames;
    private bool usePartAsset = false;

    private string GetBaseImportPath()
    {
        return Path.Combine(Application.streamingAssetsPath, LdConstant.LD_PARTS_PATH);
    }

    public IEnumerator LoadPartsListFile()
    {
        StopWatch stopWatch = new StopWatch("LoadPartsListFile");

        var filePath = Path.Combine(GetBaseImportPath(), LdConstant.PARTS_LIST_FNAME);

        AsyncFileLoader afileLoader = new AsyncFileLoader();
        yield return StartCoroutine(afileLoader.LoadFile(filePath));

        string[] readText;
        if (!afileLoader.GetSplitLines(out readText))
            yield break;

        for (int i = 0; i < readText.Length; ++i)
        {
            string[] words = readText[i].Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0)
                partsListCache.Add(words[0], "");
        }

        Debug.Log(string.Format("Parts list cache is ready.", filePath));

        stopWatch.EndTick();
    }

    public IEnumerator LoadPartsPathFile()
    {
        StopWatch stopWatch = new StopWatch("LoadPartsPathFile");

        var filePath = Path.Combine(GetBaseImportPath(), LdConstant.PARTS_PATH_LIST_FNAME);

        AsyncFileLoader afileLoader = new AsyncFileLoader();
        yield return StartCoroutine(afileLoader.LoadFile(filePath));

        string[] readText;
        if (!afileLoader.GetSplitLines(out readText))
            yield break;

        for (int i = 0; i < readText.Length; ++i)
        {
            string[] words = readText[i].Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
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

        Debug.Log(string.Format("Parts path cache is ready.", filePath));

        stopWatch.EndTick();
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

    IEnumerator LoadCacheFiles(string fileName)
    {
        string cacheFileName = fileName.Replace(@"\", @"/").ToLower();

        if (usePartAsset && partsListCache.ContainsKey(cacheFileName))
            yield break;

        LdFileParser.FileLines fileLines;
        HashSet<string> localSubFileNames = new HashSet<string>();

        if (fileCache.TryGetValue(cacheFileName, out fileLines))
        {
            if (fileLines.loadCompleted)
                yield break;

            LdSubFileNameLoader.ExtractSubFileNames(fileLines.cache.ToArray(), ref localSubFileNames, usePartAsset, partsListCache);
            fileCache[cacheFileName].loadCompleted = true;
        }
        else
        {
            Queue<string> pathQueue;
            if (!partsPathCache.TryGetValue(cacheFileName, out pathQueue) || pathQueue.Count==0)
            {
                Debug.Log(string.Format("Parts path has no {0}", cacheFileName));
                yield break;
            }

            string val = pathQueue.Peek();
            string filePath = Path.Combine(GetBaseImportPath(), val);

            AsyncFileLoader afileLoader = new AsyncFileLoader();
            yield return StartCoroutine(afileLoader.LoadFile(filePath));

            string[] readText;
            if (!afileLoader.GetSplitLines(out readText))
                yield break;

            LdSubFileNameLoader.ExtractSubFileNames(readText, ref localSubFileNames, usePartAsset, partsListCache);
            fileCache.Add(cacheFileName, new LdFileParser.FileLines(val, readText));
        }
        if (localSubFileNames.Count > 0)
        {
            subFileNames.UnionWith(localSubFileNames);
        }
    }

    public IEnumerator LoadMPDFile(string fileName)
    {
        StopWatch stopWatch = new StopWatch();

        mainModelName = string.Empty;

        string ext = Path.GetExtension(fileName);
        if (!ext.Equals(LdConstant.TAG_MPD_FILE_EXT, StringComparison.OrdinalIgnoreCase))
            yield break;

		var ModelPath = Path.Combine (Application.streamingAssetsPath, "LdModels");
		var filePath = Path.Combine (ModelPath, fileName);

        AsyncFileLoader afileLoader = new AsyncFileLoader();
        yield return StartCoroutine(afileLoader.LoadFile(filePath));

        string[] readText;
        if (!afileLoader.GetSplitLines(out readText))
            yield break;

        stopWatch.StartTick("Load ldr files");
		mainModelName = LoadLDRFiles(readText);
        stopWatch.EndTick();

        stopWatch.StartTick("Load sub files");
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
                yield return StartCoroutine("LoadCacheFiles", fname);
            }

            Debug.Log(string.Format("Loading finished: {0} file caches", fileCache.Count.ToString()));
        }
        Debug.Log(string.Format("subfile loop cnt {0}", cc));
        stopWatch.EndTick();
    }

    public IEnumerator Load(string fileName, bool usePreparedPartAsset)
    {
        usePartAsset = usePreparedPartAsset;

        yield return StartCoroutine("LoadPartsListFile");
        yield return StartCoroutine("LoadPartsPathFile");
        yield return StartCoroutine("LoadMPDFile", fileName);

        LdFileParser.FileLines val;
        string cacheModelName = mainModelName.Replace(@"\", @"/").ToLower();
        if (!fileCache.TryGetValue(cacheModelName, out val))
        {
            Debug.Log(string.Format("Cannot find file cache for {0}", mainModelName));
            yield break;
        }

        if (!ldFileParser.Start(out model, mainModelName, partsPathCache, fileCache, Matrix4x4.identity, usePartAsset))
            yield break;

        Debug.Log(string.Format("Parsing model finished: {0}", mainModelName));
    }

    private void Awake()
    {
        partsListCache = new Dictionary<string, string>();
        partsPathCache = new Dictionary<string, Queue<string>>();
        fileCache = new Dictionary<string, LdFileParser.FileLines>();
        ldFileParser = new LdFileParser();
        model = null;
    }
}
