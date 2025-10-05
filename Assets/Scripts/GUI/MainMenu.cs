using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Buttons")]
    [SerializeField] private Button startGame;
    [SerializeField] private Button joinGame;
    [SerializeField] private Button options;
    [SerializeField] private Button credits;
    [SerializeField] private Button quitGame;

    private bool _isBusy;

    private void Start()
    {
        if (startGame) startGame.onClick.AddListener(StartAsHostAndLoadGame);
        if (joinGame) joinGame.onClick.AddListener(StartAsClient);
        if (options) options.onClick.AddListener(OpenOptions);
        if (credits) credits.onClick.AddListener(OpenCredits);
        if (quitGame) quitGame.onClick.AddListener(QuitGame);

        SetUIInteractable(true);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEventLog;
    }

    private void Awake()
    {
        // Si un NetworkManager existe déjà et tourne, ne relancez rien depuis ce MainMenu.
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
        {
            Debug.LogWarning("[MainMenu] Réseau déjà en cours; ce MainMenu ne lancera rien.");
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (startGame) startGame.onClick.RemoveListener(StartAsHostAndLoadGame);
        if (joinGame) joinGame.onClick.RemoveListener(StartAsClient);
        if (options) options.onClick.RemoveListener(OpenOptions);
        if (credits) credits.onClick.RemoveListener(OpenCredits);
        if (quitGame) quitGame.onClick.RemoveListener(QuitGame);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEventLog;
    }

    private void OnSceneEventLog(SceneEvent e)
    {
        Debug.Log($"[SceneEvent] Type={e.SceneEventType} | " +
                  $"OriginIsServer={e.ClientId == NetworkManager.ServerClientId} | " +
                  $"Scene={e.SceneName} | " +
                  $"Clients={string.Join(",", e.ClientsThatCompleted?.Count ?? 0)}");
    }

    // ---- Host flow ----
    public void StartAsHostAndLoadGame()
    {
        if (_isBusy) return;
        if (!EnsureNetworkManager()) return;

        // Avoid double-start
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("Network is already running. Loading game scene as server (if host)...");
            if (NetworkManager.Singleton.IsServer)
                LoadGameSceneAsServer();
            return;
        }

        SetUIInteractable(false);
        _isBusy = true;

        // Optional: subscribe to server started to know it's ready
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;

        if (!NetworkManager.Singleton.StartHost())
        {
            Debug.LogError("Failed to start Host!");
            CleanupServerStartSubscriptions();
            _isBusy = false;
            SetUIInteractable(true);
            return;
        }

        // If StartHost succeeds immediately, OnServerStarted will fire next tick.
    }

    private void OnServerStarted()
    {
        CleanupServerStartSubscriptions();
        Debug.Log("Host started. Loading networked game scene...");
        LoadGameSceneAsServer();
        _isBusy = false;
        // UI can stay disabled if you unload/destroy the menu in the next scene
    }

    private void CleanupServerStartSubscriptions()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    private void LoadGameSceneAsServer()
    {
        var sceneManager = NetworkManager.Singleton.SceneManager;
        if (sceneManager == null)
        {
            Debug.LogError("NetworkSceneManager is null. Cannot load network scene.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"Scene '{gameSceneName}' is not added to Build Settings or cannot be loaded.");
            return;
        }

        // Appel direct, NGO gère la synchronisation côté clients
        sceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }


    // ---- Client flow ----
    public void StartAsClient()
    {
        if (_isBusy) return;
        if (!EnsureNetworkManager()) return;

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Network is already running; not starting a second client/server.");
            return;
        }

        SetUIInteractable(false);
        _isBusy = true;

        // Optional: subscribe to connection callbacks if you want to react to success/failure
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start Client!");
            CleanupClientCallbacks();
            _isBusy = false;
            SetUIInteractable(true);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Connected to host.");
            CleanupClientCallbacks();
            _isBusy = false;
            // UI can remain disabled if you expect the host to switch scenes for you
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("Disconnected from host.");
            CleanupClientCallbacks();
            _isBusy = false;
            SetUIInteractable(true);
        }
    }

    private void CleanupClientCallbacks()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // ---- Misc ----
    public void OpenOptions()
    {
        Debug.Log("Options menu not implemented yet!");
        // e.g., open a panel instead of Debug.Log
    }

    public void OpenCredits()
    {
        Debug.Log("Credits menu not implemented yet!");
    }

    public void QuitGame()
    {
        Debug.Log("Quit game");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---- Helpers ----
    private bool EnsureNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager.Singleton found in the scene. Add a NetworkManager (with a transport, e.g., UnityTransport).");
            return false;
        }
        return true;
    }

    private void SetUIInteractable(bool interactable)
    {
        if (startGame) startGame.interactable = interactable;
        if (joinGame) joinGame.interactable = interactable;
        if (options) options.interactable = interactable;
        if (credits) credits.interactable = interactable;
        if (quitGame) quitGame.interactable = interactable;
    }
}
