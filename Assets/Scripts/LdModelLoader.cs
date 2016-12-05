using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

public class LdModelLoader
{
    private const int VERTEX_CNT_LIMIT_PER_MESH = 60000;

    private enum eCertified { NA = 0, TRUE, FALSE };
    private enum eWinding   { CCW = 0, CW };

    private readonly string[] PARTS_REL_PATH =
    {
        Path.Combine(Path.Combine("..", "LdParts"), "p"),
        Path.Combine(Path.Combine("..", "LdParts"), "parts"),
    };
    private readonly string MODEL_REL_PATH = Path.Combine("..", "LdModels");

    private LdColorTable colorTable;
    private Dictionary<string, List<string>> ldrCache;
    private eCertified certifed = eCertified.NA;

    public LdModelLoader(LdColorTable palette)
    {
        colorTable = palette;
        ldrCache = new Dictionary<string, List<string>>();
    }

    private void Reset()
    {
        ldrCache.Clear();
        certifed = eCertified.NA;
    }

    private Color32 GetColor32(int localColor, int parentColor)
    {
        int colorIndex = (parentColor == LdConstant.LD_COLOR_MAIN) ? localColor : parentColor;

        return colorTable.GetColor(colorIndex);
    }

    private Vector3 ParseVector(string[] words, ref int offset)
    {
        float x = float.Parse(words[offset++]);
        float y = float.Parse(words[offset++]);
        float z = float.Parse(words[offset++]);

        return new Vector3(x, y, z);
    }

    // Ldraw matrix
    // a b c d e f g h iis a top left 3x3 matrix of a standard 4x4 homogeneous transformation matrix.
    // This represents the rotation and scaling of the part.
    // The entire 4x4 3D transformation matrix would then take either of the following forms :
    // post-multiplication: [X*] = [X][R]
    // / a d g 0 \
    // | b e h 0 |
    // | c f i 0 |
    // \ x y z 1 /
    // pre-multiplication: [X*] = inv[R] [X]
    // / a b c x \
    // | d e f y |
    // | g h i z |
    // \ 0 0 0 1 /
    private Matrix4x4 ParseMatrix(string[] words, ref int offset)
    {
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

        Vector4 right = new Vector4(a, d, g, 0);
        Vector4 up = new Vector4(b, e, h, 0);
        Vector4 forward = new Vector4(c, f, i, 0);
        Vector4 pos = new Vector4(x, y, z, 1);

        Matrix4x4 matrix = new Matrix4x4();
        matrix.SetColumn(0, right);
        matrix.SetColumn(1, up);
        matrix.SetColumn(2, forward);
        matrix.SetColumn(3, pos);

        return matrix;
    }

    private bool ParseBFCInfo(string line, bool accumInvert, ref eWinding winding, ref bool invertNext)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
            return true;

        int offset = 1;

        if (!words[offset++].Equals(LdConstant.TAG_BFC, StringComparison.OrdinalIgnoreCase))
            return true;

        if (words[offset].Equals(LdConstant.TAG_CERTIFY, StringComparison.OrdinalIgnoreCase))
        {
            offset++;

            if (certifed == eCertified.FALSE)
                return false;

            if (certifed == eCertified.NA)
                certifed = eCertified.TRUE;

        }
        else if (words[offset].Equals(LdConstant.TAG_NOCERTIFY, StringComparison.OrdinalIgnoreCase))
        {
            offset++;

            if (certifed == eCertified.TRUE)
                return false;

            if (certifed == eCertified.NA)
                certifed = eCertified.FALSE;
        }

        if (certifed == eCertified.FALSE)
            return true;

        if (offset >= words.Length)
        {
            winding = accumInvert ? eWinding.CW : eWinding.CCW;
            return true;
        }
        else if (words[offset].Equals(LdConstant.TAG_CW, StringComparison.OrdinalIgnoreCase))
        {
            offset++;
            winding = accumInvert ? eWinding.CCW : eWinding.CW;
            return true;

        }
        else if (words[offset].Equals(LdConstant.TAG_CCW, StringComparison.OrdinalIgnoreCase))
        {
            offset++;
            winding = accumInvert ? eWinding.CW : eWinding.CCW;
            return true;
        }

        if (words[offset].Equals(LdConstant.TAG_INVERTNEXT, StringComparison.OrdinalIgnoreCase))
        {
            offset++;
            invertNext = true;
        }

        return true;
    }

    private bool ParseSubFileInfo(string line, Matrix4x4 trMatrix,
        ref BrickMesh brickMesh, int parentColor, bool accInvertNext)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 15)
            return false;

        int offset = 1;

        int localColor = Int32.Parse(words[offset++]);
        int color = (parentColor == LdConstant.LD_COLOR_MAIN) ? localColor : parentColor;

        Matrix4x4 mLocal = ParseMatrix(words, ref offset);
        string fname = words[offset];
        for (int i = offset + 1; i < words.Length; ++i)
            fname += words[i];

        if (mLocal.determinant < 0)
        {
            accInvertNext = !accInvertNext;
        }

        Matrix4x4 mAcc = trMatrix * mLocal;
        return LoadModel(fname, mAcc, ref brickMesh, color, accInvertNext);
    }

    private bool ParseTriInfo(string line, Matrix4x4 trMatrix,
        ref BrickMesh brickMesh, int parentColor, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 11)
            return false;

        int offset = 1;

        Color32 vtColor = GetColor32(Int32.Parse(words[offset++]), parentColor);

        Vector3 v1 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v2 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v3 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));

        // Unity is LHS
        brickMesh.vertices.Add(new Vector3(v1.x, -v1.y, v1.z));
        brickMesh.vertices.Add(new Vector3(v2.x, -v2.y, v2.z));
        brickMesh.vertices.Add(new Vector3(v3.x, -v3.y, v3.z));

        // winding is for RHS so apply reverse for Unity
        int lastIndex = brickMesh.triangles.Count;
        if (winding == eWinding.CW)
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
            brickMesh.colors.Add(vtColor);

        return true;
    }

    private bool ParseQuadInfo(string line, Matrix4x4 trMatrix,
        ref BrickMesh brickMesh, int parentColor, eWinding winding)
    {
        string[] words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 14)
            return false;

        int offset = 1;

        Color32 vtColor = GetColor32(Int32.Parse(words[offset++]), parentColor);

        Vector3 v1 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v2 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v3 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));
        Vector3 v4 = trMatrix.MultiplyPoint3x4(ParseVector(words, ref offset));

        brickMesh.vertices.Add(new Vector3(v1.x, -v1.y, v1.z));
        brickMesh.vertices.Add(new Vector3(v2.x, -v2.y, v2.z));
        brickMesh.vertices.Add(new Vector3(v3.x, -v3.y, v3.z));

        brickMesh.vertices.Add(new Vector3(v3.x, -v3.y, v3.z));
        brickMesh.vertices.Add(new Vector3(v4.x, -v4.y, v4.z));
        brickMesh.vertices.Add(new Vector3(v1.x, -v1.y, v1.z));

        int lastIndex = brickMesh.triangles.Count;
        if (winding == eWinding.CW)
        {
            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 1);
            brickMesh.triangles.Add(lastIndex + 2);

            brickMesh.triangles.Add(lastIndex + 3);
            brickMesh.triangles.Add(lastIndex + 4);
            brickMesh.triangles.Add(lastIndex + 5);
        }
        else
        {
            brickMesh.triangles.Add(lastIndex + 0);
            brickMesh.triangles.Add(lastIndex + 2);
            brickMesh.triangles.Add(lastIndex + 1);

            brickMesh.triangles.Add(lastIndex + 3);
            brickMesh.triangles.Add(lastIndex + 5);
            brickMesh.triangles.Add(lastIndex + 4);
        }

        for (int i = 0; i < 6; ++i)
            brickMesh.colors.Add(vtColor);

        return true;
    }

    private bool ParseModel(string[] readText, Matrix4x4 trMatrix,
        ref BrickMesh brickMesh, int parentColor, bool accumInvert)
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
                    if (!ParseBFCInfo(line, accumInvert, ref winding, ref invertNext))
                    {
                        Console.WriteLine("ParseBFCInfo failed: {0}", line);
                        return false;
                    }
                    break;
                case 1:
                    bool passInvertNext = invertNext ? !accumInvert : accumInvert;
                    if (!ParseSubFileInfo(line, trMatrix, ref brickMesh, parentColor, passInvertNext))
                    {
                        Console.WriteLine("ParseSubFileInfo failed: {0}", line);
                        return false;
                    }
                    invertNext = false;
                    break;
                case 3:
                    if (!ParseTriInfo(line, trMatrix, ref brickMesh, parentColor, winding))
                    {
                        Console.WriteLine("ParseTriInfo failed: {0}", line);
                        return false;
                    }
                    break;
                case 4:
                    if (!ParseQuadInfo(line, trMatrix, ref brickMesh, parentColor, winding))
                    {
                        Console.WriteLine("ParseQuadInfo failed: {0}", line);
                        return false;
                    }
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    private bool LoadModel(string fileName, Matrix4x4 trMatrix,
        ref BrickMesh brickMesh, int parentColor, bool accInvertNext)
    {
        foreach (string element in PARTS_REL_PATH)
        {
            var path = Path.Combine(Application.dataPath, element);
            var filePath = Path.Combine(path, fileName);

            if (File.Exists(filePath))
            {
                string[] readText = File.ReadAllLines(filePath);
                return ParseModel(readText, trMatrix, ref brickMesh, parentColor, accInvertNext);
            }
        }

        if (!ldrCache.ContainsKey(fileName))
        {
            Console.WriteLine("mpd file does not contains: {0}", fileName);
            return false;
        }

        BrickMesh subBrickMesh = new BrickMesh(fileName);

        if (!ParseModel(ldrCache[fileName].ToArray(), trMatrix, ref subBrickMesh, parentColor, accInvertNext))
            return false;

        brickMesh.children.Add(subBrickMesh);
        return true;
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
        Reset();

        string ext = Path.GetExtension(fileName);

        if (ext.Equals(LdConstant.TAG_MPD_FILE_EXT, StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(Application.dataPath, MODEL_REL_PATH);
            var filePath = Path.Combine(path, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exists: {0}", filePath);
                return false;
            }

            string[] readText = File.ReadAllLines(filePath);
            string mainModelName = SplitModel(readText);

            if (mainModelName.Length == 0 || !ldrCache.ContainsKey(mainModelName))
            {
                Console.WriteLine("Cannot find main model: {0}", mainModelName);
                return false;
            }

            return ParseModel(ldrCache[mainModelName].ToArray(), Matrix4x4.identity, ref brickMesh, LdConstant.LD_COLOR_MAIN, false);
        }
        else
        {
            return LoadModel(fileName, Matrix4x4.identity, ref brickMesh, LdConstant.LD_COLOR_MAIN, false);
        }
    }
}
