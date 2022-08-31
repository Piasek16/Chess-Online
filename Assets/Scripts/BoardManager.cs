using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using System;

public class BoardManager : MonoBehaviour {
    [SerializeField] public Color whiteColor;
    [SerializeField] public Color blackColor;
    [SerializeField] public Color highlightOffsetColor;
    [SerializeField] public Color whiteHighlightColor;
    [SerializeField] public Color blackHighlightColor;
    [SerializeField] Piece[] piecesPrefabs;

    Dictionary<int, Piece> pieces;
    private bool localPlayerColor;

    public King localPlayerKing;
    public GameObject[,] board = new GameObject[8, 8];
    public enum PieceType : int {
        Empty = 0,
        WKing = 1,
        WQueen = 2,
        WBishop = 3,
        WKnight = 4,
        WRook = 5,
        WPawn = 6,
        BKing = -1,
        BQueen = -2,
        BBishop = -3,
        BKnight = -4,
        BRook = -5,
        BPawn = -6
    }

    string files = "abcdefgh";
    Shader defaultShader;

    public static BoardManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(this); else Instance = this;
        defaultShader = Shader.Find("Unlit/Color");
        pieces = new Dictionary<int, Piece>();
        foreach (Piece piece in piecesPrefabs) {
            pieces.Add(piece.ID, piece);
        }
    }

    /*void Update() {
        for (int i = 0; i < 8; i++) {
            if (i % 2 == 0) {
                for (int j = 0; j < 8; j++) {
                    board[i, j].GetComponent<MeshRenderer>().material.color = j % 2 == 0 ? blackColor : whiteColor;
                }
            } else {
                for (int j = 0; j < 8; j++) {
                    board[i, j].GetComponent<MeshRenderer>().material.color = j % 2 == 0 ? whiteColor : blackColor;
                }
            }
        }
    }*/

    public void OnPlayerLogin() {
        localPlayerColor = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().PlayerColor;
        GenerateBoard();
        //LoadBoardState("/6*/////-6*////6///-6///5////2//-6*/-1//-2////////-3///5//-6*/-5//////////6*////-6///1/6*/////-6*//");
        //DefaultSetup();
        LoadStateFromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 0");
        //localPlayerKing = (King)(localPlayerColor ? GetPieceFromSpace("e1") : GetPieceFromSpace("e8"));
        var kings = FindObjectsOfType<King>();
        foreach (var king in kings) {
            if (king.ID * (localPlayerColor ? 1 : -1) > 0) localPlayerKing = king;
        }
        Debug.Log("MY king is on: " + localPlayerKing.Position);
    }

    void GenerateBoard() {
        for (int i = 0; i < 8; i++) {
            if (i % 2 == 0) {
                for (int j = 0; j < 8; j++) {
                    board[i, j] = CreateSpace(i, j, j % 2 == 1);
                }
            } else {
                for (int j = 0; j < 8; j++) {
                    board[i, j] = CreateSpace(i, j, j % 2 == 0);
                }
            }
        }
    }

    GameObject CreateSpace(int file, int rank, bool color) { //file is abcdefgh rank is 1-8
        var _newSpace = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _newSpace.transform.position = new Vector3Int(file, rank, 0);
        _newSpace.transform.parent = transform;
        _newSpace.name = files[file] + (rank + 1).ToString();
        _newSpace.GetComponent<MeshRenderer>().material = new Material(defaultShader) {
            color = color ? whiteColor : blackColor
        };
        return _newSpace;
    }

    public void SetSpace(GameObject space, PieceType p) {
        SetSpace((int)space.transform.position.x, (int)space.transform.position.y, p);
    }

    public void SetSpace(string location, PieceType p) {
        if (location.Length > 2) { Debug.LogError("Wrong Piece Format"); return; }
        SetSpace(files.IndexOf(location[0]), location[1] - '0' - 1, p);
    }
    public void SetSpace(char file, int rank, PieceType p) {
        SetSpace(files.IndexOf(file), rank - 1, p);
    }
    public void SetSpace(Vector2Int location, PieceType p) {
        SetSpace(location.x, location.y, p);
    }
    public void SetSpace(int positionX, int positionY, PieceType p) {
        if (!MoveManager.Instance.IsPositionValid(new Vector2Int(positionX, positionY))) return;
        if (p == PieceType.Empty) {
            var piece = GetPieceFromSpace(positionX, positionY);
            if (piece != null) Destroy(piece.gameObject);
            return;
        }
        var _ = Instantiate(pieces[(int)p], Vector3.zero, localPlayerColor ? Quaternion.identity : Quaternion.Euler(0, 0, 180), board[positionX, positionY].transform);
        _.transform.localPosition = Vector3.zero;
    }

    void DefaultSetup() {
        for (int i = 0; i < 8; i++) {
            SetSpace(i, 1, PieceType.WPawn);
            SetSpace(i, 6, PieceType.BPawn);
        }
        SetSpace('a', 1, PieceType.WRook);
        SetSpace('b', 1, PieceType.WKnight);
        SetSpace('c', 1, PieceType.WBishop);
        SetSpace('d', 1, PieceType.WQueen);
        SetSpace('e', 1, PieceType.WKing);
        SetSpace('f', 1, PieceType.WBishop);
        SetSpace('g', 1, PieceType.WKnight);
        SetSpace('h', 1, PieceType.WRook);

        SetSpace('a', 8, PieceType.BRook);
        SetSpace('b', 8, PieceType.BKnight);
        SetSpace('c', 8, PieceType.BBishop);
        SetSpace('d', 8, PieceType.BQueen);
        SetSpace('e', 8, PieceType.BKing);
        SetSpace('f', 8, PieceType.BBishop);
        SetSpace('g', 8, PieceType.BKnight);
        SetSpace('h', 8, PieceType.BRook);
    }

    public Piece GetPieceFromSpace(int positionX, int positionY) {
        if (!MoveManager.Instance.IsPositionValid(new Vector2Int(positionX, positionY))) {
            Debug.LogWarning("Player tried to get a piece from a non existent position " + new Vector2Int(positionX, positionY));
            return null;
        }
        GameObject _space = board[positionX, positionY];
        if (_space.transform.childCount >= 1) return _space.transform.GetComponentInChildren<Piece>();
        return null;
    }

    public Piece GetPieceFromSpace(char file, int rank) {
        return GetPieceFromSpace(files.IndexOf(file), rank - 1);
    }

    public Piece GetPieceFromSpace(Vector2Int position) {
        return GetPieceFromSpace(position.x, position.y);
    }

    public Piece GetPieceFromSpace(string position) {
        if (position.Length > 2) { Debug.LogError("Wrong Piece Format"); return null; }
        return GetPieceFromSpace(files.IndexOf(position[0]), position[1] - '0' - 1);
    }

    public Piece GetPieceFromSpace(GameObject space) {
        return GetPieceFromSpace((int)space.transform.position.x, (int)space.transform.position.y);
    }

    public void MovePiece(Vector2Int oldPiecePosition, Vector2Int newPiecePosition) {
        var _oldPiece = GetPieceFromSpace(newPiecePosition);
        if (_oldPiece != null) {
            if ((_oldPiece as Pawn)?.IsGhost == true && GetPieceFromSpace(oldPiecePosition).GetType() == typeof(Pawn)) {
                (_oldPiece as Pawn)?.ExecuteGhost();
            }
            _oldPiece.transform.parent = null; //Detach from gameboard to make the piece not show up in search for pieces (Destroy gets executerd later in the frame)
            Destroy(_oldPiece.gameObject);
        }
        var movedPiece = GetPieceFromSpace(oldPiecePosition);
        movedPiece.transform.parent = board[newPiecePosition.x, newPiecePosition.y].transform;
        movedPiece.transform.localPosition = Vector3.zero;
        //Debug.Log("Moved " + movedPiece.name + " from " + oldPiecePosition + " to " + newPiecePosition);
    }

    public void SummonGhostPawn(Vector2Int behind, Vector2Int parentPawnPosition) {
        SetSpace(behind, PieceType.WPawn);
        var ghost = GetPieceFromSpace(behind);
        ghost.GetComponent<Pawn>().InitGhost(parentPawnPosition);
    }

    public void DestroyPiece(Vector2Int position) {
        var piece = GetPieceFromSpace(position);
        if (piece != null) {
            piece.transform.parent = null;
            Destroy(piece.gameObject);
        }
    }

    public void SetTileColor(Vector2Int tileLocation, Color colorWhite, Color colorBlack) {
        if (tileLocation.x % 2 == 0) {
            if (tileLocation.y % 2 == 0) {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorBlack;
            } else {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorWhite;
            }
        } else {
            if (tileLocation.y % 2 == 0) {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorWhite;
            } else {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorBlack;
            }
        }
    }

    public void RestoreTileColor(Vector2Int tileLocation) {
        SetTileColor(tileLocation, whiteColor, blackColor);
    }

    public void HighlightTile(GameObject tile) {
        SetTileColor(new Vector2Int((int)tile.transform.position.x, (int)tile.transform.position.y), whiteHighlightColor, blackHighlightColor);
    }

    public void HighlightTile(Vector2Int tile) {
        SetTileColor(tile, whiteHighlightColor, blackHighlightColor);
    }

    public void CleanBoard() {
        foreach (GameObject space in board) {
            SetSpace(space, PieceType.Empty);
        }
    }

    public void LogBoardState() {
        Debug.Log("Current Boardstate: " + ExportBoardState());
    }

    public string ExportBoardState() {
        string boardState = string.Empty;
        foreach (GameObject space in board) {
            var piece = GetPieceFromSpace(space);
            boardState += piece?.ID;
            if (piece?.FirstMove == true) boardState += "*";
            boardState += "/";
        }
        return boardState;
    }

    public void LoadBoardState(string boardStateData) {
        var boardEnumerator = board.GetEnumerator();
        var test = boardStateData.Split('/');
        foreach (string s in boardStateData.Split('/')) {
            if (!boardEnumerator.MoveNext()) break;
            if (string.IsNullOrEmpty(s)) {
                SetSpace((GameObject)boardEnumerator.Current, PieceType.Empty);
            } else {
                string pieceData = s;
                bool firstMoveStatus = pieceData[^1] == '*';
                if (firstMoveStatus) pieceData = pieceData.TrimEnd('*');
                SetSpace((GameObject)boardEnumerator.Current, (PieceType)int.Parse(pieceData));
                if (!firstMoveStatus) GetPieceFromSpace((GameObject)boardEnumerator.Current).FirstMoveMade();
            }
        }
    }

    Vector2Int highlightedFrom = Vector2Int.zero;
    Vector2Int highlightedTo = Vector2Int.zero;
    public void HighlightMove(Vector2Int from, Vector2Int to) {
        RestoreTileColor(highlightedFrom);
        RestoreTileColor(highlightedTo);
        highlightedFrom = from;
        highlightedTo = to;
        HighlightTile(highlightedFrom);
        HighlightTile(highlightedTo);
    }

    Dictionary<char, PieceType> fenPieces = new Dictionary<char, PieceType>() {
        { 'K', PieceType.WKing },
        { 'Q', PieceType.WQueen },
        { 'B', PieceType.WBishop },
        { 'N', PieceType.WKnight },
        { 'R', PieceType.WRook },
        { 'P', PieceType.WPawn },
        { 'k', PieceType.BKing },
        { 'q', PieceType.BQueen },
        { 'b', PieceType.BBishop },
        { 'n', PieceType.BKnight },
        { 'r', PieceType.BRook },
        { 'p', PieceType.BPawn },
    };

    public void LoadStateFromFen(FixedString128Bytes fen) {
        CleanBoard();
        var fenParameters = fen.ToString().Split(' ');
        //Board state
        int file = 0, rank = 7;
        foreach (char c in fenParameters[0]) {
            if (c == '/') {
                file = 0;
                rank--;
            } else {
                if (char.IsDigit(c)) {
                    file += (int)char.GetNumericValue(c);
                } else {
                    SetSpace(file, rank, fenPieces[c]);
                    file++;
                }
            }
        }

        //Hookup below code to game session manager after a rewrite

        //Active color
        bool whitesTurn = fenParameters[1] == "w";
        //Castling rights
        List<Rook> rooks = new List<Rook>(FindObjectsOfType<Rook>());
        if (fenParameters[2] != "-") {
            foreach (char c in fenParameters[2]) {
                switch (c) {
                    case 'K': {
                            var rook = GetPieceFromSpace(7, 0);
                            if (rook.GetType() == typeof(Rook)) rooks.Remove((Rook)rook);
                            break;
                        }
                    case 'k': {
                            var rook = GetPieceFromSpace(7, 7);
                            if (rook.GetType() == typeof(Rook)) rooks.Remove((Rook)rook);
                            break;
                        }
                    case 'Q': {
                            var rook = GetPieceFromSpace(0, 0);
                            if (rook.GetType() == typeof(Rook)) rooks.Remove((Rook)rook);
                            break;
                        }
                    case 'q': {
                            var rook = GetPieceFromSpace(0, 7);
                            if (rook.GetType() == typeof(Rook)) rooks.Remove((Rook)rook);
                            break;
                        }
                }
            }
        }
        Debug.Log("Calling first move on " + rooks.Count + " rooks!");
        rooks?.ForEach(rook => rook.FirstMoveMade());
        //En pessant
        if (fenParameters[3] != "-") {
            //GameSessionManager.Instance.SpawnGhostOnPosition and find its parent
        }
        //All moves
        var movesMade = int.Parse(fenParameters[4]);
        var turnsCompleted = int.Parse(fenParameters[5]);
    }

    [Obsolete("Method not implemented yet.", true)]
    public FixedString128Bytes ExportFenState() {
        FixedString128Bytes fenState = string.Empty;
        var reversedFenPieces = fenPieces.ToDictionary(piece => piece.Value, piece => piece.Key);
        for(int rank = 7; rank >= 0; rank--) {
            int emptySpaces = 0;
            for (int file = 0; file < 8; file++) {
                var piece = GetPieceFromSpace(file, rank);
                if (piece == null) {
                    emptySpaces++;
                } else {
                    if (emptySpaces > 0) fenState += emptySpaces.ToString();
                    emptySpaces = 0;
                    fenState += reversedFenPieces[(PieceType)piece.ID].ToString();
                }
            }
            if (rank != 0) fenState += "/";
        }
        fenState += " ";
        fenState += GameSessionManager.Instance.WhitePlayersTurn.Value ? "w" : "b";
        fenState += " ";
        //Check castling
        fenState += "-";
        fenState += " ";
        //Check ghosts
        fenState += "-";
        fenState += " ";
        //Add moves from Game session manager (when tracked)
        fenState += "?";
        fenState += " ";
        //Add turnsCompleted from Game session manager (when tracked)
        fenState += "?";
        Debug.Log("Exported FEN state:");
        Debug.Log(fenState);
        return fenState;
    }
}
