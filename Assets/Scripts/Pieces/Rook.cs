using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece {
    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetVerticalMoves(position));
        base.HighlightPossibleMoves();
    }
}
