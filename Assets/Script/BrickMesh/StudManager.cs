using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StudManager : MonoBehaviour {

    private static StudManager _instance;
    public static StudManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType(typeof(StudManager)) as StudManager;
                if (_instance == null)
                {
                    GameObject container = new GameObject();
                    container.name = "StudManager";
                    _instance = container.AddComponent(typeof(StudManager)) as StudManager;
                }
            }

            return _instance;
        }
    }
}
