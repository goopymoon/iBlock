﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjId {

    public uint Val { get; set; }
    public bool IsValid() { return Val != 0; }

    public GameObjId()
    {
        Val = 0;
    }

    public GameObjId(uint value)
    {
        Val = value;
    }

    public GameObjId(GameObjId rhs)
    {
        Val = rhs.Val;
    }
}
