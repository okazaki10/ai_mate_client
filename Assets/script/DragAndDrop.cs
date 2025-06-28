using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;

    void Start()
    {
        // Get the main camera
        cam = Camera.main;
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();
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

// Alternative method using Update() instead of OnMouse events
public class DragAndDropAlternative : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse buttonRecord pressed
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    isDragging = true;
                    Vector3 mousePos = GetMouseWorldPosition();
                    offset = transform.position - mousePos;
                }
            }
        }

        if (Input.GetMouseButton(0) && isDragging) // While holding left mouse buttonRecord
        {
            Vector3 mousePos = GetMouseWorldPosition();
            transform.position = mousePos + offset;
        }

        if (Input.GetMouseButtonUp(0)) // Left mouse buttonRecord released
        {
            isDragging = false;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = cam.WorldToScreenPoint(transform.position).z;
        return cam.ScreenToWorldPoint(mousePoint);
    }
}