using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece {
	public override char Symbol => ID > 0 ? 'N' : 'n';

    public override List<Vector2Int> GetAllMoves() {
        List<Vector2Int> allMoves = new();
		allMoves.AddRange(MoveGenerator.Instance.GetKnightMoves(Position));
		return allMoves;
	}
}
