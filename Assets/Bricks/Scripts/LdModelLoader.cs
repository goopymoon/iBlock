using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

public class LdModelLoader : MonoBehaviour
{
	public bool isInitialized { get; set; }
	public bool isModelReady { get; set; }
	public BrickMesh model;

    private enum eCertified { NA = 0, TRUE, FALSE };
    private enum eWinding   { CCW = 0, CW };

    class FileLines
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

    private Dictionary<string, BrickMesh> brickCache;
    private Dictionary<string, string> pathCache;
    private Dictionary<string, FileLines> fileCache;

    // member variables used by file loading coroutine
    private bool FileCacheResult = true;
    private bool fileLoadResult = true;
	private string readString;

	private string mainModelName;
	private List<string> subFileNames;

	private static LdModelLoader _instance;
	public static LdModelLoader Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = GameObject.FindObjectOfType(typeof(LdModelLoader)) as LdModelLoader;
				if (_instance == null)
				{
					GameObject container = new GameObject();
					container.name = "LdModelLoader";
					_instance = container.AddComponent(typeof(LdModelLoader)) as LdModelLoader;
				}
			}

			return _instance;
		}
	}
		
	public LdModelLoader()
	{
		pathCache = new Dictionary<string, string>();
		fileCache = new Dictionary<string, FileLines>();
		brickCache = new Dictionary<string, BrickMesh>();
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

        //Debug.Log(string.Format("{0}: loaded string length {1}", filePath, readString.Length));
    }
		
	IEnumerator LoadPartsPathFile()
	{ 
		var filePath = Path.Combine(Application.streamingAssetsPath, "partspath.lst");

		yield return StartCoroutine ("LoadFile", filePath);
        if (!fileLoadResult)
            yield break;
        while (readString.Length == 0)
            yield return null;

        string[] readText = readString.Split(
            Environment.NewLine.ToCharArray(),
            StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < readText.Length; ++i)
        {
            string[] words = readText[i].Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 2)
                pathCache.Add(words[0], words[1]);
        }

        isInitialized = true;

        Debug.Log(string.Format("Path cache of parts is ready.", filePath));
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
        return ParseModel(fname, ref brickMesh, mAcc, colorIndex, accumInvert, accumInvertByMatrix);
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

    private bool IsNeedMerge(string fileName, string filePath)
    {
        string ext = Path.GetExtension(fileName).ToLower();

        if (ext == ".dat")
        {
            if (filePath.Length > 0)
            {
                string dirName = Path.GetDirectoryName(filePath);
                return (dirName != "parts");
            }
            else
            {
                return (Path.GetDirectoryName(fileName).Length > 0);
            }
        }

        return false;
    }

    private bool ParseModel(string fileName, ref BrickMesh parentMesh, 
        Matrix4x4 trMatrix, short parentColor = LdConstant.LD_COLOR_MAIN, bool accInvertNext = false, bool accInvertByMatrix = false)
    {
        string cacheFileName = fileName.Replace(@"\", @"/").ToLower();
        BrickMesh subBrickMesh = null;

        FileLines val;
        if (!fileCache.TryGetValue(cacheFileName, out val))
        {
            Debug.Log(string.Format("Cannot find file cache for {0}", cacheFileName));
            return false;
        }

        if (brickCache.ContainsKey(cacheFileName))
        {
            subBrickMesh = new BrickMesh(brickCache[cacheFileName]);
        }
        else
        {
            string[] readText = val.cache.ToArray();

            subBrickMesh = ParseModel(fileName, readText, Matrix4x4.identity);
            if (subBrickMesh == null)
                return false;

            subBrickMesh.Optimize();
            brickCache[cacheFileName] = new BrickMesh(subBrickMesh);
        }

        if (IsNeedMerge(fileName, val.filePath))
            parentMesh.MergeChildBrick(accInvertNext, accInvertByMatrix, parentColor, trMatrix, subBrickMesh);
        else
            parentMesh.AddChildBrick(accInvertNext, parentColor, trMatrix, subBrickMesh);

        return true;
    }

    private bool ExtractSubFileNames(string[] readText, ref List<string> subFileNames)
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
                    return false;
                }

                string fname = words[14];
                for (int j = 15; j < words.Length; ++j)
                {
                    fname += words[j];
                }

                subFileNames.Add(fname.ToLower());
            }
        }

        return true;
    }

    IEnumerator LoadCacheFiles(string fileName)
    {
        string cacheFileName = fileName.Replace(@"\", @"/").ToLower();

        FileLines fileLines;
        List<string> localSubFileNames = new List<string>();

        if (fileCache.TryGetValue(cacheFileName, out fileLines))
        {
            if (fileLines.loadCompleted)
            {
                FileCacheResult = true;
                yield break;
            }

            if (!ExtractSubFileNames(fileLines.cache.ToArray(), ref localSubFileNames))
            {
                Debug.Log(string.Format("Extracting sub file failed: {0}", cacheFileName));
                FileCacheResult = false;
                yield break;
            }

            fileCache[cacheFileName].loadCompleted = true;
        }
        else
        {
            string val;
            if (!pathCache.TryGetValue(cacheFileName, out val))
            {
                FileCacheResult = false;
                Debug.Log(string.Format("Parts list has no {0}", cacheFileName));
				yield break;
            }

			var ldPartsPath = Path.Combine(Application.streamingAssetsPath, "LdParts");
            var filePath = Path.Combine(ldPartsPath, val);

			yield return StartCoroutine ("LoadFile", filePath);
            if (!fileLoadResult)
            {
                FileCacheResult = false;
                yield break;
            }
            while (readString.Length == 0)
                yield return null;

            string[] readText = readString.Split(
				Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (!ExtractSubFileNames(readText, ref localSubFileNames))
            {
                Debug.Log(string.Format("Extracting sub file failed: {0}", cacheFileName));
                FileCacheResult = false;
                yield break;
            }

            fileCache.Add(cacheFileName, new FileLines(val, readText));
        }

        if (localSubFileNames.Count != 0)
            subFileNames.AddRange(localSubFileNames);

        FileCacheResult = true;
    }

    private bool ExtractModelName(string line, ref string modelName)
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

        modelName.Replace(@"\", @"/").Trim();

        return true;
    }

    private string LoadLDRFiles(string[] readText)
    {
		string mainModelName = string.Empty;
		string modelName = string.Empty;
        string cacheModelName = string.Empty;

        fileCache.Clear();

        for (int i = 0; i < readText.Length; ++i)
        {
            if (ExtractModelName(readText[i], ref modelName))
            {
                cacheModelName = modelName.Replace(@"\", @"/").ToLower();

                if (mainModelName.Length == 0)
                    mainModelName = modelName;

                fileCache.Add(cacheModelName, new FileLines());
                //Debug.Log(string.Format("Add ldr model into file cache: {0}", cacheModelName));
            }

            if (modelName != null)
                fileCache[cacheModelName].cache.Add(readText[i]);
        }

        //Debug.Log(string.Format("File cache size after adding ldr files: {0}", fileCache.Count.ToString()));

        return mainModelName;
    }

	IEnumerator LoadMPDFile(string fileName)
    {
		mainModelName = string.Empty;

        string ext = Path.GetExtension(fileName);
		if (ext.Equals (LdConstant.TAG_MPD_FILE_EXT, StringComparison.OrdinalIgnoreCase)) 
		{
			var ModelPath = Path.Combine (Application.streamingAssetsPath, "LdModels");
			var filePath = Path.Combine (ModelPath, fileName);

			yield return StartCoroutine ("LoadFile", filePath);
            if (!fileLoadResult)
                yield break;
            while (readString.Length == 0)
                yield return null;

            string[] readText = readString.Split (
				Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries);

			mainModelName = LoadLDRFiles (readText);
			if (mainModelName != string.Empty) 
			{
				subFileNames = new List<string> ();
				subFileNames.Add (mainModelName);

				while (subFileNames.Count > 0) 
				{
					string fname = subFileNames [0];
					subFileNames.RemoveAt (0);

					yield return StartCoroutine ("LoadCacheFiles", fname);
                    if (!FileCacheResult)
                    {
                        Debug.Log(string.Format("File caching failed: {0}", fname));
                        yield break;
                    }
				}

                Debug.Log(string.Format("File cache is ready: {0}", fileCache.Count.ToString()));
			}
        }
    }

    IEnumerator LoadMainModel(string fileName)
    {
        yield return StartCoroutine ("LoadMPDFile", fileName);

        if (FileCacheResult) {
            model = new BrickMesh (fileName);
            if (!ParseModel(mainModelName, ref model, Matrix4x4.identity))
                yield break;

            isModelReady = true;
        }
    }

    public void Initialize()
    {
        isInitialized = false;
        isModelReady = false;

        StartCoroutine("LoadPartsPathFile");
    }

    public void Load(string fileName)
    {
        StartCoroutine ("LoadMainModel", fileName);
    }
}
