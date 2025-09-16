using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game"; // nom de la scène du jeu

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        Debug.Log("Options menu not implemented yet!");
        // Tu pourras afficher un panel ou charger une autre scène plus tard
    }

    public void OpenCredits()
    {
        Debug.Log("Credits menu not implemented yet!");
        // Idem, tu peux afficher un panel UI avec du texte
    }

    public void QuitGame()
    {
        Debug.Log("Quit game");
        Application.Quit();
    }
}
