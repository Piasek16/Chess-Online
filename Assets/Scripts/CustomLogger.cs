using UnityEngine;
using System.Collections;
using System.IO;

public class CustomLogger : MonoBehaviour {
    public static CustomLogger Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else { Instance = this; DontDestroyOnLoad(gameObject); };
    }

    uint qsize = 23;  // number of messages to keep
    Queue myLogQueue = new Queue();

    string logFileName;

    void Start() {
        logFileName = Application.dataPath + "/ChessOnlineLog.txt";
        Debug.Log("Started up logging.");
    }

    void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        string line = "[" + type + "] : " + logString;
        myLogQueue.Enqueue(line);
        if (type == LogType.Exception) {
            myLogQueue.Enqueue(stackTrace);
        }
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();
#if !UNITY_EDITOR
        SaveLog(logString, stackTrace, type);
#endif
    }

    void SaveLog(string logString, string stackTrace, LogType type) {
        TextWriter tw = new StreamWriter(logFileName, true);
        tw.WriteLine("[" + System.DateTime.Now + "] " + logString);
        if (type == LogType.Exception) {
            tw.WriteLine(stackTrace);
        }
        tw.Close();
    }

    void OnGUI() {
        GUILayout.BeginArea(new Rect(0, 0, 400, Screen.height));
        GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
        GUILayout.EndArea();
    }
}