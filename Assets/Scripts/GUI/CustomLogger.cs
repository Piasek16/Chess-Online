using UnityEngine;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CustomLogger : MonoBehaviour {
	public static CustomLogger Instance { get; private set; }
	void Awake() {
		if (Instance != null && Instance != this) Destroy(gameObject); else { Instance = this; DontDestroyOnLoad(gameObject); };
	}
	public bool LogBoxHidden { get => !transform.GetChild(0).gameObject.activeSelf; set => transform.GetChild(0).gameObject.SetActive(!value); }

	List<string> logMessages = new();
	string gameSaveFilePath = null;
	string logFileName;

    void Start() {
        logFileName = Application.dataPath + "/ChessOnlineLog.txt";
        Debug.Log("Started logging.");
    }

	void OnEnable() {
		Application.logMessageReceived += HandleLog;
	}

	void OnDisable() {
		Application.logMessageReceived -= HandleLog;
	}

	void HandleLog(string logString, string stackTrace, LogType type) {
		string line = "[" + type + "] : " + logString;
		logMessages.Add(line);
		if (type == LogType.Exception) {
			logMessages.Add(stackTrace);
		}
		if (!LogBoxHidden)
			StartCoroutine(ForceScrollDown());
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
		if (LogBoxHidden) return;
		GetComponentInChildren<TMP_Text>().text = string.Join("\n", logMessages.ToArray());
	}

	IEnumerator ForceScrollDown() {
		yield return new WaitForEndOfFrame();
		Canvas.ForceUpdateCanvases();
		GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0f;
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
}