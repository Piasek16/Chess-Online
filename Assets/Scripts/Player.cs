using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Player : NetworkBehaviour {
    public NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    bool playerColor = false;
    Vector2 whitePosition = new Vector2(-2f, 1f);
    Vector2 blackPosition = new Vector2(-2f, 6f);

    public override void OnNetworkSpawn() {
        PlayerName.OnValueChanged += UpdatePlayerObjectName;
        if (IsOwner) {
            var _playerName = FindObjectOfType<Canvas>().GetComponent<LoginManager>().username;
            PlayerName.Value = _playerName;
            UpdatePlayerObjectName(default, default);
        }

        playerColor = IsOwnedByServer;
        if (playerColor) {
            transform.position = whitePosition;
        } else {
            transform.position = blackPosition;
        }
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
        mousePos = defaultCamera.ScreenToWorldPoint(Input.mousePosition);
        var roundedMousePos = new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
        if (Input.GetMouseButtonDown(0)) AttachPiece(BoardManager.Instance.GetPieceFromSpace(roundedMousePos.x, roundedMousePos.y));
        if (Input.GetMouseButtonUp(0)) DetachPiece(roundedMousePos);
        if (isAttached) HoldPiece();
    }

    void AttachPiece(Piece piece) {
        if (piece == null) return;
        attachedPiece = piece;
        attachedPiece.transform.parent = null;
        isAttached = true;
        piece.HighlightPossibleMoves();
    }

    void DetachPiece(Vector2Int location) {
        if (attachedPiece == null) return;
        attachedPiece.transform.parent = BoardManager.Instance.board[location.x, location.y].transform;
        attachedPiece.transform.localPosition = Vector3.zero;
        attachedPiece.ResetPossibleMoves();
        attachedPiece.UpdatePosition();
        attachedPiece = null;
        isAttached = false;
    }

    void HoldPiece() {
        attachedPiece.transform.position = mousePos;
    }
}
