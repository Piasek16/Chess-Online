using System.Collections.Generic;
using UnityEngine;

public class MoveManager : MonoBehaviour {

    public static MoveManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(this); else Instance = this;
    }

    public List<Vector2Int> GetDiagonalMoves(Vector2Int position) {
        List<Vector2Int> moves = new List<Vector2Int>();
        for (int i = 1; position.x + i < 8 && position.y + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x + i, position.y + i);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
        }
        for (int i = 1; position.x + i < 8 && position.y - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x + i, position.y - i);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
        }
        for (int i = 1; position.x - i >= 0 && position.y - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x - i, position.y - i);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
        }
        for (int i = 1; position.x - i >= 0 && position.y + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x - i, position.y + i);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
        }
        return moves;
    }

    public List<Vector2Int> GetVerticalMoves(Vector2Int position) {
        List<Vector2Int> moves = new List<Vector2Int>();
        for (int i = 1; position.y + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x, position.y + i);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
        }
        for (int i = 1; position.x + i < 8; i++) {
            var _newPosition = new Vector2Int(position.x + i, position.y);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
        }
        for (int i = 1; position.y - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x, position.y - i);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
        }
        for (int i = 1; position.x - i >= 0; i++) {
            var _newPosition = new Vector2Int(position.x - i, position.y);
            if (BoardManager.Instance.GetPieceFromSpace(_newPosition) == null) moves.Add(_newPosition); else { moves.Add(_newPosition); break; }
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

    public List<Vector2Int> GetMovesForward(Vector2Int position, int ammount = 1) {
        List<Vector2Int> moves = new List<Vector2Int>();
        for (int i = 1; position.y + i < 8 && i <= ammount; i++) {
            moves.Add(new Vector2Int(position.x, position.y + i));
        }
        return moves;
    }

    public List<Vector2Int> GetPawnDiagonalMoves(Vector2Int position) {
        List<Vector2Int> moves = new List<Vector2Int>();
        if (position.x - 1 >= 0 && position.y + 1 < 8)
            if (BoardManager.Instance.GetPieceFromSpace(position.x - 1, position.y + 1) != null)
                moves.Add(new Vector2Int(position.x - 1, position.y + 1));
        if (position.x + 1 < 8 && position.y + 1 < 8)
            if (BoardManager.Instance.GetPieceFromSpace(position.x + 1, position.y + 1) != null)
                moves.Add(new Vector2Int(position.x + 1, position.y + 1));
        return moves;
    }
}
