using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    [Header("Background References")]
    [SerializeField] private Transform bgA;
    [SerializeField] private Transform bgB;
    [SerializeField] private SpriteRenderer bgARenderer;
    [SerializeField] private SpriteRenderer bgBRenderer;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 2.0f;
    [SerializeField] private bool scrollOnStart = true;
    [SerializeField] private float extraGap = 0f;

    private float bgHeight;
    private bool isScrolling;

    private void Start()
    {
        if (bgA == null || bgB == null || bgARenderer == null || bgBRenderer == null)
        {
            Debug.LogWarning("BackgroundLooper: 배경 참조가 비어 있습니다.");
            enabled = false;
            return;
        }

        CalculateHeight();
        AlignBackgrounds();
        isScrolling = scrollOnStart;
    }

    private void Update()
    {
        if (!isScrolling)
            return;

        float moveAmount = scrollSpeed * Time.deltaTime;

        bgA.position += Vector3.down * moveAmount;
        bgB.position += Vector3.down * moveAmount;

        LoopIfNeeded(bgA, bgB);
        LoopIfNeeded(bgB, bgA);
    }

    private void CalculateHeight()
    {
        bgHeight = bgARenderer.bounds.size.y + extraGap;
    }

    private void AlignBackgrounds()
    {
        Vector3 posA = bgA.position;
        Vector3 posB = bgB.position;

        bgA.position = new Vector3(posA.x, 0f, posA.z);
        bgB.position = new Vector3(posA.x, bgHeight, posA.z);
    }

    private void LoopIfNeeded(Transform current, Transform other)
    {
        float lowerThreshold = -bgHeight;

        if (current.position.y <= lowerThreshold)
        {
            current.position = new Vector3(
                other.position.x,
                other.position.y + bgHeight,
                other.position.z
            );
        }
    }

    public void SetScrolling(bool value)
    {
        isScrolling = value;
    }

    public void SetScrollSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }
}
