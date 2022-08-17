using UnityEngine;
using Unity.Netcode;
using TMPro;

public class LoginManager : MonoBehaviour {

    string mode = "not connected";
    public string username = "unnamed";
    public Player playerObject;

    public void StartHost() {
        username = transform.GetChild(0).GetComponent<TMP_InputField>().text;
        NetworkManager.Singleton.StartHost();
        mode = "host";
        LogConnectionParameters();
        PutChildrenToSleep();
    }

    public void StartClient() {
        username = transform.GetChild(0).GetComponent<TMP_InputField>().text;
        NetworkManager.Singleton.StartClient();
        mode = "client";
        LogConnectionParameters();
        PutChildrenToSleep();
    }

    private void LogConnectionParameters() {
        Debug.Log("Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        Debug.Log("Mode: " + mode);
    }

    private void PutChildrenToSleep() {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(false);
    }
}
