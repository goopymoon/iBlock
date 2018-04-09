using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

public class LdFileParser
{
    public class FileLines
    {
        public bool LoadCompleted { get; set; }
        public string FilePath { get; private set; }
        public List<string> cache = new List<string>();

        public FileLines()
        {
            LoadCompleted = false;
            FilePath = "";
        }

        public FileLines(string path, string[] lines)
        {
            LoadCompleted = true;

            FilePath = path;
            cache.AddRange(lines);
        }

        public void Add(string line)
        {
            cache.Add(line);
        }
    }

    private enum eCertified { NA = 0, TRUE, FALSE };
    private enum eWinding { CCW = 0, CW };

    private bool isUsingPartsAsset;
    private Dictionary<string, string> partListCache;
    private Dictionary<string, FileLines> fileCache;

    static System.Diagnostics.Stopwatch stopWatch;

    private Vector3 ParseVector(string[] words, ref int offset)
    {
        Vector3 v;

        // Unity is LHS
        v.x = float.Parse(words[offset++]);
        v.y = -1 * float.Parse(words[offset++]);
        v.z = float.Parse(words[offset++]);

        return v;
    }

    // Ldraw matrix
    // / a b c x \
    // | d e f y |
    // | g h i z |
    // \ 0 0 0 1 /
    private Matrix4x4 ParseMatrix(string[] words, ref int offset)
    {
        Matrix4x4 m;

        float x = float.Parse(words[offset++]);
        float y = float.Parse(words[offset++]);
        float z = float.Parse(words[offset++]);
        float a = float.Parse(words[offset++]);
        float b = float.Parse(words[offset++]);
        float c = float.Parse(words[offset++]);
        float d = float.Parse(words[offset++]);
        float e = float.Parse(words[offset++]);
        float f = float.Parse(words[offset++]);
        float g = float.Parse(words[offset++]);
        float h = float.Parse(words[offset++]);
        float i = float.Parse(words[offset++]);

        // Unity is LHS
        m.m00 = a;
        m.m01 = -1 * b;
        m.m02 = c;
        m.m03 = x;
        m.m10 = -1 * d;
        m.m11 = e;
        m.m12 = -1 * f;
        m.m13 = -1 * y;
        m.m20 = g;
        m.m21 = -1 * h;
        m.m22 = i;
        m.m23 = z;
        m.m30 = 0;
        m.m31 = 0;
        m.m32 = 0;
        m.m33 = 1;

        return m;
    }

    private bool ParseBFCInfo(string line, ref eCertified certified, ref eWinding winding, ref bool invertNext)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
            return false;

        int offset = 1;

        if (!words[offset++].Equals(LdConstant.TAG_BFC, StringComparison.OrdinalIgnoreCase))
            return false;

        if (words[offset].Equals(LdConstant.TAG_NOCERTIFY, StringComparison.OrdinalIgnoreCase))
        {
            offset++;

            Debug.Assert(certified != eCertified.TRUE, "Previous Certificate should not be TRUE.");

            if (certified == eCertified.NA)
            {
                certified = eCertified.FALSE;
                return true;
            }
        }
        else if (words[offset].Equals(LdConstant.TAG_CERTIFY, StringComparison.OrdinalIgnoreCase))
        {
            offset++;

            Debug.Assert(certified != eCertified.FALSE, "Previous Certificate should not be FALSE.");

            if (certified == eCertified.NA)
                certified = eCertified.TRUE;
        }

        if (offset >= words.Length)
        {
            winding = eWinding.CCW;
            return true;
        }

        if (words[offset].Equals(LdConstant.TAG_CW, StringComparison.OrdinalIgnoreCase))
        {
            offset++;
            winding = eWinding.CW;
            return true;

        }
        else if (words[offset].Equals(LdConstant.TAG_CCW, StringComparison.OrdinalIgnoreCase))
        {
            offset++;
            winding = eWinding.CCW;
            return true;
        }
        else if (words[offset].Equals(LdConstant.TAG_INVERTNEXT, StringComparison.OrdinalIgnoreCase))
        {
            offset++;
            invertNext = true;
            return true;
        }

        return false;
    }

    // Does not support hexadecimal format
    private bool ParseColor(string word, out short color)
    {
        color = LdConstant.LD_COLOR_MAIN;

        int result = 0;
        if (!int.TryParse(word, out result))
            return false;

        color = (short)result;
        return true;
    }

    private bool ParseTriInfo(ref BrickMesh brickMesh, string line, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 11)
            return false;

        int offset = 1;

        short localColor;
        if (!ParseColor(words[offset++], out localColor))
            return false;

        Vector3 v1 = ParseVector(words, ref offset);
        Vector3 v2 = ParseVector(words, ref offset);
        Vector3 v3 = ParseVector(words, ref offset);

        bool renderWinding = (winding == eWinding.CW);
        return brickMesh.PushTriangle(localColor, ref v1, ref v2, ref v3, renderWinding);
    }

    private bool ParseQuadInfo(ref BrickMesh brickMesh, string line, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 14)
            return false;

        int offset = 1;

        short localColor;
        if (!ParseColor(words[offset++], out localColor))
            return false;

        Vector3 v1 = ParseVector(words, ref offset);
        Vector3 v2 = ParseVector(words, ref offset);
        Vector3 v3 = ParseVector(words, ref offset);
        Vector3 v4 = ParseVector(words, ref offset);

        bool renderWinding = (winding == eWinding.CW);
        return brickMesh.PushQuad(localColor, ref v1, ref v2, ref v3, ref v4, renderWinding);
    }

    private bool ParseSubFileInfo(ref BrickMesh brickMesh, string line, bool accumInvert)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 15)
            return false;

        int offset = 1;

        short localColor;
        if (!ParseColor(words[offset++], out localColor))
            return false;

        Matrix4x4 mLocal = ParseMatrix(words, ref offset);
        string fname = words[offset];
        for (int i = offset + 1; i < words.Length; ++i)
            fname += words[i];

        return TryParseModel(ref brickMesh, fname, mLocal, localColor, accumInvert);
    }

    private bool ParseModel(ref BrickMesh brickMesh, string[] readText)
    {
        eCertified certified = eCertified.NA;
        eWinding winding = eWinding.CCW;
        bool invertNext = false;

        brickMesh.CreateMeshInfo();

        for (int i = 0; i < readText.Length; ++i)
        {
            string line = readText[i];

            line.Replace("\t", " ");
            line = line.Trim();

            if (line.Length == 0)
                continue;

            int lineType = (int)Char.GetNumericValue(line[0]);
            switch (lineType)
            {
                case 0:
                    ParseBFCInfo(line, ref certified, ref winding, ref invertNext);
                    break;
                case 1:
                    if (!ParseSubFileInfo(ref brickMesh, line, invertNext))
                    {
                        Debug.Log(string.Format("ParseSubFileInfo failed: {0}", line));
                        return false;
                    }
                    invertNext = false;
                    break;
                case 3:
                    if (!ParseTriInfo(ref brickMesh, line, winding))
                    {
                        Debug.Log(string.Format("ParseTriInfo failed: {0}", line));
                        return false;
                    }
                    break;
                case 4:
                    if (!ParseQuadInfo(ref brickMesh, line, winding))
                    {
                        Debug.Log(string.Format("ParseQuadInfo failed: {0}", line));
                        return false;
                    }
                    break;
                default:
                    break;
            }
        }

        brickMesh.FinalizeBrickMeshInfo(certified == eCertified.TRUE);

        return true;
    }

    private bool IsMergeNeeded(BrickMesh parentMesh, string fileName)
    {
        if (!isUsingPartsAsset)
            return false;

        string ext = Path.GetExtension(fileName).ToLower();
        return (ext == ".dat" && !partListCache.ContainsKey(fileName));
    }

    private bool TryParseModel(ref BrickMesh parentMesh, string fileName, Matrix4x4 trMatrix,
        short parentColor = LdConstant.LD_COLOR_MAIN, bool accInvertNext = false)
    {
        string canonicalName = fileName.Replace(@"\", @"/").ToLower();

        BrickMesh subBrickMesh = null;    
        BrickMesh.Create(canonicalName, out subBrickMesh);
        if (!subBrickMesh.IsRegisteredMeshInfo())
        {
            FileLines val;
            if (!fileCache.TryGetValue(canonicalName, out val))
            {
                Debug.Log(string.Format("Cannot find file cache for {0}", canonicalName));
                return false;
            }

            if (!ParseModel(ref subBrickMesh, val.cache.ToArray()))
                return false;
        }

        if (parentMesh == null)
        {
            parentMesh = subBrickMesh;
        }
        else
        {
            subBrickMesh.SetProperties(trMatrix, accInvertNext, parentColor);

            if (IsMergeNeeded(parentMesh, canonicalName))
            {
                parentMesh.MergeChildBrick(subBrickMesh);
            }
            else
            {
                parentMesh.AddChildBrick(subBrickMesh);
            }
        }

        return true;
    }

    public bool Start(out BrickMesh brickMesh, string modelName, Dictionary<string, string> partList, 
        Dictionary<string, FileLines> fCache, bool usePartsAsset)
    {
        isUsingPartsAsset = usePartsAsset;
        partListCache = partList;
        fileCache = fCache;

        brickMesh = null;

        stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();
        bool ret = TryParseModel(ref brickMesh, modelName, Matrix4x4.identity);
        stopWatch.Stop();
        Debug.Log("Parsing Model: " + stopWatch.ElapsedMilliseconds + " ms");

        return ret;
    }
}
