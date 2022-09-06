using ParrelSync;
using Unity.Netcode;
using UnityEngine;

public class GameTestingManager : MonoBehaviour {
#if UNITY_EDITOR
    public static GameTestingManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this;
    }

    string testingOriginalUsername = "PiasekHost";
    string testingCloneUsername = "PiasekClient";
    public string TestingUsername;

    void Start() {
        if (ClonesManager.IsClone()) {
            TestingUsername = testingCloneUsername;
            NetworkManager.Singleton.StartClient();
        } else {
            TestingUsername = testingOriginalUsername;
            NetworkManager.Singleton.StartHost();
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            GameSessionManager.Instance.InitializeTestGame();
        }
    }
#endif
}
