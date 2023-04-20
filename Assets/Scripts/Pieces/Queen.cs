using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece {
	public override char Symbol => ID > 0 ? 'Q' : 'q';

    public override List<Vector2Int> GetAllMoves() {
		List<Vector2Int> allMoves = new();
		allMoves.AddRange(MoveGenerator.Instance.GetVerticalMoves(Position));
		allMoves.AddRange(MoveGenerator.Instance.GetDiagonalMoves(Position));
		return allMoves;
	}
}
