using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine.SceneManagement;

public class LoginSessionManager : MonoBehaviour {
    public static LoginSessionManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this) Destroy(this); else { Instance = this; DontDestroyOnLoad(gameObject); };
    }

    public string Username { get; private set; } = "Chess Player";
    public string IP { get; private set; } = "127.0.0.1";

    public Player playerObject;
    [SerializeField] private Canvas usernameCanvas;
    [SerializeField] private Canvas connectionCanvas;
    private UNetTransport uNetTransport;

    void Start() {
        //SceneManager.sceneLoaded += OnSceneLoaded;

        //Subscribed later in start host

        uNetTransport = NetworkManager.Singleton.GetComponent<UNetTransport>();
        uNetTransport.ConnectAddress = "127.0.0.1"; //Temporary local address
        uNetTransport.ConnectPort = 25565; //Temporary port
        uNetTransport.ServerListenPort = 25565; //Temporary port
    }

    private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode) {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        Debug.Log("Loaded scene name " + sceneName);
        if (NetworkManager.Singleton.IsServer) {
            var player = Instantiate(playerObject);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void PassUsername(string username) {
        Username = username;
    }

    public void AdvanceLoginCanvas() {
        usernameCanvas.gameObject.SetActive(false);
        connectionCanvas.gameObject.SetActive(true);
    }

    public void PassIP(string ip) {
        uNetTransport.ConnectAddress = ip;
    }

    public void ConnectClient() {
        if (NetworkManager.Singleton.StartClient()) Debug.Log("d");
            //NetworkManager.Singleton.SceneManager.LoadScene("ClassicMode", LoadSceneMode.Single);
        //SceneManager.LoadScene(1);
    }

    public void StartHost() {
        if (NetworkManager.Singleton.StartHost()) {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
            NetworkManager.Singleton.SceneManager.LoadScene("ClassicMode", LoadSceneMode.Single);
        }
        //SceneManager.LoadScene(1);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.buildIndex == 1) {
            Debug.Log("Loaded scene name " + scene.name);
            if (NetworkManager.Singleton.IsServer) {
                var player = Instantiate(playerObject);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
            }
        }
    }
}
