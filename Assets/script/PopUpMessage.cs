using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PopUpMessage : MonoBehaviour
{
    [Header("Animation Settings")]
    public float fadeInDuration = 0.3f;
    public float displayDuration = 2f;
    public float fadeOutDuration = 0.5f;

    [Header("Scale Animation (Optional)")]
    public bool useScaleAnimation = true;
    public Vector3 startScale = Vector3.zero;
    public Vector3 targetScale = Vector3.one;

    [Header("Movement Animation (Optional)")]
    public bool useMoveAnimation = false;
    public Vector3 moveOffset = Vector3.up * 50f;

    public Text textComponent;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    public bool isAnimating = false;
    public bool isEnable = true;

    void Awake()
    {
        // Get or add CanvasGroup component for alpha control
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        // Start invisible
        canvasGroup.alpha = 0f;
        if (useScaleAnimation)
        {
            transform.localScale = startScale;
        }
    }

    void Start()
    {
        // Auto-start the popup animation
        //ShowPopUp();
        //showPopUpForever("test");
    }

    public void ShowPopUp()
    {
        if (isAnimating) return;

        gameObject.SetActive(true);
        StartCoroutine(PopUpSequence());
    }

    public void ShowPopUpForever()
    {
        if (isAnimating) return;

        gameObject.SetActive(true);
        StartCoroutine(PopUpForever());
    }

    public void HidePopUp()
    {
        if (!isAnimating) return;
        StartCoroutine(FadeOut());
        //isAnimating = false;
    }

    private IEnumerator PopUpSequence()
    {
        isAnimating = true;

        // Fade in and scale up
        yield return StartCoroutine(FadeIn());

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        yield return StartCoroutine(FadeOut());

        isAnimating = false;

    }

    private IEnumerator PopUpForever()
    {
        isAnimating = true;

        // Fade in and scale up
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Vector3 startPos = originalPosition;

        if (useMoveAnimation)
        {
            startPos = originalPosition - moveOffset;
            rectTransform.anchoredPosition = startPos;
        }

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeInDuration;

            // Smooth step for better easing
            t = t * t * (3f - 2f * t);

            // Fade in alpha
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // Scale animation
            if (useScaleAnimation)
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            }

            // Move animation
            if (useMoveAnimation)
            {
                rectTransform.anchoredPosition = Vector3.Lerp(startPos, originalPosition, t);
            }

            yield return null;
        }

        // Ensure final values
        canvasGroup.alpha = 1f;
        if (useScaleAnimation)
        {
            transform.localScale = targetScale;
        }
        if (useMoveAnimation)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        Vector3 startPos = rectTransform.anchoredPosition;
        Vector3 endPos = startPos + (useMoveAnimation ? moveOffset : Vector3.zero);

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeOutDuration;

            // Smooth step for better easing
            t = t * t * (3f - 2f * t);

            // Fade out alpha
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            // Scale animation
            if (useScaleAnimation)
            {
                transform.localScale = Vector3.Lerp(targetScale, startScale, t);
            }

            // Move animation
            if (useMoveAnimation)
            {
                rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            }

            yield return null;
        }

        // Ensure final values
        canvasGroup.alpha = 0f;
        if (useScaleAnimation)
        {
            transform.localScale = startScale;
        }


        isAnimating = false;
        // Optionally destroy or deactivate the GameObject
        gameObject.SetActive(false);
        // Or: Destroy(gameObject);
    }

    // Public methods to trigger animations manually
    public void ShowWithCustomDuration(float customDisplayDuration)
    {
        displayDuration = customDisplayDuration;
        ShowPopUp();
    }

    public void showMessage(string text)
    {
        if (!isEnable)
        {
            return;
        }
        SetMessage(text);
        ShowPopUp();
    }

    public void showPopUpForever(string text)
    {
        if (!isEnable)
        {
            return;
        }
        SetMessage(text);
        ShowPopUpForever();
    }

    public void SetMessage(string message)
    {
        textComponent.text = message;
        print(message);
        //// Also support TextMeshPro
        //TMPro.TextMeshProUGUI tmpText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        //if (tmpText != null)
        //{
        //    tmpText.text = message;
        //}
    }

    public void setEnable(bool isEnable)
    {
        this.isEnable = isEnable;
    }
}