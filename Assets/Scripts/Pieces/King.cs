using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Piece {
    public override void HighlightPossibleMoves() {
        possibleMoves.AddRange(MoveManager.Instance.GetKingMoves(position));
        base.HighlightPossibleMoves();
    }
}
