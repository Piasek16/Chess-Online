using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece {
    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetKnightMoves(Position));
        base.HighlightPossibleMoves();
    }
}
