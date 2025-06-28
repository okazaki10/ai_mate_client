using UnityEngine;

public class DragAndDropModel : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;

    public VRMModelManager vrmModelManager;

    void Start()
    {
        // Get the main camera
        cam = Camera.main;
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();
    }

    void OnMouseDown()
    {
        vrmModelManager.animator.SetBool("isDragging", true);
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
        vrmModelManager.animator.SetBool("isDragging", false);
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