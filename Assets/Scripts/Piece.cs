using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {

    public int ID;

    private List<Vector2Int> possibleMoves;

    public void HighlightPossibleMoves() {
        possibleMoves = MoveManager.Instance.GetMovesForward(new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)), 5);
        foreach (var move in possibleMoves) {
            BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.color -= BoardManager.Instance.highlightOffsetColor;
        }
    }

    public void ResetPossibleMoves() {
        foreach (var move in possibleMoves) {
            BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.color += BoardManager.Instance.highlightOffsetColor;
        }
        possibleMoves.Clear();
    }
}
