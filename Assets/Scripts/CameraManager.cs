using UnityEngine;

public class CameraManager : MonoBehaviour {
    Camera main;
    float targetRight = 7.5f;
    void Start() {
        main = GetComponent<Camera>();
        var maxRight = main.aspect * main.orthographicSize;
        Debug.Log("target: " + targetRight);
        Debug.Log(maxRight);
        Debug.Log(targetRight - maxRight);
        transform.position = new Vector3(transform.position.x + (targetRight - maxRight - transform.position.x), transform.position.y, transform.position.z);
    }
}
