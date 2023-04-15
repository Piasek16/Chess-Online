using UnityEngine;

public class GhostPawn : Pawn {
	private Pawn ghostTarget = null;

	public void InitGhost(Vector2Int pawnParentLocation) {
		ghostTarget = BoardManager.Instance.GetPieceFromSpace(pawnParentLocation).GetComponent<Pawn>();
		ID = ghostTarget.ID;
		Debug.Log("Summoned ghost pawn on " + Position + " with target on " + ghostTarget.Position);
	}

	public void ExecuteGhost() {
		if (ghostTarget == null) {
			Debug.LogError("Unable to execute ghost function - ghost target not set");
			return;
		}
		Debug.Log("Executing ghost function on " + Position);
		BoardManager.Instance.DestroyPiece(ghostTarget);
	}

	public void DisposeOfGhost() {
		if (ghostTarget == null)
			Debug.LogWarning("Disposing of ghost with no ghost target set");
		Debug.Log($"Disposed of ghost pawn on {Position}");
		ghostTarget = null;
		BoardManager.Instance.ReturnGhost(this);
	}
}
