using System.Globalization;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerNet : NetworkBehaviour
{
    [Header("Refs (auto si vide)")]
    public FpsController fps;               // ton contrôleur existant
    public Camera playerCamera;             // caméra FPS enfant
    public AudioListener audioListener;     // audio listener de la cam

    void Awake()
    {
        if (!fps) fps = GetComponent<FpsController>();
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>(true);
        if (!audioListener) audioListener = GetComponentInChildren<AudioListener>(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        bool mine = IsOwner;

        // Activer les éléments locaux uniquement pour le propriétaire
        if (playerCamera) playerCamera.enabled = mine;
        if (audioListener) audioListener.enabled = mine;
        if (fps) fps.enabled = mine; // tes contrôles restent 100% locaux

        if (mine)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        base.OnNetworkDespawn();
    }
}
