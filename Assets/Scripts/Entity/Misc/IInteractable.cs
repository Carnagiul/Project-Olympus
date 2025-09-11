public interface IInteractable
{
    // Appelé quand le joueur interagit (E). Retourne true si l’interaction a réussi.
    bool Interact(FpsController interactor);
}
