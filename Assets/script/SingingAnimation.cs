using UnityEngine;

public class SingingAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float swingAngle = 15f; // Maximum angle to swing (degrees)
    [SerializeField] private float swingSpeed = 2f;  // Speed of the swinging motion
    [SerializeField] private bool useRandomTiming = true; // Add slight randomness to make it more natural

    [Header("Animator Integration")]
    [SerializeField] private bool overrideAnimator = false; // Use LateUpdate to override Animator
    [SerializeField] private string singingTrigger = "StartSinging"; // Animator parameter name

    public VRMModelManager vRMModelManager;

    private float timeOffset;
    private Vector3 headOriginalRotation;
    private Vector3 shoulderOriginalRotation;

    void Start()
    {
        // Store original rotations
        if (vRMModelManager.neck != null)
            headOriginalRotation = vRMModelManager.neck.localEulerAngles;

        if (vRMModelManager.spine != null)
            shoulderOriginalRotation = vRMModelManager.spine.localEulerAngles;

        // Add random offset for more natural movement
        if (useRandomTiming)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    // Method to change swing intensity
    public void SetSwingAngle(float newAngle)
    {
        swingAngle = newAngle;
    }


    // Use LateUpdate to apply changes after Animator
    void LateUpdate()
    {
        if (overrideAnimator)
        {
            PerformSingingAnimation();
        }
    }

    void PerformSingingAnimation()
    {
        // Calculate swing angle using sine wave for smooth back-and-forth motion
        float currentTime = Time.time * swingSpeed + timeOffset;
        float swingZ = Mathf.Sin(currentTime) * swingAngle;

        // Apply slight variation to make it more natural
        if (useRandomTiming)
        {
            float variation = Mathf.Sin(currentTime * 0.7f) * 0.3f; // Subtle secondary motion
            swingZ += variation;
        }

        // Apply rotation to head
        if (vRMModelManager.neck != null)
        {
            Vector3 headRotation = headOriginalRotation;
            headRotation.z += swingZ;
            vRMModelManager.neck.localEulerAngles = headRotation;
        }

        // Apply reduced rotation to shoulders (optional)
        if (vRMModelManager.spine != null)
        {
            Vector3 shoulderRotation = shoulderOriginalRotation;
            shoulderRotation.z += swingZ * 0.5f; // Shoulders move less than head
            vRMModelManager.spine.localEulerAngles = shoulderRotation;
        }
    }

    // Method to start/stop animation
    public void SetAnimationActive(bool active)
    {
        this.enabled = active;

        // Reset to original position when stopping
        if (!active)
        {
            if (vRMModelManager.neck != null)
                vRMModelManager.neck.localEulerAngles = headOriginalRotation;

            if (vRMModelManager.spine != null)
                vRMModelManager.spine.localEulerAngles = shoulderOriginalRotation;
        }
    }

    // Method to change animation speed during runtime
    public void SetSwingSpeed(float newSpeed)
    {
        swingSpeed = newSpeed;
    }

    // Method to trigger singing animation through Animator
    public void StartSingingAnimation()
    {
        overrideAnimator = true;
        SetAnimationActive(true);
    }

    // Method to stop singing animation
    public void StopSingingAnimation()
    {
        overrideAnimator = false;
        SetAnimationActive(false);
    }
}