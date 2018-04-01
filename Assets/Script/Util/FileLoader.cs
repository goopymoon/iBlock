using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Text.RegularExpressions;

public class FileLoader
{
    private string readString;

    public bool GetSplitLines(out string[] lines)
    {
        lines = null;

        if (readString.Length > 0)
        {
            lines = readString.Split(
                Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            return (lines.Length > 0);
        }

        return false;
    }

    public bool LoadFile(string filePath)
    {
        readString = string.Empty;

        if (filePath.Contains("://"))
        {
            Debug.Log(string.Format("Does not support url: {0}", filePath));
            return false;
        }
        else
        {
            if (!File.Exists(filePath))
            {
                Debug.Log(string.Format("File does not exists: {0}", filePath));
                return false;
            }
            readString = File.ReadAllText(filePath);
        }

        readString = Regex.Replace(readString, @"\r\n?|\n", Environment.NewLine);
        //Debug.Log(string.Format("{0}: loaded file length {1}", filePath, readString.Length));

        return true;
    }
}
