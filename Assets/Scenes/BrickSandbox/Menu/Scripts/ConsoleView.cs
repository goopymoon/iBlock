/// <summary>
/// Marshals events and data between ConsoleController and uGUI.
/// Copyright (c) 2014-2015 Eliot Lash
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections;

public class ConsoleView : MonoBehaviour
{
    // Container for console view, should be a child of this GameObject
    public GameObject viewContainer;
    public Text logTextArea;
    public InputField inputField;

    /// <summary>
	/// The hotkey to show and hide the console window.
	/// </summary>
	public KeyCode toggleKey = KeyCode.BackQuote;

    ConsoleController console = new ConsoleController();
    bool didShow = false;

    void Awake()
    {
        Services.instance.Set<ConsoleView>(this);
    }

    void Start()
    {
        if (console != null)
        {
            console.visibilityChanged += onVisibilityChanged;
            console.logChanged += onLogChanged;
        }
        updateLogStr(console.log);
    }

    ~ConsoleView()
    {
        console.visibilityChanged -= onVisibilityChanged;
        console.logChanged -= onLogChanged;
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
        //Toggle visibility when tilde key pressed
        if (Input.GetKeyUp(toggleKey))
        {
            toggleVisibility();
        }

        //Toggle visibility when 5 fingers touch.
        if (Input.touches.Length == 5)
        {
            if (!didShow)
            {
                toggleVisibility();
                didShow = true;
            }
        }
        else
        {
            didShow = false;
        }
    }

    public void toggleVisibility()
    {
        setVisibility(!viewContainer.activeSelf);
    }

    void setVisibility(bool visible)
    {
        viewContainer.SetActive(visible);
    }

    void onVisibilityChanged(bool visible)
    {
        setVisibility(visible);
    }

    void onLogChanged(string[] newLog)
    {
        updateLogStr(newLog);
    }

    void updateLogStr(string[] newLog)
    {
        if (newLog == null)
        {
            logTextArea.text = "";
        }
        else
        {
            logTextArea.text = string.Join("\n", newLog);
        }
    }

    /// <summary>
    /// Event that should be called by anything wanting to submit the current input to the console.
    /// </summary>
    public void runCommand()
    {
        console.runCommandString(inputField.text);
        inputField.text = "";
    }

    void HandleLog(string message, string stackTrace, LogType type)
    {
        console.appendLogLine(message);
    }
}