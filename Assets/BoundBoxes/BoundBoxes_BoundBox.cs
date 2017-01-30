using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class BoundBoxes_BoundBox : MonoBehaviour
{	
	public bool colliderBased = false;
    public Color lineColor = new Color(0f, 1f, 0.4f, 0.74f);

    private bool drawFlag = false;
    private bool isInitialized = false;

    private Bounds bound;
	private Vector3[] corners;
	private Vector3[,] lines;
	private Quaternion quat;
	
	private Camera mcamera;	
	private BoundBoxes_drawLines cameralines;
	
	private Renderer[] renderers;
	private MeshFilter[] meshes;
	
	private Vector3 topFrontLeft;
	private Vector3 topFrontRight;
	private Vector3 topBackLeft;
	private Vector3 topBackRight;
	private Vector3 bottomFrontLeft;
	private Vector3 bottomFrontRight;
	private Vector3 bottomBackLeft;
	private Vector3 bottomBackRight;
	
    public bool PrepareBounds()
    {
        if (isInitialized)
            return true;

        if (!CalculateBounds())
            return false;

        SetPoints();
        SetLines();

        isInitialized = true;

        return true;
    }

    bool CalculateBounds()
    {
        quat = Quaternion.Euler(0f, 0f, 0f);
        if (colliderBased)
        {
            BoxCollider coll = GetComponent<BoxCollider>();
            if (!coll || coll.bounds.extents == Vector3.zero)
                return false;

            bound = coll.bounds;
            return true;
        }
		if(renderers[0].isPartOfStaticBatch)
        {
            bound = renderers[0].bounds;
			for(int i = 1; i < renderers.Length; i++)
            {
				bound.Encapsulate(renderers[i].bounds);
			}

			return (bound.extents != Vector3.zero);
        }

        quat = transform.rotation;
        transform.rotation = Quaternion.Euler(0f,0f,0f);
        for (int i = 0; i < meshes.Length; i++)
        {
            Bounds localBound = meshes[i].mesh.bounds;
            Vector3 ls = meshes[i].gameObject.transform.lossyScale;
            var center = meshes[i].gameObject.transform.TransformPoint(localBound.center);
            var size = meshes[i].gameObject.transform.TransformDirection(Vector3.Scale(ls, localBound.size));
            Bounds temp = new Bounds(center, size);
            if (i == 0)
                bound = temp;
            else
                bound.Encapsulate(temp);
        }
        transform.rotation = quat;

        return true;
	}

    void SetPoints()
    {
        Vector3 bc = transform.position + quat * (bound.center - transform.position);

        topFrontRight = bc + quat * Vector3.Scale(bound.extents, new Vector3(1, 1, 1));
        topFrontLeft = bc + quat * Vector3.Scale(bound.extents, new Vector3(-1, 1, 1));
        topBackLeft = bc + quat * Vector3.Scale(bound.extents, new Vector3(-1, 1, -1));
        topBackRight = bc + quat * Vector3.Scale(bound.extents, new Vector3(1, 1, -1));
        bottomFrontRight = bc + quat * Vector3.Scale(bound.extents, new Vector3(1, -1, 1));
        bottomFrontLeft = bc + quat * Vector3.Scale(bound.extents, new Vector3(-1, -1, 1));
        bottomBackLeft = bc + quat * Vector3.Scale(bound.extents, new Vector3(-1, -1, -1));
        bottomBackRight = bc + quat * Vector3.Scale(bound.extents, new Vector3(1, -1, -1));

        corners = new Vector3[] { topFrontRight, topFrontLeft, topBackLeft, topBackRight,
               bottomFrontRight, bottomFrontLeft, bottomBackLeft, bottomBackRight};
    }

    void SetLines()
    {	
		int i1;
		int linesCount = 12;

		lines = new Vector3[linesCount,2];
		for (int i = 0; i < 4; i++)
        {
			i1 = (i + 1) % 4;//top rectangle
			lines[i, 0] = corners[i];
			lines[i, 1] = corners[i1];
			//break;
			i1 = i + 4;//vertical lines
			lines[i + 4, 0] = corners[i];
			lines[i + 4, 1] = corners[i1];
			//bottom rectangle
			lines[i + 8, 0] = corners[i1];
			i1 = 4 + (i + 1) % 4;
			lines[i + 8, 1] = corners[i1];
		}
	}

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        meshes = GetComponentsInChildren<MeshFilter>();
    }

    void DrawBoundBoxes(bool turnedOn)
    {
        if (turnedOn)
        {
            if (PrepareBounds())
                cameralines.SetOutlines(lines, lineColor);
        }
        else
        {
            cameralines.ClearOutlines();
            isInitialized = false;
        }
    }

    void Start()
    {
        mcamera = Camera.main;
        cameralines = mcamera.GetComponent<BoundBoxes_drawLines>();

        DrawBoundBoxes(drawFlag);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            drawFlag = !drawFlag;
            DrawBoundBoxes(drawFlag);
        }
    }
}
