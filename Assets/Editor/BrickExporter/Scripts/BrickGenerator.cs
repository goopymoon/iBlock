#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System;
using System.IO;

public class BrickGeneratorScript : EditorWindow
{
    static System.Diagnostics.Stopwatch stopWatch;

    private GameObject CreateBrickPrefab(BrickMesh brickMesh, Vector3 scale)
    {
        UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Brick.prefab", typeof(GameObject));
        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
        go.name = brickMesh.Name;

        go.GetComponent<Brick>().CreateMesh(brickMesh, false, LdConstant.LD_COLOR_MAIN);

        for (int i = 0; i < brickMesh.Children.Count; ++i)
        {
            CreateChild(brickMesh.Children[i], go.transform, false, LdConstant.LD_COLOR_MAIN);
        }

        go.transform.localScale = scale;
        go.AddComponent<Rigidbody>();

        return go;
    }

    private void CreateChild(BrickMesh brickMesh, Transform parent, bool invertNext, short parentBrickColor)
    {
        UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Brick.prefab", typeof(GameObject));
        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;

        go.GetComponent<Brick>().CreateMesh(brickMesh, invertNext, parentBrickColor);

        go.name = brickMesh.Name;
        go.GetComponent<Brick>().SetParent(parent);

        bool accInvert = invertNext ^ brickMesh.InvertNext;
        short accuColor = LdConstant.GetEffectiveColorIndex(brickMesh.BrickColor, parentBrickColor);

        for (int i = 0; i < brickMesh.Children.Count; ++i)
        {
            CreateChild(brickMesh.Children[i], go.transform, accInvert, accuColor);
        }
    }

    private GameObject GetTerrain()
    {
        GameObject go = GameObject.Find("Ground");
        return go;
    }

    private void SnapToTerrain(GameObject go)
    {
        Bounds aabb = go.GetComponent<Brick>().AABB;
        var collider = GetTerrain().GetComponent<Collider>();

        if (collider)
        {
            RaycastHit hit;
            Ray ray = new Ray(aabb.center, -go.transform.up);
            if (collider.Raycast(ray, out hit, Mathf.Infinity))
            {
                float rayDistance = aabb.center.y - hit.point.y;
                Vector3 shift = go.transform.up * (aabb.extents.y - rayDistance);
                go.transform.Translate(shift);
            }
        }
    }

    public bool LoadModel(Vector3 scale)
    {
        LdModelLoader loader = ScriptableObject.CreateInstance<LdModelLoader>();

        if (!loader.Initialize())
            return false;

        BrickMesh brickMesh = loader.Load(true);
        if (brickMesh != null)
        {
            stopWatch.Start();
            GameObject go = CreateBrickPrefab(brickMesh, scale);
            stopWatch.Stop();
            Debug.Log("CreateMesh: " + stopWatch.ElapsedMilliseconds + " ms");

            SnapToTerrain(go);

            BrickMeshManager.Instance.DumpBrickMesh();
            return true;
        }

        return false;
    }

    public void Initialize()
    {
        stopWatch = new System.Diagnostics.Stopwatch();
    }
}

public class BrickGenerator : ScriptableObject
{
    public static bool IsInitialized { get; private set; }
    public static Vector3 scale;

    static BrickGenerator()
    {
        BrickGenerator.IsInitialized = false;
        BrickGenerator.scale = new Vector3(0.1f, 0.1f, 0.1f);
    }

    static void DoImport()
    {
        if (!IsInitialized)
        {
            if (!LdColorTable.Instance.Initialize())
                return;

            BrickMaterial.Instance.Initialize();
            BrickMeshManager.Instance.Initialize();

            IsInitialized = true;
        }

        BrickGeneratorScript import = ScriptableObject.CreateInstance<BrickGeneratorScript>();
        import.Initialize();

        import.LoadModel(scale);
    }

    [MenuItem("Ldraw/Import model")]
    static void DoExportSingle()
    {
        DoImport();
    }
}

#endif
