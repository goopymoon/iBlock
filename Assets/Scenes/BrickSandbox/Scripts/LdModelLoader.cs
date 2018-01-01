using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;

public class LdModelLoader : MonoBehaviour
{
	public BrickMesh model;

    // parts list contains only recommended parts
    private Dictionary<string, string> partsListCache;
    // parts path contains paths of all parts
    private Dictionary<string, string> partsPathCache;

    private Dictionary<string, LdFileParser.FileLines> fileCache;
    private LdFileParser ldFileParser;

    private string mainModelName;
    private List<string> subFileNames;
    private bool usePartAsset = false;

    private string GetBaseImportPath()
    {
        return Path.Combine(Application.streamingAssetsPath, LdConstant.LD_PARTS_PATH);
    }

    public IEnumerator LoadPartsListFile()
    {
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
    }

    public IEnumerator LoadPartsPathFile()
    {
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
            if (words.Length == 2)
                partsPathCache.Add(words[0], words[1]);
        }

        Debug.Log(string.Format("Parts path cache is ready.", filePath));
    }

    private bool ExtractSubFileNames(string[] readText, ref List<string> subFileNames)
    {
        for (int i = 0; i < readText.Length; ++i)
        {
            string line = readText[i];

            line.Replace("\t", " ");
            line = line.Trim();

            if (line.Length == 0)
                continue;

            int lineType = (int)Char.GetNumericValue(line[0]);
            if (lineType == 1)
            {
                string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 15)
                {
                    Debug.Log(string.Format("Subfile syntax error: {0}", line));
                    return false;
                }

                string fname = words[14];
                for (int j = 15; j < words.Length; ++j)
                {
                    fname += words[j];
                }

                subFileNames.Add(fname.ToLower());
            }
        }

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

        if (usePartAsset)
        {
            if (partsListCache.ContainsKey(cacheFileName))
            {
                //Debug.Log(string.Format("Skip loading part asset from ldraw file: {0}", cacheFileName));
                yield break;
            }
        }

        LdFileParser.FileLines fileLines;
        List<string> localSubFileNames = new List<string>();

        if (fileCache.TryGetValue(cacheFileName, out fileLines))
        {
            if (fileLines.loadCompleted)
                yield break;

            if (!ExtractSubFileNames(fileLines.cache.ToArray(), ref localSubFileNames))
            {
                Debug.Log(string.Format("Extracting sub file failed: {0}", cacheFileName));
                yield break;
            }

            fileCache[cacheFileName].loadCompleted = true;
        }
        else
        {
            string val;
            if (!partsPathCache.TryGetValue(cacheFileName, out val))
            {
                Debug.Log(string.Format("Parts path has no {0}", cacheFileName));
                yield break;
            }

            string filePath = Path.Combine(GetBaseImportPath(), val);

            AsyncFileLoader afileLoader = new AsyncFileLoader();
            yield return StartCoroutine(afileLoader.LoadFile(filePath));

            string[] readText;
            if (!afileLoader.GetSplitLines(out readText))
                yield break;

            if (!ExtractSubFileNames(readText, ref localSubFileNames))
            {
                Debug.Log(string.Format("Extracting sub file failed: {0}", cacheFileName));
                yield break;
            }

            fileCache.Add(cacheFileName, new LdFileParser.FileLines(val, readText));
        }

        if (localSubFileNames.Count != 0)
            subFileNames.AddRange(localSubFileNames);
    }

    public IEnumerator LoadMPDFile(string fileName)
    {
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

		mainModelName = LoadLDRFiles(readText);

        if (mainModelName.Length > 0)
        {
            subFileNames = new List<string>();
            subFileNames.Add(mainModelName);

            Debug.Log(string.Format("Load sub files for: {0}", mainModelName));

            while (subFileNames.Count > 0)
            {
                string fname = subFileNames[0];
                subFileNames.RemoveAt(0);
                yield return StartCoroutine("LoadCacheFiles", fname);
            }

            Debug.Log(string.Format("Loading finished: {0}", fileCache.Count.ToString()));
        }
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

        if (!ldFileParser.Start(out model, mainModelName, fileCache, Matrix4x4.identity, usePartAsset))
            yield break;

        Debug.Log(string.Format("Parsing model finished: {0}", mainModelName));
    }

    private void Awake()
    {
        partsListCache = new Dictionary<string, string>();
        partsPathCache = new Dictionary<string, string>();
        fileCache = new Dictionary<string, LdFileParser.FileLines>();
        ldFileParser = new LdFileParser();
        model = null;
    }
}
