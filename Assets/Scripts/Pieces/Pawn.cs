using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Pawn : Piece {

    private bool firstMove = true;
    private bool isGhost = false;
    private Pawn ghostParent;

    public override List<Vector2Int> PossibleMoves {
        get {
            possibleMoves.Clear();
            possibleMoves.AddRange(MoveManager.Instance.GetPawnMovesForward(Position, ID > 0, firstMove));
            possibleMoves.AddRange(MoveManager.Instance.GetPawnDiagonalMoves(Position, ID > 0));
            RemoveFriendlyPiecesFromMoves();
            return possibleMoves;
        }
    }

    public void FirstMoveMade(bool wasTwoSquares) {
        firstMove = false;
        if (wasTwoSquares) {
            var left = new Vector2Int(Position.x - 1, Position.y);
            var right = new Vector2Int(Position.x + 1, Position.y);
            bool doSummon = false;
            if (MoveManager.Instance.isPositionValid(left)) {
                if (BoardManager.Instance.GetPieceFromSpace(left)?.GetType() == typeof(Pawn)) {
                    doSummon = true;
                }
            }
            if (MoveManager.Instance.isPositionValid(right)) {
                if (BoardManager.Instance.GetPieceFromSpace(right)?.GetType() == typeof(Pawn)) {
                    doSummon = true;
                }
            }
            if (doSummon) SummonGhostPawnBehind();
        }
    }

    Vector2Int behind;
    private void SummonGhostPawnBehind() {
        behind = new Vector2Int(Position.x, Position.y + (ID > 0 ? - 1 : 1));
        if (MoveManager.Instance.isPositionValid(behind)) {
            Debug.Log("Summoning a ghost on " + behind);
            BoardManager.Instance.SetSpace(behind, BoardManager.PieceType.WPawn);
            var ghost = BoardManager.Instance.GetPieceFromSpace(behind);
            GameSessionManager.Instance.WhitesTurn.OnValueChanged += ParentDisposeOfGhost; //Only ghost creator can destroy it
            ghost.GetComponent<Pawn>().InitGhost(Position);
            if (NetworkManager.Singleton.IsServer) {
                Debug.Log("Sending a client summon ghost rpc");
                GameSessionManager.Instance.SummonGhostPawnBehindClientRPC(behind, Position);
            } else {
                Debug.Log("Sending a server summon ghost rpc");
                GameSessionManager.Instance.SummonGhostPawnBehindServerRPC(behind, Position);
            }
        }
    }

    public void InitGhost(Vector2Int pawnParentLocation) {
        isGhost = true;
        ghostParent = BoardManager.Instance.GetPieceFromSpace(pawnParentLocation).GetComponent<Pawn>();
        ID = ghostParent.ID;
        Destroy(GetComponent<SpriteRenderer>());
    }

    void OnDestroy() {
        if (isGhost && ghostParent != null) {
            Destroy(ghostParent.gameObject);
        }
    }

    public void ParentDisposeOfGhost(bool old, bool ne) {
        if (!GameSessionManager.Instance.MyTurn) return;
        GameSessionManager.Instance.WhitesTurn.OnValueChanged -= ParentDisposeOfGhost;
        (BoardManager.Instance.GetPieceFromSpace(behind) as Pawn)?.DisposeOfGhost();
        if (NetworkManager.Singleton.IsServer) GameSessionManager.Instance.DisposeOfGhostClientRPC(behind); else GameSessionManager.Instance.DisposeOfGhostServerRPC(behind);
    }

    public void DisposeOfGhost() {
        if (transform.parent.childCount > 1) return; //Scheduled for destruction
        Debug.Log("Disposed of ghost on " + Position); //For some reason this triggers additionally on enemy side without any errors
        if (isGhost) {
            Debug.Log("is ghost trigger");
            ghostParent = null;
            Destroy(gameObject);
        }
    }
}
