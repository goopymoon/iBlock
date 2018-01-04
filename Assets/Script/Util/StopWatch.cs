using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopWatch {

    private float startTime;
    private string msg;

    public StopWatch()
    {
    }

    public StopWatch(string title)
    {
        StartTick(title);
    }

    public void StartTick(string title)
    {
        msg = title;
        startTime = Time.time;
    }

    public void EndTick()
    {
        Debug.Log(string.Format("{0}: Elapsed {1}", msg, (Time.time - startTime)));
    }
}
