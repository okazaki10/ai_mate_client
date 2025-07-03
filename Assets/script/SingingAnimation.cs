using UnityEngine;

public class SingingAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float swingAngle = 15f; // Maximum angle to swing (degrees)
    [SerializeField] private float baseBPM = 120f;   // Base BPM for swing speed calculation
    [SerializeField] private float bpmMultiplier = 1f; // Multiplier to fine-tune BPM response
    [SerializeField] private bool useRandomTiming = true; // Add slight randomness to make it more natural

    [Header("Natural Movement Settings")]
    [SerializeField] private float breathingIntensity = 0.3f; // Breathing motion intensity
    [SerializeField] private float breathingSpeed = 0.8f;     // Breathing speed (slower than main swing)
    [SerializeField] private float microMovementIntensity = 0.15f; // Small random movements
    [SerializeField] private float expressionVariation = 0.4f; // Variation in expression intensity
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private bool enableMicroMovements = true;

    [Header("Multi-axis Movement")]
    [SerializeField] private bool enableYAxisMovement = true;
    [SerializeField] private bool enableXAxisMovement = true;
    [SerializeField] private float yAxisIntensity = 0.6f;
    [SerializeField] private float xAxisIntensity = 0.4f;

    [Header("Animator Integration")]
    [SerializeField] private bool overrideAnimator = false;
    [SerializeField] private string singingTrigger = "StartSinging";

    public VRMModelManager vRMModelManager;

    private float timeOffset;
    private float breathingOffset;
    private float microMovementOffset;
    private Vector3 headOriginalRotation;
    private Vector3 shoulderOriginalRotation;
    private float currentBPM;
    private float swingSpeed;

    // Perlin noise seeds for natural variation
    private float noiseXSeed;
    private float noiseYSeed;
    private float noiseZSeed;

    void Start()
    {
        // Store original rotations
        if (vRMModelManager.neck != null)
            headOriginalRotation = vRMModelManager.neck.localEulerAngles;
        if (vRMModelManager.spine != null)
            shoulderOriginalRotation = vRMModelManager.spine.localEulerAngles;

        // Initialize random offsets and seeds
        if (useRandomTiming)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
            breathingOffset = Random.Range(0f, Mathf.PI * 2f);
            microMovementOffset = Random.Range(0f, Mathf.PI * 2f);

            // Random seeds for Perlin noise
            noiseXSeed = Random.Range(0f, 1000f);
            noiseYSeed = Random.Range(0f, 1000f);
            noiseZSeed = Random.Range(0f, 1000f);
        }

        // Set initial BPM
        SetBPM(baseBPM);
    }

    // Method to set BPM and calculate swing speed
    public void SetBPM(float bpm)
    {
        currentBPM = bpm;
        // Convert BPM to swing speed (BPM / 60 gives beats per second)
        // Multiply by bpmMultiplier for fine-tuning
        swingSpeed = (bpm / 60f) * bpmMultiplier;
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
        float currentTime = Time.time;

        // Main swing calculation
        float mainSwingTime = currentTime * swingSpeed + timeOffset;
        float swingZ = Mathf.Sin(mainSwingTime) * swingAngle;

        Vector3 headRotation = headOriginalRotation;
        Vector3 shoulderRotation = shoulderOriginalRotation;

        // Add breathing motion (slower, more subtle)
        if (enableBreathing)
        {
            float breathingTime = currentTime * breathingSpeed + breathingOffset;
            float breathingMotion = Mathf.Sin(breathingTime) * breathingIntensity;
            swingZ += breathingMotion;
        }

        // Add micro movements using Perlin noise for natural variation
        if (enableMicroMovements)
        {
            float microTime = currentTime * 0.5f + microMovementOffset;
            float microX = (Mathf.PerlinNoise(noiseXSeed + microTime, 0f) - 0.5f) * 2f * microMovementIntensity;
            float microY = (Mathf.PerlinNoise(noiseYSeed + microTime, 0f) - 0.5f) * 2f * microMovementIntensity;
            float microZ = (Mathf.PerlinNoise(noiseZSeed + microTime, 0f) - 0.5f) * 2f * microMovementIntensity;

            headRotation.x += microX;
            headRotation.y += microY;
            swingZ += microZ;
        }

        // Add expression variation (intensity changes over time)
        if (useRandomTiming)
        {
            float expressionTime = currentTime * 0.3f;
            float expressionMod = (Mathf.Sin(expressionTime) + 1f) * 0.5f; // 0 to 1
            expressionMod = Mathf.Lerp(1f - expressionVariation, 1f + expressionVariation, expressionMod);
            swingZ *= expressionMod;
        }

        // Multi-axis movement for more natural motion
        if (enableYAxisMovement)
        {
            float ySwing = Mathf.Sin(mainSwingTime * 0.7f) * swingAngle * yAxisIntensity;
            headRotation.y += ySwing;
            shoulderRotation.y += ySwing * 0.3f;
        }

        if (enableXAxisMovement)
        {
            float xSwing = Mathf.Cos(mainSwingTime * 0.9f) * swingAngle * xAxisIntensity;
            headRotation.x += xSwing;
            shoulderRotation.x += xSwing * 0.4f;
        }

        // Apply main Z-axis swing
        headRotation.z += swingZ;
        shoulderRotation.z += swingZ * 0.5f; // Shoulders move less than head

        // Apply rotations
        if (vRMModelManager.neck != null)
        {
            vRMModelManager.neck.localEulerAngles = headRotation;
        }

        if (vRMModelManager.spine != null)
        {
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

    // Method to change BPM multiplier during runtime
    public void SetBPMMultiplier(float multiplier)
    {
        bpmMultiplier = multiplier;
        SetBPM(currentBPM); // Recalculate swing speed
    }

    // Method to update BPM in real-time (call this from your music system)
    public void UpdateBPM(float newBPM)
    {
        SetBPM(newBPM);
    }

    // Method to trigger singing animation through Animator
    public void StartSingingAnimation(float newBPM)
    {
        overrideAnimator = true;
        SetBPM(newBPM);
        SetAnimationActive(true);
    }

    // Method to stop singing animation
    public void StopSingingAnimation()
    {
        overrideAnimator = false;
        SetAnimationActive(false);
    }

    // Method to set animation intensity based on music volume/energy
    public void SetAnimationIntensity(float intensity)
    {
        // Clamp intensity between 0 and 2 for reasonable range
        intensity = Mathf.Clamp(intensity, 0f, 2f);

        // Adjust various parameters based on intensity
        swingAngle = 15f * intensity;
        breathingIntensity = 0.3f * intensity;
        microMovementIntensity = 0.15f * intensity;
        expressionVariation = 0.4f * intensity;
    }

    // Method to sync with music beats (call this on each beat)
    public void OnMusicBeat()
    {
        // Add a slight emphasis on beats
        if (useRandomTiming)
        {
            timeOffset += Random.Range(-0.1f, 0.1f);
        }
    }
}