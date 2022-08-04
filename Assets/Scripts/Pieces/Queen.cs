using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece {
    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetVerticalMoves(Position));
        possibleMoves.AddRange(MoveManager.Instance.GetDiagonalMoves(Position));
        base.HighlightPossibleMoves();
    }
}
