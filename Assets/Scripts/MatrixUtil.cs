using UnityEngine;
using System.Collections;

public static class MatrixUtil
{
    public static Vector3 ExtractTranslationFromMatrix(Matrix4x4 matrix)
    {
        Vector3 translate;
        translate.x = matrix.m03;
        translate.y = matrix.m13;
        translate.z = matrix.m23;

        return translate;
    }

    public static Quaternion ExtractRotationFromMatrix(Matrix4x4 matrix)
    {
        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        return Quaternion.LookRotation(forward, upwards);
    }

    static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
    {
        Vector3 scale = new Vector3(
            matrix.GetColumn(0).magnitude,
            matrix.GetColumn(1).magnitude,
            matrix.GetColumn(2).magnitude);

        if (Vector3.Cross(matrix.GetColumn(0), matrix.GetColumn(1)).normalized != (Vector3)matrix.GetColumn(2).normalized)
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

