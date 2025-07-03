using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;
    private RectTransform rectTransform;
    private BoxCollider2D boxCollider;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        boxCollider = GetComponent<BoxCollider2D>();
        // Get the main camera
        cam = Camera.main;
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();
    }

    private void Update()
    {
        if (rectTransform != null && boxCollider != null)
        {
            boxCollider.size = rectTransform.sizeDelta;
        }
    }

    void OnMouseDown()
    {
        // Calculate offset between mouse position and object position
        Vector3 mousePos = GetMouseWorldPosition();
        offset = transform.position - mousePos;
        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            // Move the object to follow the mouse
            Vector3 mousePos = GetMouseWorldPosition();
            transform.position = mousePos + offset;
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;

        // For 2D games (z = 0)
        mousePoint.z = cam.WorldToScreenPoint(transform.position).z;

        return cam.ScreenToWorldPoint(mousePoint);
    }
}