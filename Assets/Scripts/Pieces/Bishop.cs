using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece {
    public override char Symbol => ID > 0 ? 'B' : 'b';

    public override List<Vector2Int> GetAllMoves() {
		List<Vector2Int> allMoves = new();
		allMoves.AddRange(MoveGenerator.Instance.GetDiagonalMoves(Position));
		return allMoves;
	}
}
