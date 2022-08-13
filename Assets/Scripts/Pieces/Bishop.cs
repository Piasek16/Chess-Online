using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece {
    public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveManager.Instance.GetDiagonalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        }
    }
}
