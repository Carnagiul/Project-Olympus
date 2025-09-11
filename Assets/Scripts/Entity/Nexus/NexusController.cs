using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class NexusController : EntityController
{
    [Header("Nexus")]
    public UnityEvent OnNexusDestroyed; // pour brancher FX/son

    protected override void OnKilled()
    {
        // Appelle les hooks/FX éventuels
        OnNexusDestroyed?.Invoke();

        // Déclenche le Game Over (sans respawn du joueur)
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver("Nexus détruit");

        // On peut désactiver visuellement le Nexus (au lieu de Destroy si tu veux le laisser dans la scène)
        gameObject.SetActive(false);
    }
}
