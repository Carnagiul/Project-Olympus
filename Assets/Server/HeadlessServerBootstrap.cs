using System;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[DisallowMultipleComponent]
public class HeadlessServerBootstrap : MonoBehaviour
{
    [Header("Defaults")]
    public ushort defaultPort = 7777;
    public int defaultMaxPlayers = 16;
    public string defaultMapSceneName = "OutdoorsScene";

    [Header("Optional")]
    public bool autoQuitOnFatal = true;

    void Awake()
    {
        // Désactive toute caméra par sécurité
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            cam.enabled = false;

        Application.targetFrameRate = 60;        // suffisant pour un serveur de jeu
        QualitySettings.vSyncCount = 0;
        Application.focusChanged += OnFocusChanged;

        Application.logMessageReceived += (condition, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error)
                Debug.unityLogger.Log("SERVER", condition);
        };
    }

    void Start()
    {
        // Démarrage auto si on est en dedicated/headless
        if (IsHeadless())
            StartServerFromArgs();
        else
            Debug.Log("[SERVER] Non-headless: bootstrap en attente (mode éditeur ou client).");
    }

    private bool IsHeadless()
    {
#if UNITY_SERVER || DEDICATED_SERVER
        return true;
#else
        return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null
               || Application.isBatchMode;
#endif
    }

    private void StartServerFromArgs()
    {
        var args = Environment.GetCommandLineArgs();
        ushort port = GetArgUShort(args, "--port", defaultPort);
        int maxPlayers = GetArgInt(args, "--maxplayers", defaultMaxPlayers);
        string map = GetArgString(args, "--map", defaultMapSceneName);

        var nm = FindFirstObjectByType<NetworkManager>();
        if (!nm)
        {
            Debug.LogError("[SERVER] NetworkManager introuvable dans la scène.");
            SafeQuit(2);
            return;
        }

        var utp = nm.NetworkConfig.NetworkTransport as UnityTransport;
        if (!utp)
        {
            Debug.LogError("[SERVER] UnityTransport manquant sur le NetworkManager.");
            SafeQuit(3);
            return;
        }

        // Configure le port
        utp.SetConnectionData("0.0.0.0", port);

        // Optionnel : limite joueurs via ConnectionApproval
        nm.ConnectionApprovalCallback = (request, response) =>
        {
            // Exemple simple : vérifie la capacité max
            response.Approved = (nm.ConnectedClientsIds.Count < maxPlayers);
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = null;
        };

        // Hooks cycle de vie
        nm.OnServerStarted += () => Debug.Log($"[SERVER] Démarré. Port={port}, MaxPlayers={maxPlayers}, Map={map}");
        nm.OnClientConnectedCallback += id => Debug.Log($"[SERVER] Client #{id} connecté. ({nm.ConnectedClientsIds.Count}/{maxPlayers})");
        nm.OnClientDisconnectCallback += id => Debug.Log($"[SERVER] Client #{id} déconnecté.");

        // Charge la map serveur si différente
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(map) && map != active)
        {
            // Scene management NGO (serveur)
            nm.SceneManager.LoadScene(map, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        // Lancement serveur
        if (!nm.StartServer())
        {
            Debug.LogError("[SERVER] Échec du StartServer().");
            SafeQuit(4);
            return;
        }

        // Santé périodique
        InvokeRepeating(nameof(Heartbeat), 5f, 30f);
    }

    private void Heartbeat()
    {
        var nm = NetworkManager.Singleton;
        if (!nm || !nm.IsServer)
        {
            Debug.LogError("[SERVER] NetworkManager non disponible ou pas serveur. Arrêt.");
            SafeQuit(5);
            return;
        }

        // Ici tu peux émettre des métriques, nettoyer des entités, etc.
        Debug.Log($"[SERVER] HB - Clients: {nm.ConnectedClientsIds.Count}");
    }

    private static ushort GetArgUShort(string[] args, string key, ushort def)
        => (ushort)Mathf.Clamp(GetArgInt(args, key, def), 0, 65535);

    private static int GetArgInt(string[] args, string key, int def)
    {
        int i = Array.IndexOf(args, key);
        if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var val)) return val;
        return def;
    }

    private static string GetArgString(string[] args, string key, string def)
    {
        int i = Array.IndexOf(args, key);
        if (i >= 0 && i + 1 < args.Length) return args[i + 1];
        return def;
    }

    private void SafeQuit(int code)
    {
        if (!autoQuitOnFatal) return;
#if UNITY_EDITOR
        Debug.LogWarning($"[SERVER] Quit demandé (code {code}) ignoré en Éditeur.");
#else
        Application.Quit(code);
#endif
    }

    private void OnFocusChanged(bool hasFocus)
    {
        // Un serveur n’a pas besoin du focus, mais on log au cas où
        if (!hasFocus) Debug.Log("[SERVER] Focus perdu (ok pour headless).");
    }
}
