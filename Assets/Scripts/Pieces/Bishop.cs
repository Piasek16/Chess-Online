using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bishop : Piece {
    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetDiagonalMoves(position));
        base.HighlightPossibleMoves();
    }
}
