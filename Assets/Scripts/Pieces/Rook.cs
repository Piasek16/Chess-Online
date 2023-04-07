using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece {

    public override List<Vector2Int> PossibleMoves { 
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveGenerator.Instance.GetVerticalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        } 
    }
}
