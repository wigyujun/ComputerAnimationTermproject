using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "SampleScene";

    public void StartGame()
    {
        Time.timeScale = 1f;
        RunContext.ResetForNewRun();
        SceneManager.LoadScene(mapSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
