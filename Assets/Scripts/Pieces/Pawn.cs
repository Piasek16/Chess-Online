using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Pawn : Piece {

    public bool IsGhost => ghostParent != null;

    private Pawn ghostParent = null;

    public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveManager.Instance.GetPawnMovesForward(Position, ID > 0, FirstMove));
            possibleMoves.AddRange(MoveManager.Instance.GetPawnDiagonalMoves(Position, ID > 0));
            RemoveFriendlyPiecesFromMoves();
            RemoveIllegalMoves();
            return possibleMoves;
        }
    }

    public void FirstMoveMade(bool wasTwoSquares) {
        FirstMove = false;
        if (wasTwoSquares) {
            var left = new Vector2Int(Position.x - 1, Position.y);
            var right = new Vector2Int(Position.x + 1, Position.y);
            bool doSummon = false;
            if (MoveManager.Instance.IsPositionValid(left)) {
                if (BoardManager.Instance.GetPieceFromSpace(left)?.GetType() == typeof(Pawn)) {
                    doSummon = true;
                }
            }
            if (MoveManager.Instance.IsPositionValid(right)) {
                if (BoardManager.Instance.GetPieceFromSpace(right)?.GetType() == typeof(Pawn)) {
                    doSummon = true;
                }
            }
            if (doSummon) SummonGhostPawnBehind();
        }
    }

    private Vector2Int behind;
    private void SummonGhostPawnBehind() {
        behind = new Vector2Int(Position.x, Position.y + (ID > 0 ? - 1 : 1));
        if (MoveManager.Instance.IsPositionValid(behind)) {
            Debug.Log("Summoning a ghost on " + behind);
            BoardManager.Instance.SetSpace(behind, BoardManager.PieceType.WPawn);
            var ghost = BoardManager.Instance.GetPieceFromSpace(behind);
            ghost.GetComponent<Pawn>().InitGhost(Position);
            //ghost.GetComponent<Pawn>().AddGhostDispose();
            GameSessionManager.Instance.SummonGhostPawnServerRPC(Position);
        }
    }

    /*public void AddGhostDispose() {
        NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().DisposeOfGhosts += TriggerGhostDispose;
    }*/

    public void InitGhost(Vector2Int pawnParentLocation) {
        ghostParent = BoardManager.Instance.GetPieceFromSpace(pawnParentLocation).GetComponent<Pawn>();
        ID = ghostParent.ID;
        Destroy(GetComponent<SpriteRenderer>());
    }

    private bool scheduledExecution = false;

    public void ExecuteGhost() {
        if (ghostParent == null) return;
        Debug.Log("Executing ghost function in " + gameObject.name + " on " + Position);
        ghostParent.transform.parent = null;
        Destroy(ghostParent.gameObject);
        scheduledExecution = true;
    }

    /*public void TriggerGhostDispose() {
        NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().DisposeOfGhosts -= TriggerGhostDispose;
        if (NetworkManager.Singleton.IsServer) GameSessionManager.Instance.DisposeOfGhostClientRPC(Position); else GameSessionManager.Instance.DisposeOfGhostServerRPC(Position);
        DisposeOfGhost();
    }*/

    public void DisposeOfGhost() {
        if (ghostParent == null) { Debug.Log("Ghost Dispose Cancelled - piece is not a ghost"); return; }
        if (scheduledExecution) { Debug.Log("Ghost Dispose Cancelled - execution scheduled on frame end"); return; }
        Debug.Log("Disposed of ghost " + name + " on " + Position);
        ghostParent = null;
        transform.parent = null;
        Destroy(gameObject);
    }

    public bool CheckForPromotion() {
        if (ID > 0 && Position.y == 7) return true;
        if (ID < 0 && Position.y == 0) return true;
        return false;
    }
}
