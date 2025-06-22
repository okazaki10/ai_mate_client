using UnityEngine;
using VRM;
using System.Collections.Generic;

public class VRMAdvancedAudioMouth : MonoBehaviour
{
    [Header("Components")]
    public AudioSource audioSource;
    public VRMBlendShapeProxy vrmBlendShapeProxy;

    [Header("Audio Settings")]
    [Range(0f, 2000f)]
    public float sensitivity = 2f;
    [Range(0f, 1f)]
    public float smoothing = 0.15f;
    [Range(0f, 1f)]
    public float maxMouthOpen = 1f;

    [Header("Mouth Shape Settings")]
    [Range(0f, 1f)]
    public float aWeight = 0.6f;
    [Range(0f, 1f)]
    public float iWeight = 0.3f;
    [Range(0f, 1f)]
    public float uWeight = 0.4f;
    [Range(0f, 1f)]
    public float oWeight = 0.5f;

    [Header("Frequency Analysis")]
    public bool useFrequencyAnalysis = true;
    [Range(0, 8)]
    public int lowFreqBand = 0;
    [Range(0, 8)]
    public int midFreqBand = 2;
    [Range(0, 8)]
    public int highFreqBand = 4;

    private float currentMouthOpen = 0f;
    private float[] audioData = new float[512];
    private float[] freqBands = new float[8];

    void Start()
    {
        // Get VRM BlendShape Proxy if not assigned
        if (vrmBlendShapeProxy == null)
            vrmBlendShapeProxy = GetComponent<VRMBlendShapeProxy>();

        if (vrmBlendShapeProxy == null)
        {
            Debug.LogError("VRMBlendShapeProxy not found! Make sure this is attached to a VRM avatar.");
        }
    }

    void Update()
    {
        float targetMouthOpen = 0f;

        if (audioSource != null && audioSource.isPlaying)
        {
            if (useFrequencyAnalysis)
            {
                targetMouthOpen = AnalyzeFrequencies();
            }
            else
            {
                targetMouthOpen = AnalyzeSimpleVolume();
            }
        }

        // Smooth the mouth movement
        currentMouthOpen = Mathf.Lerp(currentMouthOpen, targetMouthOpen, Time.deltaTime / smoothing);

        // Apply to VRM blendshapes
        ApplyMouthShapes(currentMouthOpen);
    }

    private float AnalyzeSimpleVolume()
    {
        audioSource.GetSpectrumData(audioData, 0, FFTWindow.Rectangular);

        float sum = 0f;
        for (int i = 0; i < audioData.Length; i++)
        {
            sum += audioData[i];
        }
        float averageAmplitude = sum / audioData.Length;

        float result = averageAmplitude * sensitivity * maxMouthOpen;
        return Mathf.Clamp(result, 0f, maxMouthOpen);
    }

    private float AnalyzeFrequencies()
    {
        audioSource.GetSpectrumData(audioData, 0, FFTWindow.Rectangular);

        // Create frequency bands
        int count = 0;
        for (int i = 0; i < 8; i++)
        {
            float average = 0f;
            int sampleCount = (int)Mathf.Pow(2, i) * 2;

            if (i == 7) sampleCount += 2;

            for (int j = 0; j < sampleCount; j++)
            {
                if (count < audioData.Length)
                {
                    average += audioData[count] * (count + 1);
                    count++;
                }
            }

            average /= count;
            freqBands[i] = average * sensitivity;
        }

        // Combine different frequency bands for more realistic mouth movement
        float lowFreq = freqBands[lowFreqBand];
        float midFreq = freqBands[midFreqBand];
        float highFreq = freqBands[highFreqBand];

        float result = (lowFreq + midFreq + highFreq) * maxMouthOpen;
        return Mathf.Clamp(result, 0f, maxMouthOpen);
    }

    private void ApplyMouthShapes(float intensity)
    {
        if (vrmBlendShapeProxy == null) return;

        // Reset all mouth shapes first
        vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.A, 0f);
        vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.I, 0f);
        vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.U, 0f);
        vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.E, 0f);
        vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.O, 0f);

        if (intensity > 0.1f)
        {
            // Apply different mouth shapes based on frequency analysis or simple variation
            if (useFrequencyAnalysis)
            {
                // Use frequency bands to determine mouth shape
                float lowIntensity = freqBands[lowFreqBand];
                float midIntensity = freqBands[midFreqBand];
                float highIntensity = freqBands[highFreqBand];

                // Map frequencies to mouth shapes
                vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.A, lowIntensity * aWeight);
                vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.O, midIntensity * oWeight);
                vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.I, highIntensity * iWeight);
            }
            else
            {
                // Simple variation - alternate between shapes
                float time = Time.time * 10f;
                float variation = Mathf.Sin(time) * 0.5f + 0.5f;

                if (variation < 0.33f)
                {
                    vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.A, intensity * aWeight);
                }
                else if (variation < 0.66f)
                {
                    vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.O, intensity * oWeight);
                }
                else
                {
                    vrmBlendShapeProxy.ImmediatelySetValue(BlendShapePreset.I, intensity * iWeight);
                }
            }
        }
    }
}