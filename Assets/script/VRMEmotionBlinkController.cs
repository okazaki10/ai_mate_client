using UnityEngine;
using VRM;
using System.Collections;

public class VRMEmotionBlinkController : MonoBehaviour
{
    [Header("Components")]
    public VRMModelManager vrmModelManager;

    [Header("Blinking Settings")]
    public bool enableBlinking = true;
    [Range(1f, 10f)]
    public float blinkFrequency = 3f; // Blinks per minute
    [Range(0.1f, 0.5f)]
    public float blinkDuration = 0.15f;
    [Range(0f, 1f)]
    public float blinkIntensity = 1f;

    [Header("Emotion Settings")]
    [Range(0f, 1f)]
    public float emotionIntensity = 0.8f;
    [Range(0.1f, 2f)]
    public float emotionTransitionSpeed = 0.5f;

    [Header("Current Emotion")]
    public EmotionType currentEmotion = EmotionType.Neutral;

    [Header("Emotion Weights")]
    [Range(0f, 1f)]
    public float happyEyeWeight = 0.7f;
    [Range(0f, 1f)]
    public float happyMouthWeight = 0.6f;
    [Range(0f, 1f)]
    public float sadEyeWeight = 0.8f;
    [Range(0f, 1f)]
    public float sadMouthWeight = 0.5f;
    [Range(0f, 1f)]
    public float angryEyeWeight = 0.9f;
    [Range(0f, 1f)]
    public float angryMouthWeight = 0.4f;

    public enum EmotionType
    {
        Neutral,
        Happy,
        Sad,
        Angry
    }

    private float nextBlinkTime;
    private bool isBlinking = false;
    private EmotionType targetEmotion;
    private float currentEmotionValue = 0f;
    private Coroutine emotionTransition;
    private Coroutine blinkCoroutine;

    // Emotion blend values
    private float happyValue = 0f;
    private float sadValue = 0f;
    private float angryValue = 0f;

    void Start()
    {
        // Get VRM BlendShape Proxy if not assigned
        if (vrmModelManager.vrmBlendShapeProxy == null)
            vrmModelManager.vrmBlendShapeProxy = GetComponent<VRMBlendShapeProxy>();

        if (vrmModelManager.vrmBlendShapeProxy == null)
        {
            Debug.LogError("vrmModelManager.vrmBlendShapeProxy not found! Make sure this is attached to a VRM avatar.");
            return;
        }

        // Initialize blinking
        if (enableBlinking)
        {
            ScheduleNextBlink();
        }

        targetEmotion = currentEmotion;
    }

    void Update()
    {
        // Handle blinking
        if (enableBlinking && Time.time >= nextBlinkTime && !isBlinking)
        {
            StartBlink();
        }

        // Handle emotion transitions
        if (targetEmotion != currentEmotion)
        {
            SetEmotion(currentEmotion);
        }

        // Apply current emotion blendshapes
        ApplyEmotions();
    }

    #region Blinking System
    private void ScheduleNextBlink()
    {
        // Random interval between blinks (more natural)
        float interval = 60f / blinkFrequency;
        float randomVariation = Random.Range(-interval * 0.3f, interval * 0.3f);
        nextBlinkTime = Time.time + interval + randomVariation;
    }

    private void StartBlink()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkSequence());
    }

    private IEnumerator BlinkSequence()
    {
        isBlinking = true;

        // Close eyes
        float timer = 0f;
        float halfDuration = blinkDuration * 0.5f;

        // Closing phase
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float blinkValue = Mathf.Lerp(0f, blinkIntensity, timer / halfDuration);
            ApplyBlink(blinkValue);
            yield return null;
        }

        // Opening phase
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float blinkValue = Mathf.Lerp(blinkIntensity, 0f, timer / halfDuration);
            ApplyBlink(blinkValue);
            yield return null;
        }

        // Ensure eyes are fully open
        ApplyBlink(0f);

        isBlinking = false;
        ScheduleNextBlink();
    }

    private void ApplyBlink(float intensity)
    {
        if (vrmModelManager.vrmBlendShapeProxy == null) return;

        vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
            BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink),
            intensity
        );
    }

    public void TriggerBlink()
    {
        if (!isBlinking)
        {
            StartBlink();
        }
    }
    #endregion

    #region Emotion System
    public void SetEmotion(EmotionType emotion)
    {
        if (emotion == targetEmotion) return;

        targetEmotion = emotion;
        currentEmotion = emotion;

        if (emotionTransition != null)
            StopCoroutine(emotionTransition);

        emotionTransition = StartCoroutine(TransitionToEmotion(emotion));
    }

    private IEnumerator TransitionToEmotion(EmotionType targetEmotion)
    {
        float startTime = Time.time;

        // Store starting values
        float startHappy = happyValue;
        float startSad = sadValue;
        float startAngry = angryValue;

        // Target values
        float targetHappy = (targetEmotion == EmotionType.Happy) ? emotionIntensity : 0f;
        float targetSad = (targetEmotion == EmotionType.Sad) ? emotionIntensity : 0f;
        float targetAngry = (targetEmotion == EmotionType.Angry) ? emotionIntensity : 0f;

        while (Time.time - startTime < emotionTransitionSpeed)
        {
            float progress = (Time.time - startTime) / emotionTransitionSpeed;
            progress = Mathf.SmoothStep(0f, 1f, progress);

            happyValue = Mathf.Lerp(startHappy, targetHappy, progress);
            sadValue = Mathf.Lerp(startSad, targetSad, progress);
            angryValue = Mathf.Lerp(startAngry, targetAngry, progress);

            yield return null;
        }

        // Ensure final values are set
        happyValue = targetHappy;
        sadValue = targetSad;
        angryValue = targetAngry;
    }

    private void ApplyEmotions()
    {
        if (vrmModelManager.vrmBlendShapeProxy == null) return;

        // Apply Happy emotion
        if (happyValue > 0f)
        {
            //vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
            //    BlendShapeKey.CreateFromPreset(BlendShapePreset.Joy),
            //    happyValue * happyEyeWeight
            //);
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.Fun),
                happyValue * happyEyeWeight
            );
        }
        else
        {
            //vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
            //    BlendShapeKey.CreateFromPreset(BlendShapePreset.Joy), 0f
            //);
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.Fun), 0f
            );
        }

        // Apply Sad emotion
        if (sadValue > 0f)
        {
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.Sorrow),
                sadValue * sadEyeWeight
            );
            // Note: VRM doesn't have a dedicated sad mouth, so we'll use a subtle expression
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.E),
                sadValue * sadMouthWeight * 0.3f
            );
        }
        else
        {
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.Sorrow), 0f
            );
        }

        // Apply Angry emotion
        if (angryValue > 0f)
        {
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.Angry),
                angryValue * angryEyeWeight
            );
            // Use a combination of mouth shapes for angry expression
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.E),
                angryValue * angryMouthWeight * 0.5f
            );
        }
        else
        {
            vrmModelManager.vrmBlendShapeProxy.ImmediatelySetValue(
                BlendShapeKey.CreateFromPreset(BlendShapePreset.Angry), 0f
            );
        }
    }
    #endregion

    #region Public Methods
    public void SetHappy()
    {
        SetEmotion(EmotionType.Happy);
    }

    public void SetSad()
    {
        SetEmotion(EmotionType.Sad);
    }

    public void SetAngry()
    {
        SetEmotion(EmotionType.Angry);
    }

    public void SetNeutral()
    {
        SetEmotion(EmotionType.Neutral);
    }

    public void SetBlinking(bool enabled)
    {
        enableBlinking = enabled;
        if (!enabled && blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            ApplyBlink(0f);
            isBlinking = false;
        }
        else if (enabled && !isBlinking)
        {
            ScheduleNextBlink();
        }
    }

    public void SetEmotionIntensity(float intensity)
    {
        emotionIntensity = Mathf.Clamp01(intensity);
        // Re-apply current emotion with new intensity
        SetEmotion(currentEmotion);
    }
    #endregion

    #region Editor Helpers
    [System.Serializable]
    public class EmotionPreset
    {
        public string name;
        public EmotionType emotion;
        [Range(0f, 1f)]
        public float intensity = 0.8f;
    }

    // Method to test emotions in the inspector
    [ContextMenu("Test Happy")]
    private void TestHappy() { SetHappy(); }

    [ContextMenu("Test Sad")]
    private void TestSad() { SetSad(); }

    [ContextMenu("Test Angry")]
    private void TestAngry() { SetAngry(); }

    [ContextMenu("Test Neutral")]
    private void TestNeutral() { SetNeutral(); }

    [ContextMenu("Test Blink")]
    private void TestBlink() { TriggerBlink(); }
    #endregion
}