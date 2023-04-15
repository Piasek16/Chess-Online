using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece {
	public override char Symbol => ID > 0 ? 'N' : 'n';

	public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveGenerator.Instance.GetKnightMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        }
    }
}
