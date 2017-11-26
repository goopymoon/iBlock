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
        public bool loadCompleted { get; set; }
        public string filePath { get; private set; }
        public List<string> cache = new List<string>();

        public FileLines()
        {
            loadCompleted = false;
            filePath = "";
        }

        public FileLines(string path, string[] lines)
        {
            loadCompleted = true;

            filePath = path;
            cache.AddRange(lines);
        }

        public void Add(string line)
        {
            cache.Add(line);
        }
    }

    private enum eCertified { NA = 0, TRUE, FALSE };
    private enum eWinding { CCW = 0, CW };

    private Dictionary<string, FileLines> fileCache;

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

    private bool ParseTriInfo(string line, ref BrickMesh brickMesh,
        Matrix4x4 trMatrix, short parentColor, bool accumInvert, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 11)
            return false;

        int offset = 1;

        short localColor;
        if (!ParseColor(words[offset++], out localColor))
            return false;

        short vtColorIndex = LdConstant.GetEffectiveColorIndex(localColor, parentColor);

        Vector3 v1 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v2 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v3 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));

        int lastIndex = brickMesh.vertices.Count;

        brickMesh.vertices.Add(v1);
        brickMesh.vertices.Add(v2);
        brickMesh.vertices.Add(v3);

        bool renderWinding = (winding == eWinding.CW);
        if (accumInvert) renderWinding = !renderWinding;

        // winding is for RHS so apply reverse for Unity
        if (renderWinding)
        {
            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 1);
            brickMesh.triangles.Add(lastIndex + 2);
        }
        else
        {
            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 2);
            brickMesh.triangles.Add(lastIndex + 1);
        }

        for (int i = 0; i < 3; ++i)
            brickMesh.colorIndices.Add(vtColorIndex);

        return true;
    }

    private bool ParseQuadInfo(string line, ref BrickMesh brickMesh,
        Matrix4x4 trMatrix, short parentColor, bool accumInvert, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 14)
            return false;

        int offset = 1;

        short localColor;
        if (!ParseColor(words[offset++], out localColor))
            return false;

        short vtColorIndex = LdConstant.GetEffectiveColorIndex(localColor, parentColor);

        Vector3 v1 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v2 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v3 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v4 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));

        int lastIndex = brickMesh.vertices.Count;

        brickMesh.vertices.Add(v1);
        brickMesh.vertices.Add(v2);
        brickMesh.vertices.Add(v3);
        brickMesh.vertices.Add(v4);

        bool renderWinding = (winding == eWinding.CW);
        if (accumInvert) renderWinding = !renderWinding;

        if (renderWinding)
        {
            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 1);
            brickMesh.triangles.Add(lastIndex + 2);

            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 2);
            brickMesh.triangles.Add(lastIndex + 3);
        }
        else
        {
            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 2);
            brickMesh.triangles.Add(lastIndex + 1);

            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 3);
            brickMesh.triangles.Add(lastIndex + 2);
        }

        for (int i = 0; i < 4; ++i)
            brickMesh.colorIndices.Add(vtColorIndex);

        return true;
    }

    private bool ParseSubFileInfo(string line, ref BrickMesh brickMesh,
        Matrix4x4 trMatrix, short parentColor, bool accumInvert, bool accumInvertByMatrix)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 15)
            return false;

        int offset = 1;

        short localColor;
        if (!ParseColor(words[offset++], out localColor))
            return false;

        short colorIndex = LdConstant.GetEffectiveColorIndex(localColor, parentColor);

        Matrix4x4 mLocal = ParseMatrix(words, ref offset);
        string fname = words[offset];
        for (int i = offset + 1; i < words.Length; ++i)
            fname += words[i];

        if (mLocal.determinant < 0)
            accumInvertByMatrix = !accumInvertByMatrix;

        Matrix4x4 mAcc = trMatrix * mLocal;
        return ParseModel(fname, ref brickMesh, mAcc, colorIndex, accumInvert, accumInvertByMatrix);
    }

    private bool ParseModel(string fileName, ref BrickMesh parentMesh, Matrix4x4 trMatrix, 
        short parentColor = LdConstant.LD_COLOR_MAIN, bool accInvertNext = false, bool accInvertByMatrix = false, 
        bool merge=true)
    {
        string cacheFileName = fileName.Replace(@"\", @"/").ToLower();

        FileLines val;
        if (!fileCache.TryGetValue(cacheFileName, out val))
        {
            Debug.Log(string.Format("Cannot find file cache for {0}", cacheFileName));
            return false;
        }

        BrickMesh subBrickMesh = null;
        if (!ParseModel(out subBrickMesh, fileName, val.cache.ToArray(), Matrix4x4.identity))
        {
            return false;
        }
        else
        {
            subBrickMesh.Optimize();

            if (merge)
                parentMesh.MergeChildBrick(accInvertNext, accInvertByMatrix, parentColor, trMatrix, subBrickMesh);
            else
                parentMesh.AddChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh);

            return true;
        }
    }

    private bool ParseModel(out BrickMesh brickMesh, string modelName, string[] readText, 
    Matrix4x4 trMatrix, short parentColor = LdConstant.LD_COLOR_MAIN, bool accumInvert = false, bool accumInvertByMatrix = false)
    {
        brickMesh = new BrickMesh(modelName);

        eCertified certified = eCertified.NA;
        eWinding winding = eWinding.CCW;
        bool invertNext = false;

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
                    if (!ParseSubFileInfo(line, ref brickMesh, trMatrix, parentColor, invertNext ^ accumInvert, accumInvertByMatrix))
                    {
                        Debug.Log(string.Format("ParseSubFileInfo failed: {0}", line));
                        return false;
                    }
                    invertNext = false;
                    break;
                case 3:
                    if (!ParseTriInfo(line, ref brickMesh, trMatrix, parentColor, accumInvert, winding))
                    {
                        Debug.Log(string.Format("ParseTriInfo failed: {0}", line));
                        return false;
                    }
                    break;
                case 4:
                    if (!ParseQuadInfo(line, ref brickMesh, trMatrix, parentColor, accumInvert, winding))
                    {
                        Debug.Log(string.Format("ParseQuadInfo failed: {0}", line));
                        return false;
                    }
                    break;
                default:
                    break;
            }
        }

        brickMesh.bfcEnabled = certified == eCertified.TRUE;

        //Debug.Log(string.Format("Parsing model finished: {0}", modelName));

        return true;
    }

    public bool ParseModel(out BrickMesh brickMesh, string modelName, string[] readText, Dictionary<string, FileLines> fCache,
        Matrix4x4 trMatrix, short parentColor = LdConstant.LD_COLOR_MAIN, bool accumInvert = false, bool accumInvertByMatrix = false)
    {
        fileCache = fCache;
        return ParseModel(out brickMesh, modelName, readText, trMatrix, parentColor, accumInvert, accumInvertByMatrix);
    }
}
