using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Unity.Netcode; // <— IMPORTANT

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs (optionnelles)")]
    public Canvas gameOverCanvas;         // UI à afficher
    public bool pauseTimeOnGameOver = true;

    [Tooltip("Composants à désactiver au Game Over (si vide ? auto détection minimale)")]
    public List<Behaviour> disableOnGameOver = new();

    [SerializeField]
    private FpsController player;

    // ---- NEW: Networker spawning ----
    [Header("Networking")]
    [Tooltip("Prefab Networker (doit contenir un NetworkObject) et être listé dans NetworkManager > NetworkPrefabs")]
    [SerializeField] private NetworkObject networkerPrefab;

    // Pour éviter les doublons si on ré-entre dans la scène, on mémorise pour qui on a déjà spawn
    private readonly HashSet<ulong> _spawnedFor = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gameOverCanvas) gameOverCanvas.enabled = false;

        if (!player)
            player = FindFirstObjectByType<FpsController>();

        // ---- NEW: souscrire aux callbacks NGO ----
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

            // Si on est déjà serveur ici (ex: host) et que des clients sont connectés
            // (y compris l’host lui-même), assure qu’ils ont leur Networker.
            if (NetworkManager.Singleton.IsServer)
            {
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    EnsureNetworkerFor(clientId);
                }
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] NetworkManager.Singleton introuvable lors de l'Awake. " +
                             "Le spawn Networker sera inactif tant qu'il n'existe pas.");
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    // ---- NEW: callback de connexion client (host ou client distant) ----
    private void HandleClientConnected(ulong clientId)
    {
        // On ne spawn que depuis le serveur/host
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        EnsureNetworkerFor(clientId);
    }

    // ---- NEW: crée/spawn le prefab Networker pour un client donné, sans doublon ----
    private void EnsureNetworkerFor(ulong clientId)
    {
        if (networkerPrefab == null)
        {
            Debug.LogError("[GameManager] Networker prefab non assigné !");
            return;
        }

        if (_spawnedFor.Contains(clientId))
            return;

        // Instantie côté serveur
        var instance = Instantiate(networkerPrefab);
        var no = instance.GetComponent<NetworkObject>();
        if (no == null)
        {
            Debug.LogError("[GameManager] Le prefab Networker n'a pas de NetworkObject !");
            Destroy(instance.gameObject);
            return;
        }

        // Deux options :
        // 1) Si c'est l'objet joueur "principal", utilise SpawnAsPlayerObject:
        // no.SpawnAsPlayerObject(clientId, true);

        // 2) Sinon, simple objet réseau possédé par le client:
        no.SpawnWithOwnership(clientId, true);

        _spawnedFor.Add(clientId);
        Debug.Log($"[GameManager] Networker spawné pour client {clientId}.");
    }

    // ------------------ EXISTANT ------------------

    public void GameOver(string reason = "")
    {
        if (gameOverCanvas) gameOverCanvas.enabled = true;

        if (disableOnGameOver == null || disableOnGameOver.Count == 0)
            AutoCollectOwnerBehaviours();

        foreach (var b in disableOnGameOver)
            if (b) b.enabled = false;

        var cc = player ? player.GetComponent<CharacterController>() : null;
        if (cc) cc.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseTimeOnGameOver) Time.timeScale = 0f;

        if (!string.IsNullOrEmpty(reason)) Debug.Log($"[GameOver] {reason}");
    }

    void AutoCollectOwnerBehaviours()
    {
        var look = player.GetComponentInChildren<FpsLook>(true);
        var fx = player.GetComponentInChildren<FpsCameraEffects>(true);
        var sfx = player.GetComponentInChildren<FpsAudio>(true);

        disableOnGameOver = new List<Behaviour>();
        disableOnGameOver.Add(player);
        if (look) disableOnGameOver.Add(look);
        if (fx) disableOnGameOver.Add(fx);
        if (sfx) disableOnGameOver.Add(sfx);
    }

    public void RestartLevel()
    {
        if (pauseTimeOnGameOver) Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
