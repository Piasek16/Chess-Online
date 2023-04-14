using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece, IFirstMovable {

    public override List<Vector2Int> PossibleMoves { 
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveGenerator.Instance.GetVerticalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        } 
    }

    public bool FirstMove { get; set; } = false;
}
