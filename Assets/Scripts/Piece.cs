using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Piece : MonoBehaviour {

    public int ID;

    public Vector2Int Position { get => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)); }

    protected List<Vector2Int> possibleMoves;
    public virtual List<Vector2Int> PossibleMoves { get { return possibleMoves; } }

    public bool FirstMove { get; protected set; } = true;

    void Start() {
        possibleMoves = new List<Vector2Int>();
    }

    public void FirstMoveMade() { FirstMove = false; }

    private Dictionary<GameObject, Color> spacesHighlighted;

    public void HighlightPossibleMoves(out List<Vector2Int> oldPossibleMoves) {
        oldPossibleMoves = PossibleMoves;
        if (possibleMoves == null) return;
        spacesHighlighted = new Dictionary<GameObject, Color>();
        foreach (var move in possibleMoves) {
            var moveSpace = BoardManager.Instance.board[move.x, move.y];
            Color spaceColor = moveSpace.GetComponent<MeshRenderer>().material.color;
            spacesHighlighted.Add(moveSpace, spaceColor);
            BoardManager.Instance.HighlightTile(moveSpace);
        }
    }

    public void ResetPossibleMovesHighlight() {
        if (spacesHighlighted == null) return;
        foreach (var space in spacesHighlighted.Keys) {
            space.GetComponent<MeshRenderer>().material.color = spacesHighlighted[space];
        }
    }

    protected void RemoveFriendlyPiecesFromMoves() {
        possibleMoves.RemoveAll(move => BoardManager.Instance.board[move.x, move.y].transform.childCount > 0 && BoardManager.Instance.board[move.x, move.y].GetComponentInChildren<Piece>().ID * ID > 0);
    }

    protected void RemoveIllegalMoves() {
        possibleMoves.RemoveAll(move => {
            if ((NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().PlayerColor ? 1 : -1) * ID > 0)
                return !MoveManager.Instance.IsMoveLegal(Position, move);
            return false;
        });
    }
}
