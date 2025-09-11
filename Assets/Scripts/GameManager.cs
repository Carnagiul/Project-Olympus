using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs (optionnelles)")]
    public FpsController player;          // tu peux l’assigner dans l’Inspector
    public Canvas gameOverCanvas;         // UI à afficher
    public bool pauseTimeOnGameOver = true;

    [Tooltip("Composants à désactiver au Game Over (si vide ? auto détection minimale)")]
    public List<Behaviour> disableOnGameOver = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gameOverCanvas) gameOverCanvas.enabled = false;
    }

    public void GameOver(string reason = "")
    {
        // 1) UI
        if (gameOverCanvas) gameOverCanvas.enabled = true;

        // 2) Désactiver les contrôles joueur
        if (disableOnGameOver == null || disableOnGameOver.Count == 0)
            AutoCollectOwnerBehaviours();

        foreach (var b in disableOnGameOver)
            if (b) b.enabled = false;

        var cc = player ? player.GetComponent<CharacterController>() : null;
        if (cc) cc.enabled = false;

        // 3) Curseur + pause
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseTimeOnGameOver) Time.timeScale = 0f;

        // (Optionnel) log
        if (!string.IsNullOrEmpty(reason)) Debug.Log($"[GameOver] {reason}");
    }

    void AutoCollectOwnerBehaviours()
    {
        if (!player) player = FindObjectOfType<FpsController>();
        if (!player) return;

        // Minimal : désactive le contrôleur + caméra + audio
        var look = player.GetComponentInChildren<FpsLook>(true);
        var fx = player.GetComponentInChildren<FpsCameraEffects>(true);
        var sfx = player.GetComponentInChildren<FpsAudio>(true);

        disableOnGameOver = new List<Behaviour>();
        disableOnGameOver.Add(player);
        if (look) disableOnGameOver.Add(look);
        if (fx) disableOnGameOver.Add(fx);
        if (sfx) disableOnGameOver.Add(sfx);
    }

    // (Optionnel) pour relancer la partie
    public void RestartLevel()
    {
        if (pauseTimeOnGameOver) Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
