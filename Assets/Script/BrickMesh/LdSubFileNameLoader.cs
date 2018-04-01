using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Text;

public class LdSubFileNameLoader
{
    public static void ExtractSubFileNames(string[] readText, ref HashSet<string> subFileNames)
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
                    continue;
                }

                string fname = words[14];
                for (int j = 15; j < words.Length; ++j)
                {
                    fname += words[j];
                }

                subFileNames.Add(fname.ToLower());
            }
        }
    }
}
