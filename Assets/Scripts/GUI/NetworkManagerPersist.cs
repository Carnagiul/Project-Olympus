using Unity.Netcode;
using UnityEngine;

public class NetworkManagerPersist : MonoBehaviour
{
    void Awake()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.gameObject == gameObject)
            DontDestroyOnLoad(gameObject);
        else
            Destroy(gameObject); // évite doublons si tu relances depuis l’éditeur
    }
}
