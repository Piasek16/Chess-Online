using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Pawn : Piece {

	public bool IsGhost => ghostParent != null;

	private Pawn ghostParent = null;

	public override List<Vector2Int> PossibleMoves {
		get {
			possibleMoves.Clear();
			possibleMoves.AddRange(MoveGenerator.Instance.GetPawnMovesForward(Position, ID > 0, FirstMove));
			possibleMoves.AddRange(MoveGenerator.Instance.GetPawnDiagonalMoves(Position, ID > 0));
			RemoveFriendlyPiecesFromMoves();
			RemoveIllegalMoves();
			return possibleMoves;
		}
	}

	public void InitGhost(Vector2Int pawnParentLocation) {
		ghostParent = BoardManager.Instance.GetPieceFromSpace(pawnParentLocation).GetComponent<Pawn>();
		ID = ghostParent.ID;
	}

	public void ExecuteGhost() {
		if (ghostParent == null) return;
		Debug.Log("Executing ghost function in " + gameObject.name + " on " + Position);
		BoardManager.Instance.DestroyPiece(ghostParent);
	}

	public void DisposeOfGhost() {
		if (!IsGhost) { Debug.Log($"Ghost Dispose Cancelled - piece on {Position} is not a ghost"); return; }
		Debug.Log($"Disposed of ghost {name} on {Position}");
		ghostParent = null;
		BoardManager.Instance.ReturnGhost(this);
	}
}
