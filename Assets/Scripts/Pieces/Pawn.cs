using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece {

    private bool firstMove = true;

    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetMovesForward(position, firstMove ? 2 : 1));
        possibleMoves.AddRange(MoveManager.Instance.GetPawnDiagonalMoves(position));
        base.HighlightPossibleMoves();
    }

    public void FirstMoveMade() {
        firstMove = false;
    }
}
