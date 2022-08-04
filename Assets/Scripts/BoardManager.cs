using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class BoardManager : NetworkBehaviour {
    [SerializeField] public Color whiteColor;
    [SerializeField] public Color blackColor;
    [SerializeField] public Color highlightOffsetColor;
    [SerializeField] Piece[] piecesPrefabs;

    Dictionary<int, Piece> pieces;

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
    }

    void Start() {
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
        GenerateBoard();
        DefaultSetup();
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

    void SetSpace(string location, PieceType p) {
        if (location.Length > 2) { Debug.LogError("Wrong Piece Format"); return; }
        SetSpace(files.IndexOf(location[0]), location[1] - 1, p);
    }
    void SetSpace(char file, int rank, PieceType p) {
        SetSpace(files.IndexOf(file), rank - 1, p);
    }
    void SetSpace(int file, int rank, PieceType p) {
        var _ = Instantiate(pieces[(int)p], Vector3.zero, NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<Player>().playerColor ? Quaternion.identity : Quaternion.Euler(0, 0, 180), board[file, rank].transform);
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

    public Piece GetPieceFromSpace(int file, int rank) {
        GameObject _space = board[file, rank];
        if (_space.transform.childCount >= 1) return _space.transform.GetComponentInChildren<Piece>();
        return null;
    }

    public Piece GetPieceFromSpace(char file, int rank) {
        return GetPieceFromSpace(files.IndexOf(file), rank);
    }

    public Piece GetPieceFromSpace(Vector2Int position) {
        return GetPieceFromSpace(position.x, position.y);
    }
}
