using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UIElements;

public class CustomLogger : MonoBehaviour {
    public static CustomLogger Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else { Instance = this; DontDestroyOnLoad(gameObject); };
    }
    public bool HideChat = false;

    uint qsize = 23;  // number of messages to keep
    Queue myLogQueue = new Queue();
    string gameSaveFilePath = null;
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
        tw.WriteLine("[" + System.DateTime.Now + "] " + "[" + type + "] " + logString);
        if (type == LogType.Exception) {
            tw.WriteLine(stackTrace);
        }
        tw.Close();
    }

    void OnGUI() {
        if (HideChat) return;
        GUILayout.BeginArea(new Rect(0, 0, 400, Screen.height));
        GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
        GUILayout.EndArea();
    }

    public void InitGameSave(string GameType) {
        string gameSaveFileName = GameType + " Game " + System.DateTime.Now;
        gameSaveFilePath = Application.dataPath + "/" + gameSaveFilePath + ".txt";
        TextWriter tw = new StreamWriter(gameSaveFilePath);
        tw.WriteLine("[" + gameSaveFileName + "]");
        var players = FindObjectsOfType<Player>();
        string whitePlayer, blackPlayer;
        if (players[0].PlayerColor == true) {
            whitePlayer = players[0].name;
            blackPlayer = players[1].name;
        } else {
            whitePlayer = players[1].name;
            blackPlayer = players[0].name;
        }
        tw.WriteLine("White Player Name: " + whitePlayer);
        tw.WriteLine("Black Player Name: " + blackPlayer);
        tw.Close();
    }

    public void LogMove(Vector2Int from, Vector2Int to, BoardManager.PieceType pieceType) {
        TextWriter tw = new StreamWriter(gameSaveFilePath, true);
        tw.Write(GameSessionManager.Instance.FullmoveNumber + ". "
            + pieceType.ToString() + " from " + from + " to " + to);
        if (GameSessionManager.Instance.WhitePlayersTurn.Value == false) tw.Write("\n");
        tw.Close();
    }
}