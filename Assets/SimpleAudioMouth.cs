using UnityEngine;

public class SimpleAudioMouth : MonoBehaviour
{
    [Header("Components")]
    public AudioSource audioSource;
    public SkinnedMeshRenderer meshRenderer;

    [Header("Settings")]
    public string mouthOpenBlendshapeName;
    [Range(0f, 2000f)]
    public float sensitivity = 2f;
    [Range(0f, 1f)]
    public float smoothing = 0.15f;
    [Range(0f, 100f)]
    public float maxMouthOpen = 70f;

    private int mouthBlendshapeIndex;
    private float currentMouthOpen = 0f;
    private float[] audioData = new float[256];

    void Start()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<SkinnedMeshRenderer>();
       
        mouthBlendshapeIndex = meshRenderer.sharedMesh.GetBlendShapeIndex(meshRenderer.sharedMesh.GetBlendShapeName(4));

        if (mouthBlendshapeIndex == -1)
        {
            Debug.LogError($"Blendshape '{mouthOpenBlendshapeName}' not found!");
        }
        
    }

    void Update()
    {
        float targetMouthOpen = 0f;

        if (audioSource != null && audioSource.isPlaying)
        {
            // Get audio volume
            audioSource.GetSpectrumData(audioData, 0, FFTWindow.Rectangular);

            // Calculate average amplitude
            float sum = 0f;
            for (int i = 0; i < audioData.Length; i++)
            {
                sum += audioData[i];
            }

            float averageAmplitude = sum / audioData.Length;
            targetMouthOpen = averageAmplitude * sensitivity * maxMouthOpen;
            targetMouthOpen = Mathf.Clamp(targetMouthOpen, 0f, maxMouthOpen);
        }

        // Smooth the mouth movement
        currentMouthOpen = Mathf.Lerp(currentMouthOpen, targetMouthOpen, Time.deltaTime / smoothing);

        // Apply to blendshape
        if (mouthBlendshapeIndex != -1)
        {
            meshRenderer.SetBlendShapeWeight(mouthBlendshapeIndex, currentMouthOpen);
        }
    }
}