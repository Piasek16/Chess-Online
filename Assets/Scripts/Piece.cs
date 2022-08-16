using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Piece : MonoBehaviour {

    public int ID;

    public Vector2Int Position { get => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)); }

    protected List<Vector2Int> possibleMoves;
    public virtual List<Vector2Int> PossibleMoves { get { return possibleMoves; } }
    private List<Vector2Int> highlightedPossibleMoves;

    void Start() {
        possibleMoves = new List<Vector2Int>();
    }

    public void HighlightPossibleMoves(out List<Vector2Int> oldPossibleMoves) {
        oldPossibleMoves = PossibleMoves;
        highlightedPossibleMoves = new List<Vector2Int>(PossibleMoves); //Copy list
        if (possibleMoves == null) return;
        foreach (var move in possibleMoves) {
            var moveSpace = BoardManager.Instance.board[move.x, move.y];
            moveSpace.GetComponent<MeshRenderer>().material.color -= BoardManager.Instance.highlightOffsetColor;
        }
    }

    public void ResetPossibleMovesHighlight() {
        if (highlightedPossibleMoves == null) return;
        foreach (var move in highlightedPossibleMoves) {
            BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.color += BoardManager.Instance.highlightOffsetColor;
        }
    }

    protected void RemoveFriendlyPiecesFromMoves() {
        possibleMoves.RemoveAll(move => BoardManager.Instance.board[move.x, move.y].transform.childCount > 0 && BoardManager.Instance.board[move.x, move.y].GetComponentInChildren<Piece>().ID * ID > 0);
    }

    protected void RemoveIllegalMoves() {
        possibleMoves.RemoveAll(move => {
            if ((NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().playerColor ? 1 : -1) * ID > 0)
                return !MoveManager.Instance.IsMoveLegal(Position, move);
            return false;
        });
    }
}
