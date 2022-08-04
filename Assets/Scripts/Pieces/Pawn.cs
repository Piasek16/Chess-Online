using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece {

    private bool firstMove = true;

    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetPawnMovesForward(Position, ID > 0, firstMove));
        possibleMoves.AddRange(MoveManager.Instance.GetPawnDiagonalMoves(Position, ID > 0));
        base.HighlightPossibleMoves();
    }

    public void FirstMoveMade() {
        firstMove = false;
    }
}
