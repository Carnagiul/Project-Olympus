using UnityEngine;
using Unity.Netcode;

public class GracefulShutdown : MonoBehaviour
{
    void OnEnable() => Application.quitting += OnQuit;
    void OnDisable() => Application.quitting -= OnQuit;

    private void OnQuit()
    {
        Debug.Log("[SERVER] Arrêt en cours…");
        var nm = NetworkManager.Singleton;
        if (nm)
        {
            if (nm.IsServer) nm.Shutdown();
        }
        // TODO: Sauvegardes, flush logs, etc.
    }
}
