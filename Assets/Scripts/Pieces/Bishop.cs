using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece {
    public override char Symbol => ID > 0 ? 'B' : 'b';

	public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveGenerator.Instance.GetDiagonalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        }
    }
}
