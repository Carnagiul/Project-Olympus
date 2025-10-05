using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrap : MonoBehaviour
{
    [Tooltip("Host = crée la partie locale. Client = rejoint une partie distante. Pour un premier test : Host.")]
    public bool startAsHost = true;

    // Appelé par ton bouton "Start"
    public void StartGame()
    {
        var nm = NetworkManager.Singleton;
        if (!nm)
        {
            Debug.LogError("No NetworkManager in scene!");
            return;
        }

        // Evite double start
        if (nm.IsClient || nm.IsServer) return;

        if (startAsHost)
        {
            // Partie locale (server + client)
            if (!nm.StartHost())
                Debug.LogError("StartHost failed");
        }
        else
        {
            // Pour un vrai client distant, renseigne l'IP/port dans UnityTransport
            if (!nm.StartClient())
                Debug.LogError("StartClient failed");
        }
    }
}
