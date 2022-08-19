using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using TMPro;

public class Player : NetworkBehaviour {
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool playerColor = false;

    Vector2 whitePosition = new Vector2(-2f, 1f);
    Vector2 blackPosition = new Vector2(-2f, 6f);

    public override void OnNetworkSpawn() {
        usernameText = GetComponentInChildren<TMP_Text>();

        PlayerName.OnValueChanged += UpdatePlayerObjectName;
        if (IsOwner) {
            var _playerName = LoginSessionManager.Instance != null ? LoginSessionManager.Instance.Username : "Piasek";
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
            if (!playerColor) {
                Quaternion rotated = Quaternion.Euler(0, 0, 180);
                Camera.main.transform.SetPositionAndRotation(
                    new Vector3(6.61111f, Camera.main.transform.position.y, Camera.main.transform.position.z), 
                    rotated);
                transform.rotation = rotated;
                foreach (Player p in FindObjectsOfType<Player>()) {
                    p.transform.position = new Vector2(p.transform.position.x + 11, p.transform.position.y);
                    if (p.playerColor == true) p.transform.rotation = rotated;
                }
            }
        }

        if (IsOwner) BoardManager.Instance.OnPlayerLogin();

        if (IsLocalPlayer && !IsOwnedByServer) GameSessionManager.Instance.StartGameServerRPC();
    }

    void UpdatePlayerObjectName(FixedString128Bytes previous, FixedString128Bytes newValue) {
        gameObject.name = "Player (" + PlayerName.Value + ")";
        usernameText.text = PlayerName.Value.ToString();
    }

    Camera defaultCamera;
    Vector2 mousePos;
    Piece attachedPiece = null;
    bool isAttached = false;
    TMP_Text usernameText;
    bool pawnPromotionMode = false;
    Vector2Int promotionLocation;

    void Start() {
        //Called after OnNetworkSpawn
        defaultCamera = Camera.main;
    }

    void Update() {
        if (!IsOwner) return;
        if (pawnPromotionMode) {
            ProceedPromotion();
            return;
        }
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
            attachedPiece.FirstMoveMade();
            if (attachedPiece.GetType() == typeof(King) && Mathf.Abs(location.x - oldPiecePosition.x) == 2) { //Castling check
                if (location.x - oldPiecePosition.x == 2) {
                    BoardManager.Instance.MovePiece(new Vector2Int(location.x + 1, location.y), new Vector2Int(location.x - 1, location.y));
                    if (IsServer) GameSessionManager.Instance.MovePieceClientRPC(new Vector2Int(location.x + 1, location.y), new Vector2Int(location.x - 1, location.y)); else GameSessionManager.Instance.MovePieceServerRPC(new Vector2Int(location.x + 1, location.y), new Vector2Int(location.x - 1, location.y));
                    BoardManager.Instance.GetPieceFromSpace(new Vector2Int(location.x - 1, location.y))?.FirstMoveMade();
                }
                if (location.x - oldPiecePosition.x == -2) {
                    BoardManager.Instance.MovePiece(new Vector2Int(location.x - 2, location.y), new Vector2Int(location.x + 1, location.y));
                    if (IsServer) GameSessionManager.Instance.MovePieceClientRPC(new Vector2Int(location.x - 2, location.y), new Vector2Int(location.x + 1, location.y)); else GameSessionManager.Instance.MovePieceServerRPC(new Vector2Int(location.x - 2, location.y), new Vector2Int(location.x + 1, location.y));
                    BoardManager.Instance.GetPieceFromSpace(new Vector2Int(location.x + 1, location.y))?.FirstMoveMade();
                }
            }
            if ((attachedPiece as Pawn)?.CheckForPromotion() == true) {
                pawnPromotionMode = true;
                promotionLocation = location;
                Debug.Log("Choose a promotion by typing:");
                Debug.Log("1 - Queen");
                Debug.Log("2 - Rook");
                Debug.Log("3 - Bishop");
                Debug.Log("4 - Knight");
            }
            if (!pawnPromotionMode) GameSessionManager.Instance.AdvanceTurnServerRPC();
        }
        attachedPiece.transform.localPosition = Vector3.zero;
        attachedPiece.ResetPossibleMovesHighlight();
        attachedPiece = null;
        isAttached = false;
    }

    void HoldPiece() {
        attachedPiece.transform.position = new Vector3(mousePos.x, mousePos.y, -0.1f);
    }

    void ProceedPromotion() {
        if (Input.GetKeyDown(KeyCode.Alpha1)) PromotePawnTo(playerColor ? BoardManager.PieceType.WQueen : BoardManager.PieceType.BQueen);
        if (Input.GetKeyDown(KeyCode.Alpha2)) PromotePawnTo(playerColor ? BoardManager.PieceType.WRook : BoardManager.PieceType.BRook);
        if (Input.GetKeyDown(KeyCode.Alpha3)) PromotePawnTo(playerColor ? BoardManager.PieceType.WBishop : BoardManager.PieceType.BBishop);
        if (Input.GetKeyDown(KeyCode.Alpha4)) PromotePawnTo(playerColor ? BoardManager.PieceType.WKnight : BoardManager.PieceType.BKnight);
    }

    void PromotePawnTo(BoardManager.PieceType p) {
        pawnPromotionMode = false;
        BoardManager.Instance.DestroyPiece(promotionLocation);
        BoardManager.Instance.SetSpace(promotionLocation, p);
        if (IsServer) GameSessionManager.Instance.PromotePawnToClientRPC(p, promotionLocation); else GameSessionManager.Instance.PromotePawnToServerRPC(p, promotionLocation);
        promotionLocation = Vector2Int.zero;
        GameSessionManager.Instance.AdvanceTurnServerRPC();
    }
}
