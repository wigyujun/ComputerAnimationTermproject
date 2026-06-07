using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearSceneController : MonoBehaviour
{
    [SerializeField] private string startSceneName = "StartScene";

    public void GoToStartScene()
    {
        Time.timeScale = 1f;
        RunContext.ResetForNewRun();
        SceneManager.LoadScene(startSceneName);
    }
}
