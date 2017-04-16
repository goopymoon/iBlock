using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraWithTouchFilter : MonoBehaviour {

    private bool touchOperation = false;
    private static List<RaycastResult> tempRaycastResults = new List<RaycastResult>();

    private bool PointIsOverUI(int x, int y)
    {
        var eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(x, y);

        tempRaycastResults.Clear();

        EventSystem.current.RaycastAll(eventDataCurrentPosition, tempRaycastResults);
        return tempRaycastResults.Count > 0;
    }

    protected void CheckTouchOperation()
    {
        if (Input.touchCount > 0)
        {
            Touch touch1 = Input.touches[0];
            if (!touchOperation && touch1.phase == TouchPhase.Began)
            {
                if (!PointIsOverUI((int)touch1.position.x, (int)touch1.position.y))
                    touchOperation = true;
            }
        }
        else
        {
            touchOperation = false;
        }
    }

    public bool IsTouchAvailable()
    {
        return touchOperation;
    }
}
