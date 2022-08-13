using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece {

    public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveManager.Instance.GetVerticalMoves(Position));
            possibleMoves.AddRange(MoveManager.Instance.GetDiagonalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        }
    }
}
