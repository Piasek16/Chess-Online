using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Piece : MonoBehaviour {

	public int ID;

	public Vector2Int Position { get => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)); }

	protected List<Vector2Int> possibleMoves = new List<Vector2Int>();
	public virtual List<Vector2Int> PossibleMoves { get { return possibleMoves; } }

	public bool FirstMove { get; set; } = false;

	private List<GameObject> spacesHighlighted;

	public void HighlightPossibleMoves() {
		if (PossibleMoves.Count == 0) return;
		spacesHighlighted = new();
		foreach (var move in PossibleMoves) {
			var moveSpace = BoardManager.Instance.board[move.x, move.y];
			spacesHighlighted.Add(moveSpace);
			BoardManager.Instance.HighlightTile(moveSpace);
		}
	}

	public void ResetPossibleMovesHighlight() {
		if (spacesHighlighted == null) return;
		foreach (var space in spacesHighlighted) {
			BoardManager.Instance.RestoreTileColor(space);
		}
	}

	protected void RemoveFriendlyPiecesFromMoves() {
		possibleMoves.RemoveAll(move => BoardManager.Instance.board[move.x, move.y].transform.childCount > 0
		&& BoardManager.Instance.board[move.x, move.y].GetComponentInChildren<Piece>().ID * ID > 0);
	}

	protected void RemoveIllegalMoves() {
		possibleMoves.RemoveAll(move => {
			if ((NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().PlayerColor ? 1 : -1) * ID > 0)
				return !MoveGenerator.Instance.IsMoveLegal(Position, move);
			return false;
		});
	}
}
