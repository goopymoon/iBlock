using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text.RegularExpressions;

public class LdPartsLoader
{
    public bool IsInitialized { get; private set; }

    private List<string> subFileNames = null;
    private Dictionary<string, LdFileParser.FileLines> fileCache = null;
    private LdFileParser ldFileParser;

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

    private bool LoadSubFiles(string fileName, string basePath, Dictionary<string, string> canonicalPathCache)
    {
        string cacheFileName = fileName.Replace(@"\", @"/").ToLower();

        LdFileParser.FileLines fileLines;
        List<string> localSubFileNames = new List<string>();

        if (fileCache.TryGetValue(cacheFileName, out fileLines))
        {
            if (fileLines.loadCompleted)
                return true;

            if (!ExtractSubFileNames(fileLines.cache.ToArray(), ref localSubFileNames))
            {
                Debug.Log(string.Format("Extracting sub file failed: {0}", cacheFileName));
                return false;
            }

            fileCache[cacheFileName].loadCompleted = true;
        }
        else
        {
            string val;
            if (!canonicalPathCache.TryGetValue(cacheFileName, out val))
            {
                Debug.Log(string.Format("Parts list has no {0}", cacheFileName));
                return false;
            }

            var filePath = Path.Combine(basePath, val);
            string readString = null;

            if (!LoadFile(filePath, ref readString))
                return false;

            if (readString.Length == 0)
                return false;

            string[] readText = readString.Split(
                Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (!ExtractSubFileNames(readText, ref localSubFileNames))
            {
                Debug.Log(string.Format("Extracting sub file failed: {0}", cacheFileName));
                return false;
            }

            fileCache.Add(cacheFileName, new LdFileParser.FileLines(val, readText));
        }

        if (localSubFileNames.Count != 0)
            subFileNames.AddRange(localSubFileNames);

        return true;
    }

    public bool LoadPart(out BrickMesh brickMesh, string fileName, string basePath, Dictionary<string, string> canonicalPathCache)
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
            string fname = subFileNames[0];
            subFileNames.RemoveAt(0);
            if (!LoadSubFiles(fname, basePath, canonicalPathCache))
                return false;
        }

        if (!ldFileParser.Start(out brickMesh, fileName, canonicalPathCache, fileCache, Matrix4x4.identity, false))
            return false;

        //Debug.Log(string.Format("Parsing model finished: {0}", fileName));

        return true;
    }

    public bool Initialize()
    {
        fileCache = new Dictionary<string, LdFileParser.FileLines>();
        subFileNames = new List<string>();
        ldFileParser = new LdFileParser();

        IsInitialized = true;

        return true;
    }
}
