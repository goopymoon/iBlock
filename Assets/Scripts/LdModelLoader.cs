using UnityEngine;
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

    private readonly string[] PARTS_REL_PATH =
    {
        Path.Combine(Path.Combine("..", "LdParts"), "parts"),
        Path.Combine(Path.Combine("..", "LdParts"), "p"),
    };
    private readonly string MODEL_REL_PATH = Path.Combine("..", "LdModels");

    private Dictionary<string, List<string>> ldrCache;
    private Dictionary<string, BrickMesh> brickCache;

    private eCertified certifed = eCertified.NA;

    public LdModelLoader()
    {
        ldrCache = new Dictionary<string, List<string>>();
        brickCache = new Dictionary<string, BrickMesh>();
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

    private bool ParseBFCInfo(string line, ref eWinding winding, ref bool invertNext)
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

            Debug.Assert(certifed != eCertified.TRUE, "Previous Certificate should not be TRUE.");

            if (certifed == eCertified.NA)
            {
                certifed = eCertified.FALSE;
                return true;
            }
        }
        else if (words[offset].Equals(LdConstant.TAG_CERTIFY, StringComparison.OrdinalIgnoreCase))
        {
            offset++;

            Debug.Assert(certifed != eCertified.FALSE, "Previous Certificate should not be FALSE.");

            if (certifed == eCertified.NA)
                certifed = eCertified.TRUE;
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
        Matrix4x4 trMatrix, byte parentColor, bool accumInvert)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 15)
            return false;

        int offset = 1;

        byte localColor = (byte)Int32.Parse(words[offset++]);
        byte colorIndex = LdConstant.GetEffectiveColorIndex(localColor, parentColor);

        Matrix4x4 mLocal = ParseMatrix(words, ref offset);
        string fname = words[offset];
        for (int i = offset + 1; i < words.Length; ++i)
            fname += words[i];

        if (mLocal.determinant < 0)
            accumInvert = !accumInvert;

        Matrix4x4 mAcc = trMatrix * mLocal;
        return LoadModel(fname, ref brickMesh, mAcc, colorIndex, accumInvert);
    }

    private bool ParseTriInfo(string line, ref BrickMesh brickMesh, 
        Matrix4x4 trMatrix, byte parentColor, bool accumInvert, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 11)
            return false;

        int offset = 1;

        byte localColor = (byte)Int32.Parse(words[offset++]);
        byte vtColorIndex = LdConstant.GetEffectiveColorIndex(localColor, parentColor);

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
        Matrix4x4 trMatrix, byte parentColor, bool accumInvert, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 14)
            return false;

        int offset = 1;

        byte localColor = (byte)Int32.Parse(words[offset++]);
        byte vtColorIndex = LdConstant.GetEffectiveColorIndex(localColor, parentColor);

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

    private bool ParseModel(string[] readText, ref BrickMesh brickMesh,
        Matrix4x4 trMatrix, byte parentColor = LdConstant.LD_COLOR_MAIN, bool accumInvert = false)
    {
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
                    ParseBFCInfo(line, ref winding, ref invertNext);
                    break;
                case 1:
                    if (!ParseSubFileInfo(line, ref brickMesh, trMatrix, parentColor, invertNext ^ accumInvert))
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

        return true;
    }

    private bool LoadModel(string fileName, ref BrickMesh brickMesh, 
        Matrix4x4 trMatrix, byte parentColor = LdConstant.LD_COLOR_MAIN, bool accInvertNext = false)
    {
        bool isStud = (fileName.IndexOf("stud", StringComparison.CurrentCultureIgnoreCase) != -1);

        for (int i = 0; i < PARTS_REL_PATH.Length; ++i)
        {
            var subDirName = Path.GetDirectoryName(fileName);
            var path = Path.Combine(Application.dataPath, PARTS_REL_PATH[i]);
            var filePath = Path.Combine(path, fileName);

            if (File.Exists(filePath))
            {
                if (brickCache.ContainsKey(fileName))
                {
                    BrickMesh subBrickMesh = new BrickMesh(brickCache[fileName]);

                    if (i == 0 && subDirName.Length == 0)
                        brickMesh.AddChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh);
                    else
                        brickMesh.MergeChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh, isStud);

                    return true;
                }
                else
                {
                    string[] readText = File.ReadAllLines(filePath);

                    BrickMesh subBrickMesh = new BrickMesh(fileName);
                    if (ParseModel(readText, ref subBrickMesh, Matrix4x4.identity))
                    {
                        brickCache[fileName] = new BrickMesh(subBrickMesh);

                        if (i == 0 && subDirName.Length == 0)
                            brickMesh.AddChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh);
                        else
                            brickMesh.MergeChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh, isStud);

                        return true;
                    }
                }
            }
        }

        if (ldrCache.ContainsKey(fileName))
        {
            BrickMesh subBrickMesh = new BrickMesh(fileName);
            if (ParseModel(ldrCache[fileName].ToArray(), ref subBrickMesh, Matrix4x4.identity))
            {
                brickCache[fileName] = new BrickMesh(subBrickMesh);
                brickMesh.AddChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh);
                return true;
            }
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

    public bool Load(string fileName, ref BrickMesh brickMesh)
    {
        string ext = Path.GetExtension(fileName);

        certifed = eCertified.NA;
        Matrix4x4 transform = Matrix4x4.identity;

        if (ext.Equals(LdConstant.TAG_MPD_FILE_EXT, StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(Application.dataPath, MODEL_REL_PATH);
            var filePath = Path.Combine(path, fileName);

            if (!File.Exists(filePath))
            {
                Debug.Log(string.Format("File does not exists: {0}", filePath));
                return false;
            }

            string[] readText = File.ReadAllLines(filePath);
            string mainModelName = SplitModel(readText);

            if (mainModelName.Length == 0 || !ldrCache.ContainsKey(mainModelName))
            {
                Debug.Log(string.Format("Cannot find main model: {0}", mainModelName));
                return false;
            }

            return ParseModel(ldrCache[mainModelName].ToArray(), ref brickMesh, transform);
        }
        else
        {
            return LoadModel(fileName, ref brickMesh, transform);
        }
    }
}
