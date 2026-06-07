using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "SampleScene";

    // 새 게임 시작 시 런 데이터를 초기화하고 맵 씬으로 진입한다.
    public void StartGame()
    {
        Time.timeScale = 1f;
        RunContext.ResetForNewRun();
        SceneManager.LoadScene(mapSceneName);
    }

    // 빌드된 게임 종료 버튼에서 호출되는 메서드다.
    public void QuitGame()
    {
        Application.Quit();
    }
}
