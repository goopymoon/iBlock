using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Text.RegularExpressions;

public class AsyncFileLoader {

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

    public IEnumerator LoadFile(string filePath)
    {
        readString = string.Empty;

        if (filePath.Contains("://"))
        {
            WWW www = new WWW(filePath);
            new WWW(filePath);
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.Log(string.Format("{0}: {1}", filePath, www.error));
                yield break;
            }
            readString = www.text;
        }
        else
        {
            if (!File.Exists(filePath))
            {
                Debug.Log(string.Format("File does not exists: {0}", filePath));
                yield break;
            }
            readString = File.ReadAllText(filePath);
        }

        readString = Regex.Replace(readString, @"\r\n?|\n", Environment.NewLine);

        //Debug.Log(string.Format("{0}: loaded file length {1}", filePath, readString.Length));
    }
}
