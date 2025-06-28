using UnityEngine;
using UnityEngine.UI;

public class ScrollToScale : MonoBehaviour
{
    [SerializeField] private float scaleSpeed = 0.1f;
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 3.0f;

    public Canvas mainMenuCanvas;
 
    private void OnMouseOver()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            Vector3 currentScale = transform.localScale;
            Vector3 newScale = currentScale + Vector3.one * (scroll * scaleSpeed);

            // Clamp the scale
            float clampedScale = Mathf.Clamp(newScale.x, minScale, maxScale);
            transform.localScale = Vector3.one * clampedScale;
        }

        var rightClick = Input.GetMouseButtonDown(1);
        if (rightClick)
        {
            mainMenuCanvas.enabled = !mainMenuCanvas.enabled;
        }
    }
}