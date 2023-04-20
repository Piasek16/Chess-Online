using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour {

	public int ID;
	public Vector2Int Position { get => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)); }
	public PieceType Type { get => (PieceType)ID; }
	public abstract char Symbol { get; }

	/// <summary>
	/// A list containing possible legal moves for a piece.
	/// </summary>
	public List<Vector2Int> PossibleMoves {
		get {
			possibleMoves = GetAllMoves();
			RemoveFriendlyPiecesFromMoves();
			RemoveIllegalMoves();
			return possibleMoves;
		}
	}
	protected List<Vector2Int> possibleMoves;
	
	/// <summary>
	/// Returns a list of all possible moves for this piece, regardless of whether they are legal or not.
	/// </summary>
	/// <returns>A list of all possible moves for this piece</returns>
	public abstract List<Vector2Int> GetAllMoves();

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
		possibleMoves.RemoveAll(move => 
			!ClassicGameLogicManager.Instance.IsMoveLegal(new Move(Position, move), Type.GetColor())
		);
	}
}
