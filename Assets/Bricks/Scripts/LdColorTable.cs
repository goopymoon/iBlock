using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class LdColorTable : MonoBehaviour
{
	public bool isReady { get; private set; }

    private readonly Color32 DEF_BRICK_COLOR = new Color32(0x7F, 0x7F, 0x7F, 0xFF);
    private Dictionary<int, Color32> _palette;

    // member variables used by file loading coroutine
    private bool fileLoadResult = true;
    private string readString;

    private static LdColorTable _instance;
    public static LdColorTable Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType(typeof(LdColorTable)) as LdColorTable;
                if (_instance == null)
                {
                    GameObject container = new GameObject();
                    container.name = "LdColorTable";
                    _instance = container.AddComponent(typeof(LdColorTable)) as LdColorTable;
                }
            }

            return _instance;
        }
    }

    public Color32 GetColor(int colorIndex)
    {
        if (_palette.ContainsKey(colorIndex))
            return _palette[colorIndex];
        else
            return DEF_BRICK_COLOR;
    }

    private void ParseColor(string[] readText, ref Dictionary<int, Color32> palette)
    {
        for (int i = 0; i < readText.Length; ++i)
        {
            string line = readText[i];

            line.Replace("\t", " ");
            string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 9)
                continue;

            decimal lineType;
            if (decimal.TryParse(words[0], out lineType) && lineType == 0)
            {
                if (!words[1].Equals(LdConstant.TAG_COLOR, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!words[3].Equals(LdConstant.TAG_CODE, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!words[5].Equals(LdConstant.TAG_VALUE, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!words[7].Equals(LdConstant.TAG_EDGE, StringComparison.OrdinalIgnoreCase))
                    continue;

                int code = Int32.Parse(words[4]);

                if (code == LdConstant.LD_COLOR_MAIN) continue;
                if (code == LdConstant.LD_COLOR_EDGE) continue;

                string hexVal = words[6];
                var RBuilder = new StringBuilder();
                var GBuilder = new StringBuilder();
                var BBuilder = new StringBuilder();
                for (int pos = 0; pos < hexVal.Length; ++pos)
                {
                    if (pos == 0) continue;
                    if (pos == 1 || pos == 2)
                    {
                        RBuilder.Append(hexVal[pos]);
                    }
                    if (pos == 3 || pos == 4)
                    {
                        GBuilder.Append(hexVal[pos]);
                    }
                    if (pos == 5 || pos == 6)
                    {
                        BBuilder.Append(hexVal[pos]);
                    }
                }

                Color32 color = new Color32();
                color.r = Convert.ToByte(Convert.ToInt32(RBuilder.ToString(), 16));
                color.g = Convert.ToByte(Convert.ToInt32(GBuilder.ToString(), 16));
                color.b = Convert.ToByte(Convert.ToInt32(BBuilder.ToString(), 16));
                color.a = 255;
                if (words.Length == 11)
                {
                    if (!words[9].Equals(LdConstant.TAG_ALPHA, StringComparison.OrdinalIgnoreCase))
                        continue;
                    color.a = Convert.ToByte(words[10]);
                }

                palette.Add(code, color);
            }
        }

		Debug.Log (string.Format ("Parsing color table is finished."));
    }

    public void Initialize()
    {
        isReady = false;
        _palette = new Dictionary<int, Color32>();

        StartCoroutine("LoadLdConfigFile");
    }

    IEnumerator LoadLdConfigFile()
    { 
		string filePath = Path.Combine(Application.streamingAssetsPath, "LDConfig.ldr");

		yield return StartCoroutine ("LoadFile", filePath);
        if (!fileLoadResult)
            yield break;
        while (readString.Length == 0)
            yield return null;

        string[] readText = readString.Split(
            Environment.NewLine.ToCharArray(),
            StringSplitOptions.RemoveEmptyEntries);

        ParseColor(readText, ref _palette);

        isReady = true;
    }

    IEnumerator LoadFile(string filePath)
	{
		readString = string.Empty;
        fileLoadResult = true;

        if (filePath.Contains("://"))
        {
            WWW www = new WWW(filePath);
            new WWW(filePath);
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                fileLoadResult = false;
                Debug.Log(www.error);
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

        Debug.Log (string.Format ("{0}: loaded string length {1}", filePath, readString.Length));
	}
}
