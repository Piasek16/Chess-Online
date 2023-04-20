using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece, IFirstMovable {
	public override char Symbol => ID > 0 ? 'R' : 'r';
	public bool FirstMove { get; set; } = false;

    public void ReinitializeValues() {
		FirstMove = false;
	}

	public override List<Vector2Int> GetAllMoves() {
		List<Vector2Int> allMoves = new();
		allMoves.AddRange(MoveGenerator.Instance.GetVerticalMoves(Position));
		return allMoves;
	}
}
