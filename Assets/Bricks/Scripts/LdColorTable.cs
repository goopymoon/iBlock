using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class LdColorTable : MonoBehaviour
{
    private readonly Color32 DEF_BRICK_COLOR;
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

    public LdColorTable()
    {
        DEF_BRICK_COLOR = new Color32();

        DEF_BRICK_COLOR.r = 0x7F;
        DEF_BRICK_COLOR.g = 0x7F;
        DEF_BRICK_COLOR.b = 0x7F;
        DEF_BRICK_COLOR.a = 0xFF;
    }

    public Color32 GetColor(int colorIndex)
    {
        if (_palette.ContainsKey(colorIndex))
            return _palette[colorIndex];
        else
            return DEF_BRICK_COLOR;
    }

    public void Initialize()
    {
        LdColorLoader colorLoader = new LdColorLoader();
        _palette = new Dictionary<int, Color32>();

        string fileName = colorLoader.COLOR_CFG_FNAME;
        if (!colorLoader.Load(fileName, ref _palette))
        {
            Debug.Log(string.Format("Cannot parse: {0}", fileName));
        }
    }
}
