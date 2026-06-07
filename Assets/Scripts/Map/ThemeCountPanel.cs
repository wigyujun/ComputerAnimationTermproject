using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ThemeCountPanel : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "SampleScene";
    [SerializeField] private GameObject panelRoot;

    [Header("Theme Count Texts")]
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private TMP_Text forestText;
    [SerializeField] private TMP_Text skyText;
    [SerializeField] private TMP_Text seaText;
    [SerializeField] private TMP_Text mostVisitText;

    [SerializeField] private bool refreshEveryFrame = true;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        bool isMapScene = SceneManager.GetActiveScene().name == mapSceneName;

        if (panelRoot != null && panelRoot.activeSelf != isMapScene)
            panelRoot.SetActive(isMapScene);

        if (!isMapScene)
            return;

        if (refreshEveryFrame)
            RefreshUI();
    }

    public void RefreshUI()
    {
        if (floorText != null)
            floorText.text = $"현재 층 : {RunContext.CurrentFloor}층";

        if (forestText != null)
            forestText.text = $"숲 : {RunContext.ForestVisitCount}회";

        if (skyText != null)
            skyText.text = $"하늘 : {RunContext.SkyVisitCount}회";

        if (seaText != null)
            seaText.text = $"바다 : {RunContext.SeaVisitCount}회";

        if (mostVisitText != null)
            mostVisitText.text = $"최다 진입 테마 : {RunContext.GetMostVisitedThemesLabel()}";
    }
}
