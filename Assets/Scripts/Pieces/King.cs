using System.Collections.Generic;
using UnityEngine;

public class King : Piece, IFirstMovable {
	public override char Symbol => ID > 0 ? 'K' : 'k';
    public bool FirstMove { get; set; } = false;

    public void ReinitializeValues() {
		FirstMove = false;
	}
    
    public override List<Vector2Int> GetAllMoves() {
        List<Vector2Int> allMoves = new();
		allMoves.AddRange(MoveGenerator.Instance.GetKingMoves(Position));
		allMoves.AddRange(GetCastlingMoves());
        return allMoves;
	}

    private List<Vector2Int> GetCastlingMoves() {
        List<Vector2Int> moves = new List<Vector2Int>();
        if (!FirstMove || ClassicGameLogicManager.Instance.IsKingInCheck(Type.GetColor())) return moves;
        var leftRook = BoardManager.Instance.GetPieceFromSpace(new Vector2Int(0, Position.y)) as Rook;
        var rightRook = BoardManager.Instance.GetPieceFromSpace(new Vector2Int(7, Position.y)) as Rook;
        if (leftRook != null && leftRook.FirstMove 
            && BoardManager.Instance.GetPieceFromSpace(new Vector2Int(Position.x - 1, Position.y)) == null
            && ClassicGameLogicManager.Instance.IsMoveLegal(new Move(Position, new Vector2Int(Position.x - 1, Position.y)), Type.GetColor())
            && BoardManager.Instance.GetPieceFromSpace(new Vector2Int(Position.x - 2, Position.y)) == null
			&& ClassicGameLogicManager.Instance.IsMoveLegal(new Move(Position, new Vector2Int(Position.x - 2, Position.y)), Type.GetColor())
			&& BoardManager.Instance.GetPieceFromSpace(new Vector2Int(Position.x - 3, Position.y)) == null
            && ClassicGameLogicManager.Instance.IsMoveLegal(new Move(Position, new Vector2Int(Position.x - 3, Position.y)), Type.GetColor())) {
            var _newPosition = new Vector2Int(Position.x - 2, Position.y);
            var _oldPiece = BoardManager.Instance.GetPieceFromSpace(_newPosition);
            if (_oldPiece == null) moves.Add(_newPosition);
        }
        if (rightRook != null && rightRook.FirstMove 
            && BoardManager.Instance.GetPieceFromSpace(new Vector2Int(Position.x + 1, Position.y)) == null
			&& ClassicGameLogicManager.Instance.IsMoveLegal(new Move(Position, new Vector2Int(Position.x + 1, Position.y)), Type.GetColor())
			&& BoardManager.Instance.GetPieceFromSpace(new Vector2Int(Position.x + 2, Position.y)) == null
            && ClassicGameLogicManager.Instance.IsMoveLegal(new Move(Position, new Vector2Int(Position.x + 2, Position.y)), Type.GetColor())) {
            var _newPosition = new Vector2Int(Position.x + 2, Position.y);
            var _oldPiece = BoardManager.Instance.GetPieceFromSpace(_newPosition);
            if (_oldPiece == null) moves.Add(_newPosition);
        }
        return moves;
    }
}
