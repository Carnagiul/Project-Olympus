using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class NexusController : EntityController
{
    [Header("Nexus")]
    public UnityEvent OnNexusDestroyed; // pour brancher FX/son

    protected override void OnKilled()
    {
        // Appelle les hooks/FX �ventuels
        OnNexusDestroyed?.Invoke();

        // D�clenche le Game Over (sans respawn du joueur)
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver("Nexus d�truit");

        // On peut d�sactiver visuellement le Nexus (au lieu de Destroy si tu veux le laisser dans la sc�ne)
        gameObject.SetActive(false);
    }
}
