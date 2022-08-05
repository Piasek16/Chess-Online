using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Piece {
    public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveManager.Instance.GetKingMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            return possibleMoves;
        }
    }
}
