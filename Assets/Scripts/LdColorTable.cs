using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class LdColorTable : MonoBehaviour
{
    private LdColorLoader colorLoader;
    private Dictionary<int, Color32> palette;
    private readonly Color32 DEF_BRICK_COLOR;

    LdColorTable()
    {
        DEF_BRICK_COLOR = new Color32();

        DEF_BRICK_COLOR.r = 0x7F;
        DEF_BRICK_COLOR.g = 0x7F;
        DEF_BRICK_COLOR.b = 0x7F;
        DEF_BRICK_COLOR.a = 0xFF;
    }

    public Color32 GetColor(int colorIndex)
    {
        if (palette.ContainsKey(colorIndex))
            return palette[colorIndex];
        else
            return DEF_BRICK_COLOR;
    }

    void Awake()
    {
        colorLoader = new LdColorLoader();
        palette = new Dictionary<int, Color32>();

        string fileName = colorLoader.COLOR_CFG_FNAME;
        if (!colorLoader.Load(fileName, ref palette))
        {
            Debug.Log(string.Format("Cannot parse: {0}", fileName));
        }
    }

    // Use this for initialization
    void Start ()
    {
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
