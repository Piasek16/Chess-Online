using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece {
	public override char Symbol => ID > 0 ? 'Q' : 'q';

	public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveGenerator.Instance.GetVerticalMoves(Position));
            possibleMoves.AddRange(MoveGenerator.Instance.GetDiagonalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        }
    }
}
