using UnityEngine;

public class CameraManager : MonoBehaviour {
    float targetRight = 7.5f;
    float targetLeft = -0.5f;
    float horizontalSize;
    void Start() {
        Camera main = GetComponent<Camera>();
        horizontalSize = main.aspect * main.orthographicSize;
        AdjustPositionForWhitePlayer(); //default adjust
    }

    public void AdjustPositionForWhitePlayer() {
        transform.position = new Vector3(transform.position.x + (targetRight - (transform.position.x + horizontalSize)), transform.position.y, transform.position.z);
    }

    public void AdjustPositionForBlackPlayer() {
        transform.SetPositionAndRotation(
            new Vector3(transform.position.x + (targetLeft - (transform.position.x - horizontalSize)), transform.position.y, transform.position.z), 
            Quaternion.Euler(0, 0, 180));
    }
}
