using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class LdColorTable : MonoBehaviour
{
    private LdColorLoader colorLoader;
    private Dictionary<int, Color32> palette;

    public Color32 GetColor(int colorIndex)
    {
        if (palette.ContainsKey(colorIndex))
            return palette[colorIndex];
        else
            return colorLoader.DefBrickColor;
    }

    void Awake()
    {
        colorLoader = new LdColorLoader();
        palette = new Dictionary<int, Color32>();

        string fileName = colorLoader.COLOR_CFG_FNAME;
        if (!colorLoader.Load(fileName, ref palette))
        {
            Console.WriteLine("Cannot parse: {0}", fileName);
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
