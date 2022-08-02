using UnityEngine;

public class Piece : MonoBehaviour {

    public int ID;

    public void GetValidMoves() {
        var moves = MoveManager.Instance.GetMovesForward(new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y)), 4);
        foreach (var move in moves) {
            //var c = BoardManager.Instance.highlightColor;
            //var col = BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.color;
            //BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(col.r + c.r, col.g + c.g, col.b + c.b, col.a + c.a));
            //BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.color += new Color32(3, 0, 0, 1);

            BoardManager.Instance.board[move.x, move.y].GetComponent<MeshRenderer>().material.color = BoardManager.Instance.highlightColor;
        }
    }
}
