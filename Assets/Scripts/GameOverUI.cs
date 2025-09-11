using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public void OnClickRestart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
