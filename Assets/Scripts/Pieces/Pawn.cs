using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece, IFirstMovable {
	public override char Symbol => ID > 0 ? 'P' : 'p';
	public bool FirstMove { get; set; } = false;

	public void ReinitializeValues() {
		FirstMove = false;
	}

	public override List<Vector2Int> GetAllMoves() {
		List<Vector2Int> allMoves = new();
		allMoves.AddRange(MoveGenerator.Instance.GetPawnMovesForward(Position, ID > 0, FirstMove));
		allMoves.AddRange(MoveGenerator.Instance.GetPawnDiagonalMoves(Position, ID > 0));
		return allMoves;
	}
}
