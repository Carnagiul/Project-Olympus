using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class NexusController : EntityController
{
    [SerializeField] public int NexusLevel = 1;

    [Header("Nexus")]
    public UnityEvent OnNexusDestroyed; // pour brancher FX/son
    public UnityEvent<int> OnNexusLevelChanged; // (current, max)

    public Team Team;

    protected override void Awake()
    {
        base.Awake();
        OnNexusLevelChanged ??= new UnityEvent<int>();
        OnNexusLevelChanged.Invoke(NexusLevel);
    }

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
