using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Linq;

public static class BrickMeshOptimizer
{
    public const int SMOOTH_ANGLE_THRESHOLD_FOR_OPTIMIZE = 60;

    public static void Optimize(this BrickMesh mesh, float angle)
    {
        if (mesh.vertices.Count == 0)
            return;

        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var colors = mesh.colorIndices;
        var triNormals = new Vector3[triangles.Count / 3]; //Holds the normal of each triangle

        //Debug.Log(string.Format("Start optimize {0}: vtCnt:{1}, triCnt:{2}, colorCnt:{3}", 
        //    mesh.name, vertices.Count, triangles.Count, colors.Count));

        angle = angle * Mathf.Deg2Rad;

        var dictionary = new Dictionary<VertexKey, VertexEntry>(vertices.Count);

        // Set vertex index dictionary
        var vtIndices = new Dictionary<int, VertexIndexEnry>(vertices.Count);
        for (int i = 0; i < vertices.Count; ++i)
            vtIndices.Add(i, new VertexIndexEnry(i));

        // Goes through all the triangles and gathers up data to be used later
        for (var i = 0; i < triangles.Count; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            int color1 = colors[i1];
            int color2 = colors[i2];
            int color3 = colors[i3];

            //Calculate the normal of the triangle
            Vector3 p1 = vertices[i2] - vertices[i1];
            Vector3 p2 = vertices[i3] - vertices[i1];
            Vector3 normal = Vector3.Cross(p1, p2).normalized;
            int triIndex = i / 3;
            triNormals[triIndex] = normal;

            VertexEntry entry;
            VertexKey key;

            //For each of the three points of the triangle
            //  > Add this triangle as part of the triangles they're connected to.

            if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out entry))
            {
                entry = new VertexEntry();
                dictionary.Add(key, entry);
            }
            entry.Add(i1, triIndex, color1);

            if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
            {
                entry = new VertexEntry();
                dictionary.Add(key, entry);
            }
            entry.Add(i2, triIndex, color2);

            if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
            {
                entry = new VertexEntry();
                dictionary.Add(key, entry);
            }
            entry.Add(i3, triIndex, color3);
        }

        bool isShrinkNeeded = false;

        // Shrink vertcies index dictionary
        foreach (var value in dictionary.Values)
        {
            for (var i = 0; i < value.Count; ++i)
            {
                for (var j = i + 1; j < value.Count; ++j)
                {
                    if (value.vertexIndex[i] == value.vertexIndex[j])
                        continue;

                    if (value.colorIndex[i] != value.colorIndex[j])
                        continue;

                    float dot = Vector3.Dot(
                        triNormals[value.triangleIndex[i]],
                        triNormals[value.triangleIndex[j]]);
                    dot = Mathf.Clamp(dot, -0.99999f, 0.99999f);
                    float acos = Mathf.Acos(dot);

                    if (acos <= angle)
                    {
                        var srcIndex = value.vertexIndex[j];
                        var targetIndex = value.vertexIndex[i];

                        //Debug.Log(string.Format("Mark replace: {0} to {1}", srcIndex, targetIndex));

                        vtIndices[srcIndex].replaceFlag = true;
                        vtIndices[srcIndex].replaceIndex = targetIndex;

                        isShrinkNeeded = true;
                    }
                }
            }
        }

        if (isShrinkNeeded)
        {
            List<Vector3> shrinkedVertices = new List<Vector3>();
            List<short> shrinkedColors = new List<short>();

            var vtKeys = vtIndices.Keys.ToList();

            int serialIndex = 0;
            foreach (var key in vtKeys)
            {
                if (vtIndices[key].replaceFlag)
                {
                    int firstReplaceIndex = vtIndices[key].replaceIndex;
                    int finalReplaceIndex = firstReplaceIndex;
                    while (vtIndices[finalReplaceIndex].replaceFlag)
                    {
                        finalReplaceIndex = vtIndices[finalReplaceIndex].replaceIndex;
                        if (finalReplaceIndex == firstReplaceIndex)
                        {
                            //Debug.Log(string.Format("Cancle Replace: {0} with {1}", vtIndices[key].replaceIndex, firstReplaceIndex));
                            vtIndices[key].replaceFlag = false;
                            break;
                        }
                    }

                    if (vtIndices[key].replaceFlag)
                    {
                        //Debug.Log(string.Format("Replace: {0} to {1}", vtIndices[key].replaceIndex, finalReplaceIndex));
                        vtIndices[key].replaceIndex = finalReplaceIndex;
                        continue;
                    }
                }

                shrinkedVertices.Add(vertices[key]);
                shrinkedColors.Add(colors[key]);

                vtIndices[key].validPos = serialIndex++;
            }

            for (var i = 0; i < triangles.Count; i++)
            {
                var oriIndex = triangles[i];
                var resultIndex = vtIndices[oriIndex].replaceFlag ?
                    vtIndices[vtIndices[oriIndex].replaceIndex].validPos : vtIndices[oriIndex].validPos;
                triangles[i] = resultIndex;
            }

            //Debug.Log(string.Format("Reduced vertices of {0} : {1} to {2}", mesh.name, vertices.Count, shrinkedVertices.Count));

            mesh.vertices = shrinkedVertices;
            mesh.colorIndices = shrinkedColors;
        }
    }

    private class VertexIndexEnry
    {
        public bool replaceFlag { get; set; }
        public int replaceIndex { get; set; }
        public int validPos { get; set; }

        public VertexIndexEnry(int vtIndex)
        {
            replaceFlag = false;
            replaceIndex = vtIndex;
            validPos = -1;
        }
    }

    private struct VertexKey
    {
        private readonly long _x;
        private readonly long _y;
        private readonly long _z;

        private const int POSITION_DIFF_TOLERANCE = 100000;

        public VertexKey(Vector3 position)
        {
            _x = (long)(Mathf.Round(position.x * POSITION_DIFF_TOLERANCE));
            _y = (long)(Mathf.Round(position.y * POSITION_DIFF_TOLERANCE));
            _z = (long)(Mathf.Round(position.z * POSITION_DIFF_TOLERANCE));
        }

        public override bool Equals(object obj)
        {
            var key = (VertexKey)obj;
            return _x == key._x && _y == key._y && _z == key._z;
        }

        public override int GetHashCode()
        {
            return (_x * 7 ^ _y * 13 ^ _z * 27).GetHashCode();
        }
    }

    private sealed class VertexEntry
    {
        public List<int> vertexIndex = new List<int>();
        public List<int> triangleIndex = new List<int>();
        public List<int> colorIndex = new List<int>();

        private int count;

        public int Count { get { return count; } }

        public void Add(int vtIndex, int triIndex, int color)
        {
            vertexIndex.Add(vtIndex);
            triangleIndex.Add(triIndex);
            colorIndex.Add(color);
            ++count;
        }
    }
}

