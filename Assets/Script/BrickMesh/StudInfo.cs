using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct StudInfo
{
    public Matrix4x4 Tr { get; private set; }
    public short ColorIndex { get; private set; }
    public bool Inverted { get; private set; }

    public StudInfo(Matrix4x4 tr, short colorIndex, bool inverted)
    {
        Tr = tr;
        ColorIndex = colorIndex;
        Inverted = inverted;
    }
}
