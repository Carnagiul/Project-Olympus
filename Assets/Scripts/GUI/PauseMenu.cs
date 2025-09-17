using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Gameplay (facultatif)")]
    [Tooltip("Composants à désactiver quand le jeu est en pause (ex: contrôles joueur).")]
    [SerializeField] private Behaviour[] disableWhilePaused;

    public static bool IsPaused { get; private set; }
    public static event Action<bool> OnPauseChanged; // true = paused

    private void Awake()
    {
        if (pausePanel) pausePanel.SetActive(false);
        EnsureUnpaused(); // sécurité si on revient d'une autre scène
    }

    private void OnDestroy()
    {
        // Si on détruit le menu (changement de scène), on remet tout propre
        EnsureUnpaused();
    }

    private void Update()
    {
        if (WasEscapePressedThisFrame())
            TogglePause();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        if (pausePanel) pausePanel.SetActive(true);

        // Temps / Audio
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Curseur
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Désactiver contrôles gameplay si fournis
        SetDisabled(disableWhilePaused, true);

        OnPauseChanged?.Invoke(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        if (pausePanel) pausePanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Si ton FPS lock la souris en jeu, tu peux relocker ici :
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SetDisabled(disableWhilePaused, false);

        OnPauseChanged?.Invoke(false);
    }

    public void RestartLevel()
    {
        EnsureUnpaused();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        EnsureUnpaused();
        SceneManager.LoadScene("MainMenu"); // adapte au nom exact
    }

    public void QuitGame()
    {
        EnsureUnpaused();
        Application.Quit();
    }

    private void EnsureUnpaused()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetDisabled(disableWhilePaused, false);
        IsPaused = false;
        if (pausePanel) pausePanel.SetActive(false);
    }

    private static void SetDisabled(Behaviour[] list, bool disabled)
    {
        if (list == null) return;
        foreach (var b in list)
            if (b) b.enabled = !disabled;
    }

    private static bool WasEscapePressedThisFrame()
    {
        // Compatible New Input System OU ancien
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
