using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class BoardManager : MonoBehaviour {
    [SerializeField] public Color whiteColor;
    [SerializeField] public Color blackColor;
    [SerializeField] public Color highlightOffsetColor;
    [SerializeField] Piece[] piecesPrefabs;

    Dictionary<int, Piece> pieces;
    private bool localPlayerColor;

    public King localPlayerKing;
    public GameObject[,] board = new GameObject[8, 8];
    public enum PieceType : int {
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
        localPlayerColor = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().playerColor;
        GenerateBoard();
        DefaultSetup();
        localPlayerKing = (King)(localPlayerColor ? GetPieceFromSpace("e1") : GetPieceFromSpace("e8"));
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

    public void SetTileColor(Vector2Int tileLocation, Color32 colorWhite, Color32 colorBlack) {
        Debug.Log("Tile color from " + tileLocation + " is " + board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color);
        Debug.Log("Black color is: " + blackColor);
        if (board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color == blackColor) {
            board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorBlack;
        } else {
            board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = colorWhite;
        }
    }

    public void RestoreTileColor(Vector2Int tileLocation) {
        if (tileLocation.x % 2 == 0) {
            if (tileLocation.y % 2 == 0) {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = blackColor;
            } else {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = whiteColor;
            }
        } else {
            if (tileLocation.y % 2 == 0) {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = whiteColor;
            } else {
                board[tileLocation.x, tileLocation.y].GetComponent<MeshRenderer>().material.color = blackColor;
            }
        }
    }
}
