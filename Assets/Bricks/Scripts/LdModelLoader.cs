﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

public class LdModelLoader
{
    private enum eCertified { NA = 0, TRUE, FALSE };
    private enum eWinding   { CCW = 0, CW };

    private Dictionary<string, Tuple<string, bool>> pathCache;
    private Dictionary<string, List<string>> ldrCache;
    private Dictionary<string, BrickMesh> brickCache;

    public LdModelLoader()
    {
        pathCache = new Dictionary<string, Tuple<string, bool>>();
        ldrCache = new Dictionary<string, List<string>>();
        brickCache = new Dictionary<string, BrickMesh>();
    }

    public bool Initialize()
    { 
        var filePath = Path.Combine(Application.streamingAssetsPath, "partspath.lst");

        if (!File.Exists(filePath))
        {
            Debug.Log(string.Format("File does not exists: {0}", filePath));
            return false;
        }

        string[] readText = File.ReadAllLines(filePath);

        for (int i = 0; i < readText.Length; ++i)
        {
            string[] words = readText[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 3)
            {
                bool isPart = Convert.ToBoolean(words[2]);
                pathCache.Add(words[0], Tuple.Create<string, bool>(words[1], isPart));
            }
        }

        return true;
    }

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

    private bool ParseSubFileInfo(string line, ref BrickMesh brickMesh, 
        Matrix4x4 trMatrix, short parentColor, bool accumInvert, bool accumInvertByMatrix)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 15)
            return false;

        int offset = 1;

        short localColor = (short)Int32.Parse(words[offset++]);
        short colorIndex = LdConstant.GetEffectiveColorIndex(localColor, parentColor);

        Matrix4x4 mLocal = ParseMatrix(words, ref offset);
        string fname = words[offset];
        for (int i = offset + 1; i < words.Length; ++i)
            fname += words[i];

        if (mLocal.determinant < 0)
            accumInvertByMatrix = !accumInvertByMatrix;

        Matrix4x4 mAcc = trMatrix * mLocal;
        return LoadModel(fname, ref brickMesh, mAcc, colorIndex, accumInvert, accumInvertByMatrix);
    }

    private bool ParseTriInfo(string line, ref BrickMesh brickMesh, 
        Matrix4x4 trMatrix, short parentColor, bool accumInvert, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 11)
            return false;

        int offset = 1;

        short localColor = (short)Int32.Parse(words[offset++]);
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

        short localColor = (short)Int32.Parse(words[offset++]);
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

    private BrickMesh ParseModel(string modelName, string[] readText, Matrix4x4 trMatrix, 
        short parentColor = LdConstant.LD_COLOR_MAIN, bool accumInvert = false, bool accumInvertByMatrix = false)
    {
        BrickMesh brickMesh = new BrickMesh(modelName);

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
                        return null;
                    }
                    invertNext = false;
                    break;
                case 3:
                    if (!ParseTriInfo(line, ref brickMesh, trMatrix, parentColor, accumInvert, winding))
                    {
                        Debug.Log(string.Format("ParseTriInfo failed: {0}", line));
                        return null;
                    }
                    break;
                case 4:
                    if (!ParseQuadInfo(line, ref brickMesh, trMatrix, parentColor, accumInvert, winding))
                    {
                        Debug.Log(string.Format("ParseQuadInfo failed: {0}", line));
                        return null;
                    }
                    break;
                default:
                    break;
            }
        }

        brickMesh.bfcEnabled = certified == eCertified.TRUE;

        return brickMesh;
    }

    private bool LoadModel(string fileName, ref BrickMesh parentMesh, 
        Matrix4x4 trMatrix, short parentColor = LdConstant.LD_COLOR_MAIN, bool accInvertNext = false, bool accInvertByMatrix = false)
    {
        BrickMesh subBrickMesh = null;
        bool isStud = (fileName.IndexOf("stud", StringComparison.CurrentCultureIgnoreCase) != -1);

        Tuple<string, bool> val;
        if (pathCache.TryGetValue(fileName.ToLower(), out val))
        {
            if (brickCache.ContainsKey(fileName))
            {
                subBrickMesh = new BrickMesh(brickCache[fileName]);
            }
            else
            {
                var ldPartsPath = Path.Combine(Application.streamingAssetsPath, "LdParts");
                var filePath = Path.Combine(ldPartsPath, val.Item1);

                if (!File.Exists(filePath))
                {
                    Debug.Log(string.Format("File does not exists: {0}", filePath));
                    return false;
                }

                string[] readText = File.ReadAllLines(filePath);

                subBrickMesh = ParseModel(fileName, readText, Matrix4x4.identity);
                if (subBrickMesh == null)
                    return false;

                subBrickMesh.Optimize();
                brickCache[fileName] = new BrickMesh(subBrickMesh);
            }

            if (val.Item2)
                parentMesh.AddChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh);
            else
                parentMesh.MergeChildBrick(accInvertNext, accInvertByMatrix, parentColor, trMatrix, subBrickMesh, isStud);

            return true;
        }

        if (ldrCache.ContainsKey(fileName))
        {
            if (brickCache.ContainsKey(fileName))
            {
                subBrickMesh = new BrickMesh(brickCache[fileName]);
            }
            else
            {
                subBrickMesh = ParseModel(fileName, ldrCache[fileName].ToArray(), Matrix4x4.identity);
                if (subBrickMesh == null)
                    return false;

                subBrickMesh.Optimize();
                brickCache[fileName] = new BrickMesh(subBrickMesh);
            }

            if (Path.GetDirectoryName(fileName).Length == 0)
                parentMesh.AddChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh);
            else
                parentMesh.MergeChildBrick(accInvertNext, accInvertByMatrix, parentColor, trMatrix, subBrickMesh, isStud);

            return true;
        }

        return false;
    }

    private bool HasModelName(string line, ref string modelName)
    {
        string[] words = line.Split(' ');

        if (words.Length < 3)
            return false;

        if (!words[0].Equals(LdConstant.TAG_COMMENT, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!words[1].Equals(LdConstant.TAG_FILE, StringComparison.OrdinalIgnoreCase))
            return false;

        modelName = words[2];
        for (int i = 3; i < words.Length; ++i)
        {
            modelName += words[i];
        }

        return true;
    }

    private string SplitModel(string[] readText)
    {
        string mainModelName = "";
        string modelName = "";

        ldrCache.Clear();

        for (int i = 0; i < readText.Length; ++i)
        {
            if (HasModelName(readText[i], ref modelName))
            {
                if (mainModelName.Length == 0)
                    mainModelName = modelName;
                ldrCache.Add(modelName, new List<string>());
            }

            if (modelName != null)
                ldrCache[modelName].Add(readText[i]);
        }

        return mainModelName;
    }

    public BrickMesh Load(string fileName)
    {
        BrickMesh brickMesh = null;

        string ext = Path.GetExtension(fileName);
        if (ext.Equals(LdConstant.TAG_MPD_FILE_EXT, StringComparison.OrdinalIgnoreCase))
        {
            var ModelPath = Path.Combine(Application.streamingAssetsPath, "LdModels");
            var filePath = Path.Combine(ModelPath, fileName);

            if (!File.Exists(filePath))
            {
                Debug.Log(string.Format("File does not exists: {0}", filePath));
                return null;
            }

            string[] readText = File.ReadAllLines(filePath);

            string mainModelName = SplitModel(readText);
            if (mainModelName.Length == 0 || !ldrCache.ContainsKey(mainModelName))
            {
                Debug.Log(string.Format("Cannot find main model: {0}", mainModelName));
                return null;
            }

            brickMesh = new BrickMesh(fileName);
            LoadModel(mainModelName, ref brickMesh, Matrix4x4.identity);
        }

        return brickMesh;
    }
}
