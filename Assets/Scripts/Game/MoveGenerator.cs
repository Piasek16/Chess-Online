using System.Collections.Generic;
using UnityEngine;

public class MoveGenerator : MonoBehaviour {

    public static MoveGenerator Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
    }

    void OnDestroy() {
		Instance = null;
	}

    /// <summary>
    /// Checks if a position is valid to add to a list of possible moves and adds it if it is.
    /// </summary>
    /// <param name="moves">List of possible moves to add to</param>
    /// <param name="position">Position to check</param>
    /// <returns>True if a position is valid <br/>
    /// False if there is a real piece on the spot.</returns>
    private bool CheckAndAddPosition(List<Vector2Int> moves, Vector2Int position) {
        var pieceInPosition = BoardManager.Instance.GetPieceFromSpace(position);
        if (pieceInPosition == null || pieceInPosition is GhostPawn) {
            moves.Add(position);
            return true;
        } else {
            moves.Add(position);
            return false;
        }
    }

    public List<Vector2Int> GetDiagonalMoves(Vector2Int position) {
        List<Vector2Int> moves = new List<Vector2Int>();
        for (int i = 1; position.x + i < 8 && position.y + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x + i, position.y + i);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        for (int i = 1; position.x + i < 8 && position.y - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x + i, position.y - i);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        for (int i = 1; position.x - i >= 0 && position.y - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x - i, position.y - i);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        for (int i = 1; position.x - i >= 0 && position.y + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x - i, position.y + i);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        return moves;
    }

    public List<Vector2Int> GetVerticalMoves(Vector2Int position) {
        List<Vector2Int> moves = new List<Vector2Int>();
        for (int i = 1; position.y + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x, position.y + i);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        for (int i = 1; position.x + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x + i, position.y);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        for (int i = 1; position.y - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x, position.y - i);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        for (int i = 1; position.x - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x - i, position.y);
            if (!CheckAndAddPosition(moves, _newPosition)) break;
        }
        return moves;
    }

    public List<Vector2Int> GetKnightMoves(Vector2Int position) {
        List<Vector2Int> moves = new();
        if (position.x + 1 < 8 && position.y + 2 < 8) {
            var _newPosition = new Vector2Int(position.x + 1, position.y + 2);
            moves.Add(_newPosition);
        }
        if (position.x + 2 < 8 && position.y + 1 < 8) {
            var _newPosition = new Vector2Int(position.x + 2, position.y + 1);
            moves.Add(_newPosition);
        }
        if (position.x + 2 < 8 && position.y - 1 >= 0) {
            var _newPosition = new Vector2Int(position.x + 2, position.y - 1);
            moves.Add(_newPosition);
        }
        if (position.x + 1 < 8 && position.y - 2 >= 0) {
            var _newPosition = new Vector2Int(position.x + 1, position.y - 2);
            moves.Add(_newPosition);
        }
        if (position.x - 1 >= 0 && position.y - 2 >= 0) {
            var _newPosition = new Vector2Int(position.x - 1, position.y - 2);
            moves.Add(_newPosition);
        }
        if (position.x - 2 >= 0 && position.y - 1 >= 0) {
            var _newPosition = new Vector2Int(position.x - 2, position.y - 1);
            moves.Add(_newPosition);
        }
        if (position.x - 2 >= 0 && position.y + 1 < 8) {
            var _newPosition = new Vector2Int(position.x - 2, position.y + 1);
            moves.Add(_newPosition);
        }
        if (position.x - 1 >= 0 && position.y + 2 < 8) {
            var _newPosition = new Vector2Int(position.x - 1, position.y + 2);
            moves.Add(_newPosition);
        }
        return moves;
    }

    public List<Vector2Int> GetKingMoves(Vector2Int position) {
        List<Vector2Int> moves = new List<Vector2Int>();
        if (position.x + 1 < 8) moves.Add(new Vector2Int(position.x + 1, position.y));
        if (position.x - 1 >= 0) moves.Add(new Vector2Int(position.x - 1, position.y));
        if (position.y + 1 < 8) moves.Add(new Vector2Int(position.x, position.y + 1));
        if (position.y - 1 >= 0) moves.Add(new Vector2Int(position.x, position.y - 1));
        if (position.x + 1 < 8 && position.y + 1 < 8) moves.Add(new Vector2Int(position.x + 1, position.y + 1));
        if (position.x + 1 < 8 && position.y - 1 >= 0) moves.Add(new Vector2Int(position.x + 1, position.y - 1));
        if (position.x - 1 >= 0 && position.y - 1 >= 0) moves.Add(new Vector2Int(position.x - 1, position.y - 1));
        if (position.x - 1 >= 0 && position.y + 1 < 8) moves.Add(new Vector2Int(position.x - 1, position.y + 1));
        return moves;
    }

    public List<Vector2Int> GetPawnMovesForward(Vector2Int position, bool isWhite, bool firstMove) {
        List<Vector2Int> moves = new List<Vector2Int>();
        if (isWhite && firstMove) {
            for (int i = 1; i < 3; i++) {
                var _newPosition = new Vector2Int(position.x, position.y + i);
                if (!IsPositionValid(_newPosition)) break;
				if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) {
					moves.Add(_newPosition);
				} else {
					break;
				}
            }
        } else if (isWhite && !firstMove) {
            var _newPosition = new Vector2Int(position.x, position.y + 1);
            if (!IsPositionValid(_newPosition)) return moves;
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) {
                moves.Add(_newPosition);
            }
        } else if (!isWhite && firstMove) {
            for (int i = 1; i < 3; i++) {
                var _newPosition = new Vector2Int(position.x, position.y - i);
                if (!IsPositionValid(_newPosition)) break;
				if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) {
					moves.Add(_newPosition);
				} else {
					break;
				}
            }
        } else {
            var _newPosition = new Vector2Int(position.x, position.y - 1);
            if (!IsPositionValid(_newPosition)) return moves;
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) {
                moves.Add(_newPosition);
            }
        }
        return moves;
    }

    public List<Vector2Int> GetPawnDiagonalMoves(Vector2Int position, bool isWhite) {
        List<Vector2Int> moves = new List<Vector2Int>();
        var left = new Vector2Int(position.x - 1, position.y + (isWhite ? 1 : -1));
        var right = new Vector2Int(position.x + 1, position.y + (isWhite ? 1 : -1));
        if (IsPositionValid(left))
            if (BoardManager.Instance.GetPieceFromSpace(left) != null)
                moves.Add(left);
        if (IsPositionValid(right))
            if (BoardManager.Instance.GetPieceFromSpace(right) != null)
                moves.Add(right);
        return moves;
    }

    public static bool IsPositionValid(Vector2Int position) {
        return (position.x >= 0 && position.x < 8 && position.y >= 0 && position.y < 8);
    }
}
