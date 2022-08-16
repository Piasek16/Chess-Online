using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;

public class Player : NetworkBehaviour {
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool playerColor = false;

    Vector2 whitePosition = new Vector2(-2f, 1f);
    Vector2 blackPosition = new Vector2(-2f, 6f);

    public override void OnNetworkSpawn() {
        PlayerName.OnValueChanged += UpdatePlayerObjectName;
        if (IsOwner) {
            var _playerName = LoginSessionManager.Instance.Username;
            PlayerName.Value = _playerName;
            UpdatePlayerObjectName(default, default);
        }

        playerColor = IsOwnedByServer;
        if (playerColor) {
            transform.position = whitePosition;
        } else {
            transform.position = blackPosition;
        }

        if (IsOwner) {
            if (!playerColor) Camera.main.transform.rotation = Quaternion.Euler(0, 0, 180);
        }

        if (IsOwner) BoardManager.Instance.OnPlayerLogin();

        if (IsLocalPlayer && !IsOwnedByServer) GameSessionManager.Instance.StartGameServerRPC();
    }

    void UpdatePlayerObjectName(FixedString128Bytes previous, FixedString128Bytes newValue) {
        gameObject.name = "Player (" + PlayerName.Value + ")";
    }

    Camera defaultCamera;
    Vector2 mousePos;
    Piece attachedPiece = null;
    bool isAttached = false;

    void Start() {
        defaultCamera = Camera.main;
    }

    void Update() {
        if (!IsOwner) return;
        mousePos = defaultCamera.ScreenToWorldPoint(Input.mousePosition);
        var roundedMousePos = new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
        if (Input.GetMouseButtonDown(0)) AttachPiece(BoardManager.Instance.GetPieceFromSpace(roundedMousePos.x, roundedMousePos.y));
        if (Input.GetMouseButtonUp(0)) DetachPiece(roundedMousePos);
        if (isAttached) HoldPiece();
    }

    private Vector2Int oldPiecePosition;
    private List<Vector2Int> oldPossibleMoves;

    void AttachPiece(Piece piece) {
        if (piece == null) return;
        if (piece.ID > 0 && !playerColor || piece.ID < 0 && playerColor) return;
        piece.HighlightPossibleMoves(out oldPossibleMoves);
        attachedPiece = piece;
        attachedPiece.transform.parent = null;
        oldPiecePosition = piece.Position;
        isAttached = true;
    }

    void DetachPiece(Vector2Int location) {
        if (attachedPiece == null) return;
        if (!oldPossibleMoves.Contains(attachedPiece.Position) || !GameSessionManager.Instance.MyTurn) {
            attachedPiece.transform.parent = BoardManager.Instance.board[oldPiecePosition.x, oldPiecePosition.y].transform;
        } else {
            //Possibly rewrite as MovePieceLocally
            var _oldPiece = BoardManager.Instance.GetPieceFromSpace(location);
            if (_oldPiece != null) {
                if ((_oldPiece as Pawn)?.IsGhost == true && attachedPiece.GetType() == typeof(Pawn)) {
                    (_oldPiece as Pawn)?.ExecuteGhost();
                }
                _oldPiece.transform.parent = null; //Detach from gameboard to make the piece not show up in search for pieces (Destroy gets executerd later in the frame)
                Destroy(_oldPiece.gameObject);
            }
            attachedPiece.transform.parent = BoardManager.Instance.board[location.x, location.y].transform;
            Debug.Log("Moved " + attachedPiece.name + " from " + oldPiecePosition + " to " + location);
            if (IsServer) GameSessionManager.Instance.MovePieceClientRPC(oldPiecePosition, location); else GameSessionManager.Instance.MovePieceServerRPC(oldPiecePosition, location);
            (attachedPiece as Pawn)?.FirstMoveMade(Mathf.Abs(location.y - oldPiecePosition.y) == 2);
            if (attachedPiece.GetType() == typeof(King) && Mathf.Abs(location.x - oldPiecePosition.x) == 2) { //Castling check
                if (location.x - oldPiecePosition.x == 2) {
                    BoardManager.Instance.MovePiece(new Vector2Int(location.x + 1, location.y), new Vector2Int(location.x - 1, location.y));
                    if (IsServer) GameSessionManager.Instance.MovePieceClientRPC(new Vector2Int(location.x + 1, location.y), new Vector2Int(location.x - 1, location.y)); else GameSessionManager.Instance.MovePieceServerRPC(new Vector2Int(location.x + 1, location.y), new Vector2Int(location.x - 1, location.y));
                    (BoardManager.Instance.GetPieceFromSpace(new Vector2Int(location.x - 1, location.y)) as Rook)?.FirstMoveMade();
                }
                if (location.x - oldPiecePosition.x == -2) {
                    BoardManager.Instance.MovePiece(new Vector2Int(location.x - 2, location.y), new Vector2Int(location.x + 1, location.y));
                    if (IsServer) GameSessionManager.Instance.MovePieceClientRPC(new Vector2Int(location.x - 2, location.y), new Vector2Int(location.x + 1, location.y)); else GameSessionManager.Instance.MovePieceServerRPC(new Vector2Int(location.x - 2, location.y), new Vector2Int(location.x + 1, location.y));
                    (BoardManager.Instance.GetPieceFromSpace(new Vector2Int(location.x + 1, location.y)) as Rook)?.FirstMoveMade();
                }
                (attachedPiece as King)?.FirstMoveMade();
            }
            (attachedPiece as King)?.FirstMoveMade();
            (attachedPiece as Rook)?.FirstMoveMade();
            GameSessionManager.Instance.AdvanceTurnServerRPC();
        }
        attachedPiece.transform.localPosition = Vector3.zero;
        attachedPiece.ResetPossibleMovesHighlight();
        attachedPiece = null;
        isAttached = false;
    }

    void HoldPiece() {
        attachedPiece.transform.position = new Vector3(mousePos.x, mousePos.y, -0.1f);
    }
}
