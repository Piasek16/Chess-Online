using UnityEngine;
using System.Collections;
using System.IO;

public class CustomLogger : MonoBehaviour {

    uint qsize = 15;  // number of messages to keep
    Queue myLogQueue = new Queue();

    string logFileName;

    void Start() {
        DontDestroyOnLoad(gameObject);
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
        TextWriter tw = new StreamWriter(logFileName, true);
        string line = "[" + type + "] : " + logString;
        myLogQueue.Enqueue(line);
        tw.WriteLine("[" + System.DateTime.Now + "] " + line);
        if (type == LogType.Exception) {
            myLogQueue.Enqueue(stackTrace);
            tw.WriteLine(stackTrace);
        }
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();
        tw.Close();
    }

    void OnGUI() {
        GUILayout.BeginArea(new Rect(0, 100, 400, Screen.height));
        GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
        GUILayout.EndArea();
    }
}