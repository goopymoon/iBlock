using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;

public class LdColorTable : MonoBehaviour
{
    private readonly Color32 DEF_BRICK_COLOR = new Color32(0x7F, 0x7F, 0x7F, 0xFF);
    private Dictionary<int, Color32> _palette;

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
                    container.name = "LdColorTableContainer";
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
    }

    public bool Initialize()
    {
        _palette = new Dictionary<int, Color32>();

        string filePath = Path.Combine(Application.streamingAssetsPath, "LDConfig.ldr");
        if (!File.Exists(filePath))
        {
            Debug.Log(string.Format("File does not exists: {0}", filePath));
            return false;
        }

        string[] readText = File.ReadAllLines(filePath);
        ParseColor(readText, ref _palette);

        return true;
    }
}
