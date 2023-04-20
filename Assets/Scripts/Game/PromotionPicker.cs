using Unity.VisualScripting;
using UnityEngine;

public class PromotionPicker : MonoBehaviour {
	[SerializeField] Piece[] _whitePieces;
	[SerializeField] Piece[] _blackPieces;
	[SerializeField] Sprite _containerBackground;

	void Start() {
		if (GameSessionManager.Instance == null){
			Debug.LogError("Unable to bind promotion picker - GameSessionManager is null!");
			return;
		}
		if (GameSessionManager.Instance.LocalPlayer == null) {
			Debug.LogError("Unable to bind promotion picker - LocalPlayer is null!");
			return;
		}
		if (GameSessionManager.Instance.LocalPlayer.PlayerColor)
			GenerateFields(true);
		else
			GenerateFields(false);
	}

	void GenerateFields(bool generateWhite) {
		var piecesToInstantiate = generateWhite ? _whitePieces : _blackPieces;
		for (int i = 0; i < piecesToInstantiate.Length; i++) {
			var container = CreateContainer(i, piecesToInstantiate[i]);
			CreatePiece(piecesToInstantiate[i], container);
		}
	}

	Transform CreateContainer(int index, Piece piece) {
		GameObject container = new($"{piece.name} container");
		container.transform.SetParent(transform, false);
		container.transform.localPosition = new Vector3(0, -index, 0);
		var spriteRenderer = container.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = _containerBackground;
		spriteRenderer.sortingOrder = 4;
		return container.transform;
	}

	void CreatePiece(Piece pieceToInstantiate, Transform container) {
		Piece piece = Instantiate(pieceToInstantiate, container);
		piece.AddComponent<BoxCollider2D>();
		piece.AddComponent<PromotionTarget>().picker = this;
		piece.GetComponent<SpriteRenderer>().sortingOrder = 5;
	}

	public void SelectPiece(PieceType pieceType) {
		Debug.Log("[Piece Promotion] Player selected piece: " + pieceType);
		GameSessionManager.Instance.LocalPlayer.CompletePromotion(pieceType);
		Destroy(gameObject);
	}
}

public class PromotionTarget : MonoBehaviour {

	public PromotionPicker picker;

	void OnMouseDown() {
		var myType = GetComponent<Piece>().Type;
		picker.SelectPiece(myType);
	}
}