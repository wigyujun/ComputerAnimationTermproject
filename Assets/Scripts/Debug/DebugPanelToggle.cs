using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DebugPanelToggle : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private GameObject openButtonObject;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.N;
    [SerializeField] private bool hidePanelOnStart = true;

    private void Start()
    {
        if (debugPanel != null && hidePanelOnStart)
            debugPanel.SetActive(false);

        RefreshUI();
    }

    private void Update()
    {
        if (WasTogglePressed())
            TogglePanel();
    }

    private void LateUpdate()
    {
        RefreshUI();
    }

    private bool WasTogglePressed()
    {
        if (Input.GetKeyDown(toggleKey))
            return true;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        switch (toggleKey)
        {
            case KeyCode.N:
                return Keyboard.current.nKey.wasPressedThisFrame;
            case KeyCode.F1:
                return Keyboard.current.f1Key.wasPressedThisFrame;
            case KeyCode.F2:
                return Keyboard.current.f2Key.wasPressedThisFrame;
            case KeyCode.F3:
                return Keyboard.current.f3Key.wasPressedThisFrame;
        }
#endif

        return false;
    }

    public void TogglePanel()
    {
        if (debugPanel == null)
            return;

        debugPanel.SetActive(!debugPanel.activeSelf);
        RefreshUI();
    }

    public void OpenPanel()
    {
        if (debugPanel == null)
            return;

        debugPanel.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (debugPanel == null)
            return;

        debugPanel.SetActive(false);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (openButtonObject != null)
            openButtonObject.SetActive(debugPanel == null || !debugPanel.activeSelf);
    }
}
