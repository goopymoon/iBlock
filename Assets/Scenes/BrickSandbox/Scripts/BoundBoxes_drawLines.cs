using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoundBoxes_drawLines : MonoBehaviour
{
	public Material lineMaterial;

    class OBBLines
    {
        public List<Vector3[,]> outlines;
        public List<Color> colors;

        public OBBLines()
        {
            outlines = new List<Vector3[,]>();
            colors = new List<Color>();
        }

        public void ClearOut()
        {
            outlines.Clear();
            colors.Clear();
        }

        public void SetOutlines(Vector3[,] newOutlines, Color newcolor)
        {
            if (newOutlines.GetLength(0) > 0)
            {
                outlines.Add(newOutlines);
                colors.Add(newcolor);
            }
        }
    }
    private Dictionary<int, OBBLines> obbLines;

	void Awake ()
    {
        obbLines = new Dictionary<int, OBBLines>();
	}
	
	void Start ()
    {
	}

	void OnPostRender()
    {
		if(obbLines == null) return;

	    lineMaterial.SetPass( 0 );
	    GL.Begin( GL.LINES );

        foreach (var element in obbLines)
        {
            for (int j = 0; j < element.Value.outlines.Count; j++)
            {
                GL.Color(element.Value.colors[j]);
                for (int i = 0; i < element.Value.outlines[j].GetLength(0); i++)
                {
                    GL.Vertex(element.Value.outlines[j][i, 0]);
                    GL.Vertex(element.Value.outlines[j][i, 1]);
                }
            }
        }
		GL.End();
	}

    public void ClearOutOBBLines()
    {
        foreach(var element in obbLines)
        {
            element.Value.ClearOut();
        }
    }

    public void ClearOutOBBLines(int key)
    {
        obbLines.Remove(key);
    }

    public void SetOutlines(int key, Vector3[,] newOutlines, Color newcolor)
    {
        OBBLines temp;
        if (!obbLines.TryGetValue(key, out temp))
            temp = new OBBLines();

        temp.SetOutlines(newOutlines, newcolor);
        obbLines[key] = temp;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
