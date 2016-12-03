using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

public class LdColorTable : MonoBehaviour
{
    private LdColorLoader _colorLoader;
    private Dictionary<int, Color32> _palette;

    public Color32 GetColor(int colorIndex)
    {
        if (_palette.ContainsKey(colorIndex))
            return _palette[colorIndex];
        else
            return _colorLoader.DefBrickColor;
    }

    void Awake()
    {
        _colorLoader = new LdColorLoader();
        _palette = new Dictionary<int, Color32>();

        string fileName = _colorLoader.COLOR_CFG_FNAME;
        if (!_colorLoader.Load(fileName, ref _palette))
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
