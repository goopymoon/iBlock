using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class LdPartsLoader
{
    public bool IsInitialized { get; private set; }

    private LdFileParser ldFileParser;

    private HashSet<string> subFileNames;
    private Dictionary<string, LdFileParser.FileLines> fileCache;

    private bool LoadFile(string filePath, ref string readString)
    {
        if (!File.Exists(filePath))
        {
            Debug.Log(string.Format("File does not exists: {0}", filePath));
            return false;
        }

        readString = File.ReadAllText(filePath);
        readString = Regex.Replace(readString, @"\t", " ");
        readString = Regex.Replace(readString, @"\r\n?|\n", Environment.NewLine);

        //Debug.Log(string.Format("{0}: loaded file length {1}", filePath, readString.Length));
        return true;
    }

    private bool LoadSubFiles(string fileName, string basePath, Dictionary<string, Queue<string>> canonicalPathCache)
    {
        string cacheFileName = fileName.Replace(@"\", @"/").ToLower();

        LdFileParser.FileLines fileLines;
        HashSet<string> localSubFileNames = new HashSet<string>();

        if (fileCache.TryGetValue(cacheFileName, out fileLines))
        {
            if (fileLines.LoadCompleted)
                return true;

            LdSubFileNameLoader.ExtractSubFileNames(fileLines.cache.ToArray(), ref localSubFileNames);
            fileCache[cacheFileName].LoadCompleted = true;
        }
        else
        {
            Queue<string> pathQueue;
            if (!canonicalPathCache.TryGetValue(cacheFileName, out pathQueue) || pathQueue.Count == 0)
            {
                Debug.Log(string.Format("Parts path cache has no {0}", cacheFileName));
                return false;
            }

            var val = pathQueue.First();
            var filePath = Path.Combine(basePath, val);
            string readString = null;

            if (!LoadFile(filePath, ref readString))
                return false;

            if (readString.Length == 0)
                return false;

            string[] readText = readString.Split(
                Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            LdSubFileNameLoader.ExtractSubFileNames(readText, ref localSubFileNames);
            fileCache.Add(cacheFileName, new LdFileParser.FileLines(val, readText));
        }

        if (localSubFileNames.Count > 0)
        {
            subFileNames.UnionWith(localSubFileNames);
        }

        return true;
    }

    public bool LoadPart(out BrickMesh brickMesh, string fileName, string basePath, Dictionary<string, Queue<string>> canonicalPathCache)
    {
        brickMesh = null;

        if (!IsInitialized)
        {
            Debug.Log(string.Format("LdPartsLoader is not initialized"));
            return false;
        }

        if (!LoadSubFiles(fileName, basePath, canonicalPathCache))
            return false;

        while (subFileNames.Count > 0)
        {
            string fname = subFileNames.FirstOrDefault();
            if (fname != null)
            {
                subFileNames.Remove(fname);
            }
            if (!LoadSubFiles(fname, basePath, canonicalPathCache))
                return false;
        }

        Dictionary<string, string> partsListCache = new Dictionary<string, string>();
        return ldFileParser.Start(out brickMesh, fileName, partsListCache, fileCache, true);
    }

    public void Initialize()
    {
        fileCache = new Dictionary<string, LdFileParser.FileLines>();
        subFileNames = new HashSet<string>();
        ldFileParser = new LdFileParser();

        IsInitialized = true;
    }
}
