using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece {

    public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveManager.Instance.GetKnightMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            return possibleMoves;
        }
    }
}
