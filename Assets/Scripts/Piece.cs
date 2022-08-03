using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {

    public int ID;

    protected Vector2Int position;
    protected List<Vector2Int> possibleMoves;

    public virtual void HighlightPossibleMoves() {
        if (possibleMoves == null) return;
        possibleMoves.RemoveAll(move => BoardManager.Instance.board[move.x, move.y].transform.childCount > 0 && BoardManager.Instance.board[move.x, move.y].GetComponentInChildren<Piece>().ID * ID > 0);
        foreach (var move in possibleMoves) {
            var moveSpace = BoardManager.Instance.board[move.x, move.y];
            moveSpace.GetComponent<MeshRenderer>().material.color -= BoardManager.Instance.highlightOffsetColor;
        }
    }

    public void ResetPossibleMoves() {
        if(possibleMoves == null) return;
        foreach (var move in possibleMoves) {
            BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.color += BoardManager.Instance.highlightOffsetColor;
        }
        possibleMoves.Clear();
    }

    public void UpdatePosition() {
        position = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
    }

    void Start() {
        possibleMoves = new List<Vector2Int>();
        UpdatePosition();
    }
}
