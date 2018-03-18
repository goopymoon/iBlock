using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BrickObjectDictionary : MonoBehaviour
{
    public static BrickObjectDictionary singleton;
    public int minNodeSize = 1;
    public float loosenessVal = 1.1f;
    public bool drawBrickBounds = false;
    public bool drawStudBounds = false;
    public bool drawBrickObjs = false;
    public bool drawStudObjs = false;

    // It takes too much time for cafe model if reducedStud is enabled.
    // So disable it until optimzation is done.
    public bool reduceStud { get; private set; }

    private BoundsOctree<Brick> brickOctree;
    private BoundsOctree<Brick> studOctree;

    void Awake()
    {
        singleton = this;
        reduceStud = false;
    }

    void OnDestroy()
    {
        brickOctree = null;
        studOctree = null;
    }

    public void Init(float initialWorldSize, Vector3 initialWorldPos)
    {
        if (!reduceStud) return;

        brickOctree = new BoundsOctree<Brick>(initialWorldSize, initialWorldPos, minNodeSize, loosenessVal);
        studOctree = new BoundsOctree<Brick>(initialWorldSize, initialWorldPos, minNodeSize, loosenessVal);
    }

    public void AddBrick2Octree(Brick brickObj)
    {
        if (!reduceStud) return;

        brickOctree.Add(brickObj, brickObj.AABB);

        List<Brick> objs = new List<Brick>();
        studOctree.GetContainedBy(objs, brickObj.AABB);

        foreach (var element in objs)
        {
            StudObjManager.Instance.ReleaseStudMesh(element.gameObject);
            studOctree.Remove(element);
        }
    }

    public void AddStud2Octree(StudInfo studInfo, Brick brickObj)
    {
        if (!reduceStud) return;

        if (studInfo.studType == StudInfo.eStudType.ST_CONVEX)
        {
            if (brickOctree.IsContaining(brickObj.AABB))
            {
                StudObjManager.Instance.ReleaseStudMesh(brickObj.gameObject);
                return;
            }
        }
        else if (studInfo.studType == StudInfo.eStudType.ST_CONCAVE)
        {
            Brick parentBrick = brickObj.transform.parent.gameObject.GetComponent<Brick>();
            if (parentBrick.AABB.Contains(brickObj.AABB.min) && parentBrick.AABB.Contains(brickObj.AABB.max))
            {
                StudObjManager.Instance.ReleaseStudMesh(brickObj.gameObject);
                return;
            }
        }

        studOctree.Add(brickObj, brickObj.AABB);
    }

    private void OnDrawGizmos()
    {
        if (!reduceStud) return;

        if (drawBrickBounds)
        {
            brickOctree.DrawAllBounds();
        }

        if (drawStudBounds)
        {
            studOctree.DrawAllBounds();
        }

        if (drawBrickObjs)
        {
            brickOctree.DrawAllObjects();
        }

        if (drawStudObjs)
        {
            studOctree.DrawAllObjects();
        }
    }
}
