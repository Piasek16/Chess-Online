using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class ClassicGameMoveLogger : MonoBehaviour {
	public static ClassicGameMoveLogger Instance { get; private set; }
	public List<ClassicGameMove> GameMoves = new();
	public GameResult Result { get; private set; } = GameResult.NotDetermined;
	public int MovesSinceLastCaptureOrPawnMove { get; private set; } = 0;

	void Awake() {
		if (Instance != null && Instance != this)
			Destroy(gameObject);
		else
			Instance = this;
	}

	void OnDestroy() {
		Instance = null;
	}

	public void RecordMove(ClassicGameMove move) {
		GameMoves.Add(move);
		move.MoveNumber = GameMoves.Count;
		UpdateMovesMadeDisplay();
		UpdateHalfMoves();
	}

	private void UpdateMovesMadeDisplay() {
		TMP_Text movesMadeDisplay = transform.GetChild(0).GetChild(1).GetComponentInChildren<TMP_Text>();
		movesMadeDisplay.text = GetMovesLog();
	}

	private void UpdateHalfMoves() {
		var lastMove = GameMoves[^1];
		if (lastMove.MovingPieceType == PieceType.WPawn || lastMove.MovingPieceType == PieceType.BPawn || lastMove.Action.HasFlag(ClassicGameMove.SpecialAction.Capture))
			MovesSinceLastCaptureOrPawnMove = 0;
		else
			MovesSinceLastCaptureOrPawnMove++;
	}

	public enum GameResult {
		NotDetermined,
		WhiteWin,
		BlackWin,
		Draw
	}

	public void RecordResult(GameResult result) {
		Result = result;
	}

	public string GetMovesLog() {
		StringBuilder output = new();
		for (int i = 0; i < GameMoves.Count; i++) {
			if (i % 2 == 0)
				output.Append($"{i / 2 + 1}. ");
			output.Append($"{GameMoves[i]} ");
			if (i % 2 == 1)
				output[^1] = '\n';
		}
		return output.ToString();
	}

	public override string ToString() {
		return GetMovesLog().Replace('\n', ' ');
	}

	public void SaveGame() {
		if (Result == GameResult.NotDetermined) {
			Debug.LogError("Cannot save game before it is finished!");
			return;
		}
		string gameSaveFileName = "Classic Chess Game " + System.DateTime.Now;
		string gameSaveFilePath = Application.dataPath + "/" + gameSaveFileName + ".txt";
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
		tw.WriteLine(GetPGNTagLine(PGNTag.Event, "Piasek's Classic Online Chess"));
		tw.WriteLine(GetPGNTagLine(PGNTag.Site, "Chess-Online"));
		tw.WriteLine(GetPGNTagLine(PGNTag.Date, System.DateTime.Now.ToString("yyyy.MM.dd")));
		tw.WriteLine(GetPGNTagLine(PGNTag.Round, "1"));
		tw.WriteLine(GetPGNTagLine(PGNTag.White, whitePlayer));
		tw.WriteLine(GetPGNTagLine(PGNTag.Black, blackPlayer));
		tw.WriteLine(GetPGNTagLine(PGNTag.Result, GetResultSymbol()));
		tw.WriteLine();
		tw.WriteLine(ToString());
		tw.Close();
	}

	private string GetResultSymbol() {
		return Result switch {
			GameResult.WhiteWin => "1-0",
			GameResult.BlackWin => "0-1",
			GameResult.Draw => "1/2-1/2",
			_ => "*",
		};
	}

	enum PGNTag {
		Event,
		Site,
		Date,
		Round,
		White,
		Black,
		Result
	}

	private string GetPGNTagLine(PGNTag tag, string value) {
		return $"[{tag} \"{value}\"]";
	}
}
