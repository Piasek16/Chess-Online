using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using TMPro;

public class Player : NetworkBehaviour {
	public NetworkVariable<FixedString32Bytes> PlayerName = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public bool PlayerColor;
	public Color32 preMoveColorWhite;
	public Color32 preMoveColorBlack;

	Vector2 whitePosition = new(-2f, 1f);
	Vector2 blackPosition = new(-2f, 6f);
	TMP_Text usernameText;

	public override void OnNetworkSpawn() {
		Debug.Log("[Client] Spawned player for client ID: " + OwnerClientId);
	}

	public void SetupPlayerData(ulong whitePlayerID, ulong blackPlayerID) {
		Debug.Log("[Client] Setting up player data for client ID: " + OwnerClientId);
		Debug.Log("White player id: " + whitePlayerID);
		Debug.Log("Black player id: " + blackPlayerID);
		PlayerColor = OwnerClientId == whitePlayerID;
		Debug.Log("Player color set to: " + (PlayerColor ? "white" : "black"));
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
				Camera.main.GetComponent<CameraManager>().ApplyBlackPlayerViewFixes();
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
	Piece heldPiece = null; // (For display) Copy of the piece that is chosen to move by the player
	Piece movingPiece; // The piece that the player is choosing to move
	Vector2Int? arrowPointerBeginning = null;
	List<GameObject> pointerArrows = new();

	public void OnMyTurn() {
		// TODO: something something premoves
	}

	void Update() {
		if (!IsOwner) return;
		if (!GameSessionManager.Instance.GameStarted) return;
		mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		var roundedMousePos = new Vector2Int(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));
		if (Input.GetMouseButtonDown(0)) PickUpPiece(BoardManager.Instance.GetPieceFromSpace(roundedMousePos));
		if (Input.GetMouseButtonUp(0)) DropPieceAt(roundedMousePos);
		if (heldPiece != null) HoldPiece();
		if (Input.GetMouseButtonDown(1)) SetArrowPointerBeginning(roundedMousePos);
		if (Input.GetMouseButtonUp(1)) DrawArrowPointer(roundedMousePos);
	}

	void PickUpPiece(Piece piece) {
		if (piece == null || !BoardManager.Instance.IsPieceMyColor(piece) || piece is GhostPawn) return;
		DestroyHeldPiece();
		piece.HighlightPossibleMoves();
		heldPiece = Instantiate(piece, null); // copy happens before spirete renderer is disabled
		piece.GetComponent<SpriteRenderer>().enabled = false; // hide the original piece
		movingPiece = piece;
		RemovePointerArrows();
	}

	void HoldPiece() {
		heldPiece.transform.position = new Vector3(mousePos.x, mousePos.y, -0.3f);
	}

	void DropPieceAt(Vector2Int dropLocation) {
		if (heldPiece == null || movingPiece == null) {
			Debug.LogError("Cannot drop piece - held piece or moving piece is null!");
			return;
		}
		Move move = new(movingPiece.Position, dropLocation);
		movingPiece.ResetPossibleMovesHighlight();
		if (movingPiece.PossibleMoves.Contains(dropLocation)) {
			if (!GameSessionManager.Instance.MyTurn) {
				// TODO: Register pre move
			} else {
				BoardManager.Instance.ExecuteMove(move);
				GameSessionManager.Instance.RelayMoveToOtherPlayerServerRPC(move);
				GameSessionManager.Instance.EndMyTurn();
			}
		}
		DestroyHeldPiece();
	}

	void DestroyHeldPiece() {
		if (heldPiece != null)
			Destroy(heldPiece.gameObject);
		if (movingPiece != null)
			movingPiece.GetComponent<SpriteRenderer>().enabled = true;
		heldPiece = null;
		movingPiece = null;
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
}
