using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.tvOS;

public class Player : NetworkBehaviour {
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool PlayerColor;
    public Color32 preMoveColorWhite;
    public Color32 preMoveColorBlack;

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

    Vector2 mousePos;
    Piece attachedPiece = null;
    TMP_Text usernameText;
    Vector2Int? promotionLocation = null;
    List<PreMove> preMoves = new List<PreMove>();
    GameObject capturedFromPremovesContainer;
    Vector2Int? arrowPointerBeginning = null;
    List<GameObject> pointerArrows = new List<GameObject>();

    void Start() {
        //Called after OnNetworkSpawn
        if (!IsOwner) return;
        capturedFromPremovesContainer = new GameObject("PiecesCapturedFromPremovesContainer");
        capturedFromPremovesContainer.transform.position = new Vector3(0, -2, 0);
    }

    //later
    public void OnMyTurn() {
        if (preMoves.Count > 0) {
            var pieceToMove = BoardManager.Instance.GetPieceFromSpace(preMoves[0].from);
            if (pieceToMove != null && GameSessionManager.Instance.IsPieceMyColor(pieceToMove) && !MoveManager.Instance.IsKingInCheck() && pieceToMove.PossibleMoves.Contains(preMoves[0].to)) {
                ExecuteFirstPreMove();
                ReplayPreMoves();
                GameSessionManager.Instance.EndMyTurn();
            } else {
                preMoves.Clear();
            }
        }
    }

    void Update() {
        if (!IsOwner) return;
        if (!GameSessionManager.Instance.GameStarted) return;
        if (promotionLocation != null) {
            GetPromotionPiece();
            return;
        }
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var roundedMousePos = new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
        if (Input.GetMouseButtonDown(0)) PickUpPiece(BoardManager.Instance.GetPieceFromSpace(roundedMousePos));
        if (Input.GetMouseButtonUp(0)) DropPiece(roundedMousePos);
        if (attachedPiece != null) HoldPiece();
        if (Input.GetMouseButtonDown(1)) SetArrowPointerBeginning(roundedMousePos);
        if (Input.GetMouseButtonUp(1)) DrawArrowPointer(roundedMousePos);
    }

    private Vector2Int heldPieceLastPosition;
    private List<Vector2Int> heldPieceLastPossibleMoves;

    void PickUpPiece(Piece piece) {
        if (piece == null || !GameSessionManager.Instance.IsPieceMyColor(piece)) return;
        piece.HighlightPossibleMoves(out heldPieceLastPossibleMoves);
        attachedPiece = piece;
        attachedPiece.transform.parent = null;
        heldPieceLastPosition = piece.Position;
        RemovePointerArrows();
    }

    void DropPiece(Vector2Int dropLocation) {
        if (attachedPiece == null) return;
        attachedPiece.ResetPossibleMovesHighlight();
        if (!heldPieceLastPossibleMoves.Contains(attachedPiece.Position)) {
            RestoreHeldPiecePosition();
        } else if (!GameSessionManager.Instance.MyTurn) {
            RestoreHeldPiecePosition();
            RegisterPreMove(heldPieceLastPosition, dropLocation);
            MovePiece(heldPieceLastPosition, dropLocation, false);
        } else {
            RestoreHeldPiecePosition();
            MovePiece(heldPieceLastPosition, dropLocation, true);
            if (promotionLocation == null) GameSessionManager.Instance.EndMyTurn();
        }
        attachedPiece = null;
        heldPieceLastPosition = Vector2Int.zero;
    }

    void HoldPiece() {
        attachedPiece.transform.position = new Vector3(mousePos.x, mousePos.y, -0.3f);
    }

    void RestoreHeldPiecePosition() {
        if (attachedPiece != null) {
            attachedPiece.transform.parent = BoardManager.Instance.board[heldPieceLastPosition.x, heldPieceLastPosition.y].transform;
            attachedPiece.transform.localPosition = Vector3.zero;
        }
    }

    void RegisterPreMove(Vector2Int from, Vector2Int to) {
        preMoves.Add(new PreMove { from = from, to = to });
    }

    void MovePiece(Vector2Int from, Vector2Int to, bool notifyServer) {
        BoardManager.Instance.MovePiece(from, to);
        BoardManager.Instance.GetPieceFromSpace(to).FirstMove = false;
        if (notifyServer) GameSessionManager.Instance.MovePieceServerRPC(from, to);
        BoardManager.Instance.HighlightMove(from, to);
        sbyte castlingResult = CheckForCastling(from, to);
        if (castlingResult != 0) ExecuteCastle(from, to, castlingResult > 0, notifyServer);
        //if (notifyServer) { if (CheckForPromotion(to)) StartPromotion(to); } //Scheduled for later implementation
    }

    void ExecuteCastle(Vector2Int from, Vector2Int to, bool kingSide, bool notifyServer) {
        Vector2Int right = new Vector2Int(to.x + 1, to.y);
        if (kingSide) {
            Vector2Int left = new Vector2Int(to.x - 1, to.y);
            BoardManager.Instance.MovePiece(right, left);
            if (notifyServer) GameSessionManager.Instance.MovePieceServerRPC(right, left);
            BoardManager.Instance.GetPieceFromSpace(left).FirstMove = false;
        } else {
            Vector2Int left = new Vector2Int(to.x - 2, to.y);
            BoardManager.Instance.MovePiece(left, right);
            if (notifyServer) GameSessionManager.Instance.MovePieceServerRPC(left, right);
            BoardManager.Instance.GetPieceFromSpace(right).FirstMove = false;
        }
    }

    sbyte CheckForCastling(Vector2Int from, Vector2Int to) {
        if (BoardManager.Instance.GetPieceFromSpace(to).GetType() == typeof(King) && Mathf.Abs(to.x - from.x) == 2) {
            if (to.x - from.x == 2) {
                return 1;
            }
            if (to.x - from.x == -2) {
                return -1;
            }
        }
        return 0;
    }

    bool CheckForPromotion(Vector2Int movedToLocation) {
        if ((attachedPiece as Pawn)?.CheckForPromotion() == true) {
            return true;
        }
        return false;
    }

    void StartPromotion(Vector2Int to) {
        promotionLocation = to;
        Debug.Log("Choose a promotion by typing:");
        Debug.Log("1 - Queen");
        Debug.Log("2 - Rook");
        Debug.Log("3 - Bishop");
        Debug.Log("4 - Knight");
    }

    void UpdatePreMovePromotionTarget(BoardManager.PieceType promotionTarget) {
        for (int i = 0; i < preMoves.Count; i++) {
            if (preMoves[i].action == PreMoveSpecialAction.Promotion && preMoves[i].promotionPiece == BoardManager.PieceType.None) {
                var preMove = preMoves[i];
                preMove.promotionPiece = promotionTarget;
                preMoves[i] = preMove;
            }
        }
        BoardManager.Instance.DestroyPiece(promotionLocation.Value);
        BoardManager.Instance.SetSpace(promotionLocation.Value, promotionTarget);
        promotionLocation = null;
        Debug.Log("PreMove promotion target set to: " + promotionTarget.ToString());
    }

    void GetPromotionPiece() {
        if (preMoves.Count > 0) {
            if (Input.GetKeyDown(KeyCode.Alpha1)) UpdatePreMovePromotionTarget(PlayerColor ? BoardManager.PieceType.WQueen : BoardManager.PieceType.BQueen);
            if (Input.GetKeyDown(KeyCode.Alpha2)) UpdatePreMovePromotionTarget(PlayerColor ? BoardManager.PieceType.WRook : BoardManager.PieceType.BRook);
            if (Input.GetKeyDown(KeyCode.Alpha3)) UpdatePreMovePromotionTarget(PlayerColor ? BoardManager.PieceType.WBishop : BoardManager.PieceType.BBishop);
            if (Input.GetKeyDown(KeyCode.Alpha4)) UpdatePreMovePromotionTarget(PlayerColor ? BoardManager.PieceType.WKnight : BoardManager.PieceType.BKnight);
        } else {
            if (Input.GetKeyDown(KeyCode.Alpha1)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WQueen : BoardManager.PieceType.BQueen);
            if (Input.GetKeyDown(KeyCode.Alpha2)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WRook : BoardManager.PieceType.BRook);
            if (Input.GetKeyDown(KeyCode.Alpha3)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WBishop : BoardManager.PieceType.BBishop);
            if (Input.GetKeyDown(KeyCode.Alpha4)) PromotePawnTo(PlayerColor ? BoardManager.PieceType.WKnight : BoardManager.PieceType.BKnight);
        }
    }

    void PromotePawnTo(BoardManager.PieceType p) {
        if (promotionLocation == null) return;
        BoardManager.Instance.DestroyPiece(promotionLocation.Value);
        BoardManager.Instance.SetSpace(promotionLocation.Value, p);
        GameSessionManager.Instance.PromotePawnToServerRPC(p, promotionLocation.Value);
        promotionLocation = null;
        GameSessionManager.Instance.EndMyTurn();
    }

    enum PreMoveSpecialAction : sbyte {
        None,
        CastleK,
        CastleQ,
        Promotion,
    }

    struct PreMove {
        public Vector2Int from;
        public Vector2Int to;
        public PreMoveSpecialAction action;
        public BoardManager.PieceType promotionPiece;
    }

    void ExecuteFirstPreMove() {
        PreMove preMove = preMoves[0];
        MovePiece(preMove.from, preMove.to, true);
        Debug.Log($"[{PlayerName.Value}] Replayed The first premove from: {preMove.from} to: {preMove.to}");
        preMoves.RemoveAt(0);
    }

    void ReplayPreMoves() {
        Debug.Log($"[{PlayerName.Value}] Replaying the rest of saved premoves...");
        foreach (var preMove in preMoves) {
            MovePiece(preMove.from, preMove.to, false);
            Debug.Log($"[{PlayerName.Value}] Replayed premove from: {preMove.from} to: {preMove.to}");
        }
    }

    /*void PreMovePiece(Vector2Int from, Vector2Int to) {
        Piece pieceToMove = BoardManager.Instance.GetPieceFromSpace(from);
        PreMove preMove = new PreMove { from = from, to = to, action = PreMoveSpecialAction.None, promotionPiece = BoardManager.PieceType.None };
        BoardManager.Instance.MovePiece(from, to);
        switch (CheckForCastling(to)) {
            case 0: break;
            case 1: {
                preMove.action = PreMoveSpecialAction.CastleK;
                break;
            }
            case -1: {
                preMove.action = PreMoveSpecialAction.CastleQ;
                break;
            }
        }
        if (CheckForPromotion(to)) {
            promotionLocation = to;
            preMove.action = PreMoveSpecialAction.Promotion;
            Debug.Log("Choose a promotion by typing:");
            Debug.Log("1 - Queen");
            Debug.Log("2 - Rook");
            Debug.Log("3 - Bishop");
            Debug.Log("4 - Knight");
        }
        BoardManager.Instance.SetTileColor(to, preMoveColorWhite, preMoveColorBlack);
        //pieceToMove.transform.localPosition = new Vector3(0, 0, -0.1f);
        preMoves.Add(preMove);
        Debug.Log("Added a premove of " + pieceToMove.name + " from " + from + " to " + to);
        Debug.Log("Premove special action: " + preMove.action.ToString() + " Promotion target: " + preMove.promotionPiece.ToString());
    }*/

    /*void RegisterPreMove(Piece pieceToMove, Vector2Int from, Vector2Int to) {
        Piece capturedPiece = BoardManager.Instance.GetPieceFromSpace(to);
        PreMove preMove = new PreMove { from = from, to = to, capturedPiece = null, action = PreMoveSpecialAction.None, promotionPiece = BoardManager.PieceType.None };
        if (capturedPiece != null) {
            preMove.capturedPiece = capturedPiece;
            capturedPiece.transform.parent = capturedFromPremovesContainer.transform;
        }
        BoardManager.Instance.MovePiece(from, to);
        switch (CheckForCastling(to)) {
            case 0: break;
            case 1: {
                preMove.action = PreMoveSpecialAction.CastleK;
                break;
            }
            case -1: {
                preMove.action = PreMoveSpecialAction.CastleQ;
                break;
            }
        }
        if (CheckForPromotion(to)) {
            promotionLocation = to;
            Debug.Log("Choose a promotion by typing:");
            Debug.Log("1 - Queen");
            Debug.Log("2 - Rook");
            Debug.Log("3 - Bishop");
            Debug.Log("4 - Knight");
        }
        BoardManager.Instance.SetTileColor(to, preMoveColorWhite, preMoveColorBlack);

        pieceToMove.transform.localPosition = new Vector3(0, 0, -0.1f);
        preMoves.Add(preMove);
        Debug.Log("Added a premove of " + pieceToMove.name + " from " + from + " to " + to + " while capturing " + capturedPiece);
        Debug.Log("Premove special action: " + preMove.action.ToString() + " Promotion target: " + preMove.promotionPiece.ToString());
    }*/

    /*void ExecuteFirstPreMove() {
        PreMove preMove = preMoves[0];
        Vector2Int oldPosition = preMove.from;
        Vector2Int newPosition = preMove.to;
        var preMovePiece = BoardManager.Instance.GetPieceFromSpace(oldPosition);

        attachedPiece = preMovePiece;
        heldPieceLastPosition = oldPosition;

        Debug.Log("Executing a premove of " + preMovePiece.name + " from " + oldPosition);

        BoardManager.Instance.MovePiece(oldPosition, newPosition);
        BoardManager.Instance.RestoreTileColor(newPosition);

        RunLocalMoveLogic(newPosition);

        attachedPiece = null;

        Debug.Log("Premove executed, piece moved to " + newPosition);
        preMoves.RemoveAt(0);
        Debug.Log("Removed premove 0 from list");
        Debug.Log("Remaining premoves: " + preMoves.Count);
        GameSessionManager.Instance.EndMyTurn();
    }*/

    /*public void ReplayPreMoves() {
        for (int i = 0; i < preMoves.Count; i++) {
            var preMove = preMoves[i];
            *//*if (!GameSessionManager.Instance.IsPieceMyColor(BoardManager.Instance.GetPieceFromSpace(preMove.from))) {
                preMoves.Clear();
                break;
            }*/
            /*var cappedPiece = BoardManager.Instance.GetPieceFromSpace(preMove.to);
            if (cappedPiece != null) preMove.capturedPiece = cappedPiece;
            if (preMove.capturedPiece != null) preMove.capturedPiece.transform.parent = capturedFromPremovesContainer.transform;*//*
            attachedPiece = BoardManager.Instance.GetPieceFromSpace(preMove.from);
            heldPieceLastPosition = preMove.from;
            BoardManager.Instance.MovePiece(preMove.from, preMove.to);
            BoardManager.Instance.SetTileColor(preMove.to, preMoveColorWhite, preMoveColorBlack);
            RunLocalMoveLogic(preMove.to);
            attachedPiece = null;
            //BoardManager.Instance.GetPieceFromSpace(preMove.to).transform.localPosition = new Vector3(0, 0, -0.1f);
            Debug.Log("Redoing a move from " + preMove.from + " to " + preMove.to);
            preMoves[i] = preMove;
        }
    }*/

    public void ClearPreMoves() {
        preMoves.Clear();
    }

    /// <summary>
    /// Restores the official board state synchronized between players, by reverting player made premoves
    /// </summary>
    /*public void RevertPremoves() {
        if (preMoves != null) {
            preMoves.Reverse();
            foreach (PreMove preMove in preMoves) {
                if (!GameSessionManager.Instance.IsPieceMyColor(BoardManager.Instance.GetPieceFromSpace(preMove.to))) continue;
                BoardManager.Instance.MovePiece(preMove.to, preMove.from);
                BoardManager.Instance.RestoreTileColor(preMove.to);
                Debug.Log("Restoring a move to " + preMove.to + " from " + preMove.from);
                if (preMove.capturedPiece != null) preMove.capturedPiece.transform.parent = BoardManager.Instance.board[preMove.to.x, preMove.to.y].transform;
            }
            preMoves.Reverse();
        }
    }*/

    /*public void ReplayPremoves() {
        if (preMoves != null) {
            for (int i = 0; i < preMoves.Count; i++) {
                var preMove = preMoves[i];
                if (!GameSessionManager.Instance.IsPieceMyColor(BoardManager.Instance.GetPieceFromSpace(preMove.from))) {
                    preMoves.Clear();
                    break;
                }
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
    }*/

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
}
