using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class MoveTester : MonoBehaviour {

	private FENGameState state = new FENGameState("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
	private int depth = 2;
	private bool useDelay = true;
	private float timeDelay = 0.5f;
	private static int numOfPositions = 0;
	private static int lastNoOfPos = 0;

// Copied from GameTestingManager for build
	void Start() {
		NetworkManager.Singleton.StartHost();
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.I)) {
			GameSessionManager.Instance.InitializeGame();
		}
// End of what was copied

		if (Input.GetKeyDown(KeyCode.U)) {
			state = new FENGameState("rnbqkbnr/pppppppp/8/8/P7/8/1PPPPPPP/RNBQKBNR b KQkq a3 0 1");
			StartMoveTesting();
		}
	}

	public void StartMoveTesting() {
		numOfPositions = 0;
		GameSessionManager.Instance.LoadStateFromFEN(state.ToString());
		Debug.Log("Starting move testing with depth: " + depth);
		if (useDelay)
			StartCoroutine(GenerateMovesWithDelayWrapper(depth, false));
		else {
			//GenerateMoves(depth, true);
			GenerateMovesUnmanaged(depth, true);
			Debug.Log("Total calculated positions: " + numOfPositions);
		}
	}

	public void GenerateMovesUnmanaged(int depth, bool whiteToMove) {
		if (depth == 0) {
			numOfPositions++;
			return;
		}
		var allTiles = GetUnmanagedPieceTilesColorDivided();
		NativeList<Vector2Int> tiles;
		if (whiteToMove)
			tiles = allTiles.Item1;
		else
			tiles = allTiles.Item2;
		foreach (var pieceSpace in tiles) {
			var possibleMovesCopy = GetUnmanagedPossibleMoves(BoardManager.Instance.GetPieceFromSpace(pieceSpace));
			foreach (var pieceMove in possibleMovesCopy) {
				FENGameState boardState = FENGameState.CollectFENState();
				BoardManager.Instance.ExecuteMove(new Move(pieceSpace, pieceMove));
				GenerateMovesUnmanaged(depth - 1, !whiteToMove);
				
				if (depth == 3) {
					Debug.Log("After move: ");
					Debug.Log(new Move(pieceSpace, pieceMove));
					Debug.Log("No of positions is: " + (numOfPositions - lastNoOfPos));
					lastNoOfPos = numOfPositions;
				}
				
				GameSessionManager.Instance.LoadStateFromFEN(boardState.ToString());
			}
			possibleMovesCopy.Dispose();
		}
		allTiles.Item1.Dispose();
		allTiles.Item2.Dispose();
	}

	public void GenerateMoves(int depth, bool whiteToMove) {
		if (depth == 0) {
			numOfPositions++;
			return;
		}
		var allTiles = GetPiecesTiles(GetPiecesOnBoard());
		List<Vector2Int> tiles;
		if (whiteToMove)
			tiles = allTiles.Item1;
		else
			tiles = allTiles.Item2;
		foreach (var pieceSpace in tiles) {
			var possibleMovesCopy = new List<Vector2Int>(BoardManager.Instance.GetPieceFromSpace(pieceSpace).PossibleMoves);
			foreach (var pieceMove in possibleMovesCopy) {
				FENGameState boardState = FENGameState.CollectFENState();
				BoardManager.Instance.ExecuteMove(new Move(pieceSpace, pieceMove));
				GenerateMoves(depth - 1, !whiteToMove);
				GameSessionManager.Instance.LoadStateFromFEN(boardState.ToString());
			}
		}
	}

	private IEnumerator GenerateMovesWithDelayWrapper(int depth, bool whiteToMove) {
		yield return StartCoroutine(GenerateMovesWithDelay(depth, whiteToMove));
		Debug.Log("Total calculated positions: " + numOfPositions);
	}

	private IEnumerator GenerateMovesWithDelay(int depth, bool whiteToMove) {
		if (depth == 0) {
			numOfPositions++;
			yield break;
		}
		var allTiles = GetPiecesTiles(GetPiecesOnBoard());
		List<Vector2Int> tiles;
		if (whiteToMove)
			tiles = allTiles.Item1;
		else
			tiles = allTiles.Item2;
		foreach (var pieceSpace in tiles) {
			var possibleMovesCopy = new List<Vector2Int>(BoardManager.Instance.GetPieceFromSpace(pieceSpace).PossibleMoves);
			foreach (var pieceMove in possibleMovesCopy) {
				FENGameState boardState = FENGameState.CollectFENState();
				BoardManager.Instance.ExecuteMove(new Move(pieceSpace, pieceMove));
				yield return new WaitForSeconds(timeDelay);
				yield return StartCoroutine(GenerateMovesWithDelay(depth - 1, !whiteToMove));
				GameSessionManager.Instance.LoadStateFromFEN(boardState.ToString());
				yield return new WaitForSeconds(timeDelay);
			}
		}
	}

	private List<Piece> GetPiecesOnBoard() {
		List<Piece> pieces = new List<Piece>();
		foreach (var space in BoardManager.Instance.board) {
			if (space.transform.childCount > 0) {
				var piece = space.transform.GetChild(0).GetComponent<Piece>();
				if (piece is Pawn pawn && pawn.IsGhost)
					continue;
				pieces.Add(piece);
			}
		}
		return pieces;
	}

	private (List<Vector2Int>, List<Vector2Int>) GetPiecesTiles(List<Piece> pieces) {
		List<Vector2Int> tilesWhite = new();
		List<Vector2Int> tilesBlack = new();
		foreach (var piece in pieces) {
			if (piece.ID > 0)
				tilesWhite.Add(piece.Position);
			else
				tilesBlack.Add(piece.Position);
		}
		return (tilesWhite, tilesBlack);
	}

	private NativeArray<Vector2Int> GetUnmanagedPossibleMoves(Piece piece) {
		NativeArray<Vector2Int> possibleMoves = new NativeArray<Vector2Int>(piece.PossibleMoves.Count, Allocator.TempJob);
		for (int i = 0; i < piece.PossibleMoves.Count; i++) {
			possibleMoves[i] = piece.PossibleMoves[i];
		}
		return possibleMoves;
	}

	private (NativeList<Vector2Int>, NativeList<Vector2Int>) GetUnmanagedPieceTilesColorDivided() {
		NativeList<Vector2Int> tilesWhite = new NativeList<Vector2Int>(Allocator.TempJob);
		NativeList<Vector2Int> tilesBlack = new NativeList<Vector2Int>(Allocator.TempJob);
		foreach (var space in BoardManager.Instance.board) {
			if (space.transform.childCount > 0) {
				var piece = space.transform.GetChild(0).GetComponent<Piece>();
				if (piece is Pawn pawn && pawn.IsGhost)
					continue;
				if (piece.ID > 0)
					tilesWhite.Add(piece.Position);
				else
					tilesBlack.Add(piece.Position);
			}
		}
		return (tilesWhite, tilesBlack);
	}
}
