using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    [Header("Background References")]
    [SerializeField] private Transform bgA;
    [SerializeField] private Transform bgB;
    [SerializeField] private SpriteRenderer bgARenderer;
    [SerializeField] private SpriteRenderer bgBRenderer;
    [SerializeField] private Camera targetCamera;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 2.0f;
    [SerializeField] private bool scrollOnStart = true;

    [Header("Fit Settings")]
    [SerializeField] private bool fitToCameraOnStart = true;
    [SerializeField] private float scaleMultiplier = 1.02f;

    [Header("Loop Settings")]
    [SerializeField] private float seamOverlap = 0.05f;

    private bool isScrolling;

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning("BackgroundLooper: 카메라 참조가 없습니다.");
            enabled = false;
            return;
        }

        if (fitToCameraOnStart)
        {
            FitBackgroundToCamera(bgA, bgARenderer);
            FitBackgroundToCamera(bgB, bgBRenderer);
        }

        AlignBackgroundsToCamera();
        isScrolling = scrollOnStart;
    }

    private void LateUpdate()
    {
        if (!isScrolling)
            return;

        float moveAmount = scrollSpeed * Time.deltaTime;

        bgA.position += Vector3.down * moveAmount;
        bgB.position += Vector3.down * moveAmount;

        LoopIfNeeded(bgA, bgARenderer, bgB, bgBRenderer);
        LoopIfNeeded(bgB, bgBRenderer, bgA, bgARenderer);
    }

    private bool ValidateReferences()
    {
        if (bgA == null || bgB == null || bgARenderer == null || bgBRenderer == null)
        {
            Debug.LogWarning("BackgroundLooper: 배경 참조가 비어 있습니다.");
            return false;
        }

        return true;
    }

    private void FitBackgroundToCamera(Transform bg, SpriteRenderer sr)
    {
        if (bg == null || sr == null || sr.sprite == null || targetCamera == null)
            return;

        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        float scaleX = camWidth / spriteSize.x;
        float scaleY = camHeight / spriteSize.y;
        float finalScale = Mathf.Max(scaleX, scaleY) * scaleMultiplier;

        Vector3 localScale = bg.localScale;
        bg.localScale = new Vector3(finalScale, finalScale, localScale.z);
    }

    private void AlignBackgroundsToCamera()
    {
        float camX = targetCamera.transform.position.x;
        float camY = targetCamera.transform.position.y;

        float heightA = GetRendererHeight(bgARenderer);
        float heightB = GetRendererHeight(bgBRenderer);

        bgA.position = new Vector3(camX, camY, bgA.position.z);

        float bgBPosY = camY + (heightA * 0.5f) + (heightB * 0.5f) - seamOverlap;
        bgB.position = new Vector3(camX, bgBPosY, bgB.position.z);
    }

    private void LoopIfNeeded(Transform current, SpriteRenderer currentRenderer, Transform other, SpriteRenderer otherRenderer)
    {
        if (targetCamera == null || current == null || other == null || currentRenderer == null || otherRenderer == null)
            return;

        float camBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;

        float currentHeight = GetRendererHeight(currentRenderer);
        float otherHeight = GetRendererHeight(otherRenderer);

        float currentTop = current.position.y + currentHeight * 0.5f;

        if (currentTop <= camBottom + seamOverlap)
        {
            float otherTop = other.position.y + otherHeight * 0.5f;
            float newY = otherTop + currentHeight * 0.5f - seamOverlap;

            current.position = new Vector3(other.position.x, newY, current.position.z);
        }
    }

    private float GetRendererHeight(SpriteRenderer sr)
    {
        if (sr == null)
            return 0f;

        return sr.bounds.size.y;
    }

    public void SetScrolling(bool value)
    {
        isScrolling = value;
    }

    public void SetScrollSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }

    public void RefreshLayout()
    {
        if (!ValidateReferences())
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        if (fitToCameraOnStart)
        {
            FitBackgroundToCamera(bgA, bgARenderer);
            FitBackgroundToCamera(bgB, bgBRenderer);
        }

        AlignBackgroundsToCamera();
    }
}
