using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickMaterial : MonoBehaviour
{
    public enum MatType
    {
        Opaque,
        Transparent,
        OpaqueDS,
        TransparentDS,
        BEGIN = Opaque,
        END = TransparentDS,
        DEFAUTL = BEGIN,
        DS_OFFSET = OpaqueDS - Opaque,
    };

    private Dictionary<MatType, string> _materialPath = new Dictionary<MatType, string>();
    private Dictionary<MatType, Material> _customeMaterial = new Dictionary<MatType, Material>();

    private static BrickMaterial _instance;
    public static BrickMaterial Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType(typeof(BrickMaterial)) as BrickMaterial;
                if (_instance == null)
                {
                    GameObject container = new GameObject();
                    container.name = "BrickMaterial";
                    _instance = container.AddComponent(typeof(BrickMaterial)) as BrickMaterial;
                }
            }

            return _instance;
        }
    }

    public Material GetMaterial(MatType matType)
    {
        Material temp;
        if (_customeMaterial.TryGetValue(matType, out temp))
            return temp;
        else
            return _customeMaterial[MatType.DEFAUTL];
    }

    public void Initialize()
    {
        _materialPath.Clear();
        _customeMaterial.Clear();

        _materialPath.Add(MatType.Opaque, "Materials/Opaque");
        _materialPath.Add(MatType.OpaqueDS, "Materials/OpaqueDS");
        _materialPath.Add(MatType.Transparent, "Materials/Transparent");
        _materialPath.Add(MatType.TransparentDS, "Materials/TransparentDS");

        for (var i = MatType.BEGIN; i <= MatType.END; ++i)
        {
            Material temp = Resources.Load(_materialPath[i], typeof(Material)) as Material;
            if (temp != null)
            {
                _customeMaterial.Add(i, temp);
                Debug.Assert(temp, "Loaded material " + _materialPath[i]);
            }
            else
            {
                Debug.Assert(temp, "Cannot load material " + _materialPath[i]);
            }
        }

        Debug.Log(string.Format("Materials are ready."));
    }
}
