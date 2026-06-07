using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapeToStartScenePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject escapePanel;
    [SerializeField] private Button returnToStartButton;
    [SerializeField] private Button closeButton;

    [Header("Scene")]
    [SerializeField] private string startSceneName = "StartScene";

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
    [SerializeField] private bool hideOnStart = true;

    [Header("Options")]
    [SerializeField] private bool pauseWhenOpen = true;
    [SerializeField] private bool resetRunWhenReturnToStart = false;
    [SerializeField] private bool keepPanelOnTop = true;

    private bool isOpen;

    private void Awake()
    {
        if (returnToStartButton != null)
        {
            returnToStartButton.onClick.RemoveAllListeners();
            returnToStartButton.onClick.AddListener(ReturnToStartScene);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void Start()
    {
        if (escapePanel != null && hideOnStart)
            escapePanel.SetActive(false);

        isOpen = escapePanel != null && escapePanel.activeSelf;
        if (!isOpen)
            RestoreTimeScale();
    }

    // ESC 입력을 감지해 시작화면 복귀 패널을 열고 닫는다.
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen)
                ClosePanel();
            else
                OpenPanel();
        }
    }

    private void LateUpdate()
    {
        if (!keepPanelOnTop || !isOpen || escapePanel == null)
            return;

        if (escapePanel.transform.parent != null)
            escapePanel.transform.SetAsLastSibling();
    }

    // 패널을 최상단에 표시하고 필요하면 게임 시간을 멈춘다.
    public void OpenPanel()
    {
        isOpen = true;

        if (escapePanel != null)
        {
            escapePanel.SetActive(true);
            if (escapePanel.transform.parent != null)
                escapePanel.transform.SetAsLastSibling();
        }

        if (pauseWhenOpen)
            Time.timeScale = 0f;
    }

    public void ClosePanel()
    {
        isOpen = false;

        if (escapePanel != null)
            escapePanel.SetActive(false);

        RestoreTimeScale();
    }

    // 현재 런을 정리한 뒤 시작 씬으로 복귀할 때 호출된다.
    public void ReturnToStartScene()
    {
        RestoreTimeScale();

        if (resetRunWhenReturnToStart)
            RunContext.ResetForNewRun();

        SceneManager.LoadScene(startSceneName);
    }

    private void RestoreTimeScale()
    {
        if (pauseWhenOpen)
            Time.timeScale = 1f;
    }
}
