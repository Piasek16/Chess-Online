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

    private void SummonGhostPawnBehind() {
        var behind = new Vector2Int(Position.x, Position.y + (ID > 0 ? - 1 : 1));
        if (MoveManager.Instance.isPositionValid(behind)) {
            Debug.Log("Summoning a ghost on " + behind);
            BoardManager.Instance.SetSpace(behind, BoardManager.PieceType.WPawn);
            var ghost = BoardManager.Instance.GetPieceFromSpace(behind);
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
        ID = 0;
        Destroy(GetComponent<SpriteRenderer>());
    }

    void OnDestroy() {
        if (isGhost && ghostParent != null) {
            Destroy(ghostParent.gameObject);
        }
    }

    public void DisposeOfGhost() {
        if (isGhost) Destroy(gameObject);
    }
}
