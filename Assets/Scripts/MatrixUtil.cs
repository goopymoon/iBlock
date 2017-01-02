using UnityEngine;
using System.Collections;

public static class MatrixUtil
{
    public static Vector3 ExtractTranslationFromMatrix(Matrix4x4 matrix)
    {
        Vector3 translate = (Vector3)matrix.GetColumn(3);

        return translate;
    }

    public static Quaternion ExtractRotationFromMatrix(Matrix4x4 matrix)
    {
        Vector3 upwards = (Vector3)matrix.GetColumn(1);
        Vector3 forward = (Vector3)matrix.GetColumn(2);

        return Quaternion.LookRotation(forward, upwards);
    }

    static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
    {
        Vector3 right = (Vector3)matrix.GetColumn(0);
        Vector3 upwards = (Vector3)matrix.GetColumn(1);
        Vector3 forward = (Vector3)matrix.GetColumn(2);

        Vector3 scale = new Vector3(right.magnitude, upwards.magnitude, forward.magnitude);
        if (Vector3.Dot(Vector3.Cross(right, upwards).normalized, forward.normalized) < 0)
        {
            scale.x *= -1;
        }
        return scale;
    }

    public static void DecomposeMatrix(Matrix4x4 matrix, 
        out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
    {
        localPosition = ExtractTranslationFromMatrix(matrix);
        localRotation = ExtractRotationFromMatrix(matrix);
        localScale = ExtractScaleFromMatrix(matrix);
    }

    public static void SetTransformFromMatrix(ref Transform transform, Matrix4x4 matrix)
    {
        transform.localPosition = ExtractTranslationFromMatrix(matrix);
        transform.localRotation = ExtractRotationFromMatrix(matrix);
        transform.localScale = ExtractScaleFromMatrix(matrix);
    }
}

