using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;
using System.Text;

public class LdColorLoader
{
    public const string TAG_COMMENT = "0";
    public const string TAG_COLOR = "!COLOUR";
    public const string TAG_CODE = "CODE";
    public const string TAG_VALUE = "VALUE";
    public const string TAG_EDGE = "EDGE";
    public const string TAG_ALPHA = "ALPHA";

    public readonly string COLOR_REL_PATH = Path.Combine("..", "LdParts");
    public readonly string COLOR_CFG_FNAME = "LDConfig.ldr";

    public readonly Color32 DefBrickColor;

    public LdColorLoader()
    {
        DefBrickColor.r = 127;
        DefBrickColor.g = 127;
        DefBrickColor.b = 127;
        DefBrickColor.a = 255;
    }

    private bool ParseColor(string[] readText, ref Dictionary<int, Color32> palette)
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
                if (!words[1].Equals(TAG_COLOR, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!words[3].Equals(TAG_CODE, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!words[5].Equals(TAG_VALUE, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!words[7].Equals(TAG_EDGE, StringComparison.OrdinalIgnoreCase))
                    continue;

                int code = Int32.Parse(words[4]);

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

                Color32 color;
                color.r = Convert.ToByte(Convert.ToInt32(RBuilder.ToString(), 16));
                color.g = Convert.ToByte(Convert.ToInt32(GBuilder.ToString(), 16));
                color.b = Convert.ToByte(Convert.ToInt32(BBuilder.ToString(), 16));
                color.a = 255;
                if (words.Length == 11)
                {
                    if (!words[9].Equals(TAG_ALPHA, StringComparison.OrdinalIgnoreCase))
                        continue;
                    color.a = Convert.ToByte(words[10]);
                }

                palette.Add(code, color);
            }
        }

        return true;
    }

    public bool Load(string fileName, ref Dictionary<int, Color32> palette)
    {
        var path = Path.Combine(Application.dataPath, COLOR_REL_PATH);
        var filePath = Path.Combine(path, fileName);

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File does not exists: {0}", filePath);
            return false;
        }

        string[] readText = File.ReadAllLines(filePath);

        return ParseColor(readText, ref palette);
    }
}
