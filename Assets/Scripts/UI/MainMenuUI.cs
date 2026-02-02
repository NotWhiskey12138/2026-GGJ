using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Level-Tutorial");
    }

    public void OpenSettings()
    {
        Debug.Log("Open Settings");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
