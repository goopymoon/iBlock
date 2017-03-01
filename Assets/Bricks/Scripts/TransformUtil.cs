using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TransformData
{
    public Vector3 position;
    public Quaternion rotation;

    public Vector3 localPosition;
    public Vector3 localScale;
    public Quaternion localRotation;

    public Transform parent;
}

public static class TransformUtil
{
    public static TransformData Clone(this Transform transform)
    {
        TransformData td = new TransformData();

        td.position = transform.position;
        td.localPosition = transform.localPosition;

        td.rotation = transform.rotation;
        td.localRotation = transform.localRotation;

        td.localScale = transform.localScale;

        td.parent = transform.parent;

        return td;
    }

    public static void Restore(this Transform transform, TransformData td)
    {
        transform.position = td.position;
        transform.localPosition = td.localPosition;

        transform.rotation = td.rotation;
        transform.localRotation = td.localRotation;

        transform.localScale = td.localScale;

        transform.parent = td.parent;
    }
}
