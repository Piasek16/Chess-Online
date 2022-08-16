using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece {

    public bool firstMove = true;

    public override List<Vector2Int> PossibleMoves { 
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveManager.Instance.GetVerticalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        } 
    }

    public void FirstMoveMade() {
        firstMove = false;
    }
}
