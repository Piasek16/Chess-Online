using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using TMPro;

public class Player : NetworkBehaviour {
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool PlayerColor;
    public Color32 preMoveColorWhite;
    public Color32 preMoveColorBlack;

    /*public delegate void GhostDisposal();
    public event GhostDisposal DisposeOfGhosts;*/

    Vector2 whitePosition = new Vector2(-2f, 1f);
    Vector2 blackPosition = new Vector2(-2f, 6f);

    public override void OnNetworkSpawn() {
        Debug.Log("[Client] Spawned player for client ID: " + OwnerClientId);
    }

    public void SetupPlayerData(ulong whitePlayerID, ulong blackPlayerID) {
        Debug.Log("White player id: " + whitePlayerID);
        Debug.Log("Black player id: " + blackPlayerID);
        PlayerColor = OwnerClientId == whitePlayerID;
        Debug.Log("Player color set to: " + PlayerColor);

        if (PlayerColor) {
            transform.position = whitePosition;
        } else {
            transform.position = blackPosition;
        }
        usernameText = GetComponentInChildren<TMP_Text>();
        PlayerName.OnValueChanged += UpdatePlayerObjectName;
        if (IsOwner) {
            string _playerName;
            if (LoginSessionManager.Instance != null) {
                _playerName = LoginSessionManager.Instance.Username;
            } else {
                _playerName = GameTestingManager.Instance.TestingUsername;
            }
            PlayerName.Value = _playerName;
            if (!PlayerColor) {
                Camera.main.GetComponent<CameraManager>().AdjustPositionForBlackPlayer();
            }
        }
        if (GameSessionManager.Instance.LocalPlayer.PlayerColor == false) {
            Quaternion rotated = Quaternion.Euler(0, 0, 180);
            transform.SetPositionAndRotation(new Vector2(transform.position.x + 11, transform.position.y), rotated);
        }
    }

    void UpdatePlayerObjectName(FixedString32Bytes previous, FixedString32Bytes newValue) {
        gameObject.name = "Player (" + newValue + ")";
        usernameText.text = newValue.ToString();
    }

    Camera defaultCamera;
    Vector2 mousePos;
    Piece attachedPiece = null;
    bool isAttached = false;
    TMP_Text usernameText;
    bool pawnPromotionMode = false;
    Vector2Int promotionLocation;
    List<PreMove> preMoves;
    GameObject capturedFromPremovesContainer;
    Vector2Int? arrowPointerBeginning;
    List<GameObject> pointerArrows;

    void Start() {
        //Called after OnNetworkSpawn
        if (!IsOwner) return;
        defaultCamera = Camera.main;
        preMoves = new List<PreMove>();
        arrowPointerBeginning = null;
        pointerArrows = new List<GameObject>();
        capturedFromPremovesContainer = new GameObject("PiecesCapturedFromPremovesContainer");
        capturedFromPremovesContainer.transform.position = new Vector3(0, -2, 0);
    }

    void Update() {
        if (!IsOwner) return;
        if (!GameSessionManager.Instance.GameStarted) return;
        if (pawnPromotionMode) {
            ProceedPromotion();
            return;
        }
        mousePos = defaultCamera.ScreenToWorldPoint(Input.mousePosition);
        var roundedMousePos = new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
        if (Input.GetMouseButtonDown(0)) AttachPiece(BoardManager.Instance.GetPieceFromSpace(roundedMousePos));
        if (Input.GetMouseButtonUp(0)) DetachPiece(roundedMousePos);
        if (isAttached) HoldPiece();
        if (Input.GetMouseButtonDown(1)) SetArrowPointerBeginning(roundedMousePos);
        if (Input.GetMouseButtonUp(1)) DrawArrowPointer(roundedMousePos);
    }

    public void OnMyTurn() {
        //DisposeOfGhosts?.Invoke();
        if (preMoves != null && preMoves.Count > 0) {
            //Official board gets restored by game session
            var pieceToMove = BoardManager.Instance.GetPieceFromSpace(preMoves[0].from);
            if (pieceToMove != null && pieceToMove.ID * (PlayerColor ? 1 : -1) > 0 && !MoveManager.Instance.IsKingInCheck() && pieceToMove.PossibleMoves.Contains(preMoves[0].to)) {
                ExecutePreMove();
                RestorePremoves();
                return;
            } else {
                preMoves.Clear();
            }
        }
    }

    private Vector2Int oldPiecePosition;
    private List<Vector2Int> oldPossibleMoves;

    void AttachPiece(Piece piece) {
        if (piece == null) return;
        if (piece.ID > 0 && !PlayerColor || piece.ID < 0 && PlayerColor) return;
        piece.HighlightPossibleMoves(out oldPossibleMoves);
        attachedPiece = piece;
        attachedPiece.transform.parent = null;
        oldPiecePosition = piece.Position;
        isAttached = true;
        RemovePointerArrows();
    }

    void DetachPiece(Vector2Int location) {
        if (attachedPiece == null) return;
        attachedPiece.ResetPossibleMovesHighlight();
        if (!oldPossibleMoves.Contains(attachedPiece.Position)) {
            RestoreAttachedPiecePosition();
        } else if (!GameSessionManager.Instance.MyTurn) {
            RestoreAttachedPiecePosition();
            RegisterPreMove(attachedPiece, oldPiecePosition, location);
        } else {
            RestoreAttachedPiecePosition();
            BoardManager.Instance.MovePiece(oldPiecePosition, location);
            RunLocalMoveLogic(location);

            if (!pawnPromotionMode) GameSessionManager.Instance.AdvanceTurnServerRPC();
        }
        attachedPiece = null;
        isAttached = false;
    }

    void HoldPiece() {
        attachedPiece.transform.position = new Vector3(mousePos.x, mousePos.y, -0.3f);
    }

    void RestoreAttachedPiecePosition() {
        if (attachedPiece != null) {
            attachedPiece.transform.parent = BoardManager.Instance.board[oldPiecePosition.x, oldPiecePosition.y].transform;
            attachedPiece.transform.localPosition = Vector3.zero;
        }
    }

    void RunLocalMoveLogic(Vector2Int location) {
        Debug.Log("Moved " + attachedPiece.name + " from " + oldPiecePosition + " to " + location);
        GameSessionManager.Instance.MovePieceServerRPC(oldPiecePosition, location);
        BoardManager.Instance.HighlightMove(oldPiecePosition, location);

        (attachedPiece as Pawn)?.FirstMoveMade(Mathf.Abs(location.y - oldPiecePosition.y) == 2);
        attachedPiece.FirstMoveMade();

        CheckForCastling(location);
        CheckForPromotion(location);
    }

    void CheckForCastling(Vector2Int location) {
        if (attachedPiece.GetType() == typeof(King) && Mathf.Abs(location.x - oldPiecePosition.x) == 2) {
            if (location.x - oldPiecePosition.x == 2) {
                Vector2Int right = new Vector2Int(location.x + 1, location.y);
                Vector2Int left = new Vector2Int(location.x - 1, location.y);
                BoardManager.Instance.MovePiece(right, left);
                GameSessionManager.Instance.MovePieceServerRPC(right, left);
                //UpdateSecondPlayerOnMove(right, left);
                BoardManager.Instance.GetPieceFromSpace(left)?.FirstMoveMade();
            }
            if (location.x - oldPiecePosition.x == -2) {
                Vector2Int right = new Vector2Int(location.x + 1, location.y);
                Vector2Int left = new Vector2Int(location.x - 2, location.y);
                BoardManager.Instance.MovePiece(left, right);
                GameSessionManager.Instance.MovePieceServerRPC(left, right);
                //UpdateSecondPlayerOnMove(left, right);
                BoardManager.Instance.GetPieceFromSpace(right)?.FirstMoveMade();
            }
        }
    }

    void CheckForPromotion(Vector2Int location) {
        if ((attachedPiece as Pawn)?.CheckForPromotion() == true) {
            pawnPromotionMode = true;
            promotionLocation = location;
            Debug.Log("Choose a promotion by typing:");
            Debug.Log("1 - Queen");
            Debug.Log("2 - Rook");
            Debug.Log("3 - Bishop");
            Debug.Log("4 - Knight");
        }
    }

    void ProceedPromotion() {
        if (Input.GetKeyDown(KeyCode.Alpha1)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WQueen : BoardManager.PieceType.BQueen);
        if (Input.GetKeyDown(KeyCode.Alpha2)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WRook : BoardManager.PieceType.BRook);
        if (Input.GetKeyDown(KeyCode.Alpha3)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WBishop : BoardManager.PieceType.BBishop);
        if (Input.GetKeyDown(KeyCode.Alpha4)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WKnight : BoardManager.PieceType.BKnight);
    }

    void PromotePawnTo(BoardManager.PieceType p) {
        pawnPromotionMode = false;
        BoardManager.Instance.DestroyPiece(promotionLocation);
        BoardManager.Instance.SetSpace(promotionLocation, p);
        if (IsServer) GameSessionManager.Instance.PromotePawnToClientRPC(p, promotionLocation); else GameSessionManager.Instance.PromotePawnToServerRPC(p, promotionLocation);
        promotionLocation = Vector2Int.zero;
        GameSessionManager.Instance.AdvanceTurnServerRPC();
    }

    void RegisterPreMove(Piece pieceToMove, Vector2Int from, Vector2Int to) {
        Piece capturedPiece = BoardManager.Instance.GetPieceFromSpace(to);
        PreMove preMove = new PreMove { pieceToMove = pieceToMove, from = from, to = to, capturedPiece = capturedPiece };
        preMoves.Add(preMove);
        if (capturedPiece != null) capturedPiece.transform.parent = capturedFromPremovesContainer.transform;
        BoardManager.Instance.SetTileColor(to, preMoveColorWhite, preMoveColorBlack);
        BoardManager.Instance.MovePiece(from, to);
        pieceToMove.transform.localPosition = new Vector3(0, 0, -0.1f);
        Debug.Log("Added a premove of " + pieceToMove.name + " from " + from + " to " + to + " while capturing " + capturedPiece);
    }

    void ExecutePreMove() {
        Vector2Int oldPosition = preMoves[0].from;
        Vector2Int newPosition = preMoves[0].to;
        var preMovePiece = BoardManager.Instance.GetPieceFromSpace(oldPosition);

        attachedPiece = preMovePiece;
        oldPiecePosition = oldPosition;
        isAttached = true;

        Debug.Log("Executing a premove of " + preMovePiece.name + " from " + oldPosition);
        BoardManager.Instance.MovePiece(oldPosition, newPosition);
        BoardManager.Instance.RestoreTileColor(newPosition);

        RunLocalMoveLogic(newPosition);

        attachedPiece = null;
        isAttached = false;

        Debug.Log("Premove executed, piece moved to " + newPosition);
        preMoves.RemoveAt(0);
        Debug.Log("Removed premove 0 from list");
        Debug.Log("Remaining premoves: " + preMoves.Count);
        GameSessionManager.Instance.AdvanceTurnServerRPC();
    }

    void SetArrowPointerBeginning(Vector2Int location) {
        if (arrowPointerBeginning == null) arrowPointerBeginning = location;
    }

    void DrawArrowPointer(Vector2Int tipLocation) {
        if (arrowPointerBeginning == tipLocation) return;
        if (arrowPointerBeginning != null) {
            var arrowVector = tipLocation - arrowPointerBeginning.Value;
            var lookVector = Vector3.Cross((Vector3Int)tipLocation - (Vector3Int)arrowPointerBeginning.Value, Vector3.forward);
            var arrowLength = arrowVector.magnitude;
            Vector3 arrowMiddle = (Vector3)((Vector3Int)arrowPointerBeginning.Value + (Vector3Int)tipLocation) / 2;
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            arrow.transform.position = new Vector3(arrowMiddle.x, arrowMiddle.y, -0.2f);
            arrow.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Unlit/Color")) {
                color = Color.red
            };
            arrow.transform.localScale = new Vector3(arrowLength, 0.2f, 0);
            arrow.transform.rotation = Quaternion.LookRotation(Vector3.forward, lookVector);
            Debug.Log("Creating a pointer arrow: " + arrowPointerBeginning + " -> " + tipLocation + " Direction: " + arrowVector);

            var arrowPointer = CreateArrowPointer();
            arrowPointer.transform.position = new Vector3(tipLocation.x, tipLocation.y, -0.2f);
            arrowPointer.transform.localScale = new Vector3(0.7f, 0.5f, 1);
            arrowPointer.transform.rotation = Quaternion.LookRotation(Vector3.forward, lookVector);
            arrowPointer.transform.rotation *= Quaternion.Euler(0, -180, -90);

            GameObject arrowParent = new GameObject("Arrow");
            arrow.transform.parent = arrowParent.transform;
            arrowPointer.transform.parent = arrowParent.transform;

            arrowPointerBeginning = null;
            pointerArrows.Add(arrowParent);
        }
    }

    GameObject CreateArrowPointer() {
        var obj = new GameObject("ArrowPointer");
        var filter = obj.AddComponent<MeshFilter>();
        var renderer = obj.AddComponent<MeshRenderer>();
        var collider = obj.AddComponent<MeshCollider>();
        var mesh = new Mesh();
        mesh.vertices = new Vector3[3] {
             new Vector3(-0.5f, -0.5f, 0),
             new Vector3(0.5f, -0.5f, 0),
             new Vector3(0f, 0.5f, 0) };
        mesh.uv = new Vector2[3] {
             new Vector2(0, 0),
             new Vector2(1, 0),
             new Vector2(0.5f, 1) };
        mesh.triangles = new int[3] { 0, 1, 2 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
        renderer.material = new Material(Shader.Find("Unlit/Color")) {
            color = Color.red
        };
        return obj;
    }

    void RemovePointerArrows() {
        foreach (GameObject arrow in pointerArrows) {
            Destroy(arrow);
        }
        pointerArrows.Clear();
    }

    struct PreMove {
        public Piece pieceToMove;
        public Vector2Int from;
        public Vector2Int to;
        public Piece capturedPiece;
    }

    /// <summary>
    /// Restores the official board state synchronized between players, by reverting player made premoves
    /// </summary>
    public void RevertPremoves() {
        if (preMoves != null) {
            preMoves.Reverse();
            foreach (PreMove preMove in preMoves) {
                if (BoardManager.Instance.GetPieceFromSpace(preMove.to).ID * (PlayerColor ? 1 : -1) < 0) continue;
                BoardManager.Instance.MovePiece(preMove.to, preMove.from);
                BoardManager.Instance.RestoreTileColor(preMove.to);
                Debug.Log("Restoring a move to " + preMove.to + " from " + preMove.from);
                if (preMove.capturedPiece != null) preMove.capturedPiece.transform.parent = BoardManager.Instance.board[preMove.to.x, preMove.to.y].transform;
            }
            preMoves.Reverse();
        }
    }

    public void RestorePremoves() {
        if (preMoves != null) {
            for (int i = 0; i < preMoves.Count; i++) {
                var preMove = preMoves[i];
                if (BoardManager.Instance.GetPieceFromSpace(preMove.from).ID * (PlayerColor ? 1 : -1) < 0) continue;
                var cappedPiece = BoardManager.Instance.GetPieceFromSpace(preMove.to);
                if (cappedPiece != null) preMove.capturedPiece = cappedPiece;
                if (preMove.capturedPiece != null) preMove.capturedPiece.transform.parent = capturedFromPremovesContainer.transform;
                BoardManager.Instance.MovePiece(preMove.from, preMove.to);
                BoardManager.Instance.SetTileColor(preMove.to, preMoveColorWhite, preMoveColorBlack);
                BoardManager.Instance.GetPieceFromSpace(preMove.to).transform.localPosition = new Vector3(0, 0, -0.1f);
                Debug.Log("Redoing a move from " + preMove.from + " to " + preMove.to);
                preMoves[i] = preMove;
            }
        }
    }
}
