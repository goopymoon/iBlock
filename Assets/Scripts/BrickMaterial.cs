using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickMaterial : MonoBehaviour
{
    public enum BrickMaterialType {
        Opaque,
        Transparent,
        BEGIN = Opaque,
        END = Transparent,
        DEFAUTL = BEGIN,
    };

    private Dictionary<BrickMaterialType, string> _materialPath = new Dictionary<BrickMaterialType, string>();
    private Dictionary<BrickMaterialType, Material> _customeMaterial = new Dictionary<BrickMaterialType, Material>();

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
                    container.name = "LdColorTableContainer";
                    _instance = container.AddComponent(typeof(BrickMaterial)) as BrickMaterial;
                }
            }

            return _instance;
        }
    }

    public void Initialize()
    {
        _materialPath.Clear();
        _customeMaterial.Clear();

        _materialPath.Add(BrickMaterialType.Opaque, "Materials/OpaqueBrick");
        _materialPath.Add(BrickMaterialType.Transparent, "Materials/TransparentBrick");

        for (var i = BrickMaterialType.BEGIN; i <= BrickMaterialType.END; ++i)
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
    }

    public Material GetMaterial(BrickMaterialType matType)
    {
        Material temp;
        if (_customeMaterial.TryGetValue(matType, out temp))
            return temp;
        else
            return _customeMaterial[BrickMaterialType.DEFAUTL];
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
