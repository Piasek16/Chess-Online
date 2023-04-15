using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece, IFirstMovable {
	public bool FirstMove { get; set; } = false;

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
}
