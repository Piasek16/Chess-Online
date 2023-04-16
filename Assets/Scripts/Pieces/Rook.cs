using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece, IFirstMovable {
	public override char Symbol => ID > 0 ? 'R' : 'r';
	public bool FirstMove { get; set; } = false;

	public override List<Vector2Int> PossibleMoves { 
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveGenerator.Instance.GetVerticalMoves(Position));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        } 
    }
}
