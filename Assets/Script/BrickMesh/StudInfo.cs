using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct StudInfo
{
    public enum eStudType
    {
        ST_NA,
        ST_CONVEX,
        ST_CONCAVE,
    };

    public string Name { get; private set; }
    public Matrix4x4 Tr { get; private set; }
    public bool Inverted { get; private set; }
    public short ColorIndex { get; private set; }
    public StudInfo.eStudType studType { get; private set; }

    public StudInfo(string name, Matrix4x4 tr, bool inverted, short colorIndex, eStudType sType)
    {
        Name = name;
        Tr = tr;
        Inverted = inverted;
        ColorIndex = colorIndex;
        studType = sType;
    }
}
