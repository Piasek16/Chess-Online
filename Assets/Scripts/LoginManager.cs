using UnityEngine;
using Unity.Netcode;

public class LoginManager : MonoBehaviour
{
    void OnGUI() {
        GUILayout.Label("Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + (NetworkManager.Singleton.IsHost ? "Host" : "Not Host (Client obv)"));
    }

    public void StartHost() {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient() {
        NetworkManager.Singleton.StartClient();
    }
}
