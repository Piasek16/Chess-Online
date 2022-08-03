using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece {
    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetVerticalMoves(position));
        possibleMoves.AddRange(MoveManager.Instance.GetDiagonalMoves(position));
        base.HighlightPossibleMoves();
    }
}
