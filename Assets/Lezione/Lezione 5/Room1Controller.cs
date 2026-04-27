using DG.Tweening;
using UnityEngine;
using Ludocore;

/// <summary>
/// Room 1 interaction controller.
/// Detects player presence using Trigger Sensor and plays layered feedback animations.
/// Each feedback has its own DoTween animation with exposed curves.
/// Layered feedbacks:
/// 1. Audio pitch animation (0 to 1)
/// 2. Environment lighting animation (intensity multipliers)
/// 3. Ceiling material emissive animation
/// </summary>
public class Room1Controller : MonoBehaviour
{
    //==================== CONFIG =====================
    [Header("Trigger Sensor")]
    [Tooltip("Trigger Sensor to detect player")]
    [SerializeField] private TriggerSensor triggerSensor;

    [Header("Audio Feedback")]
    [Tooltip("Audio Source for pitch animation")]
    [SerializeField] private AudioSource audioSource;
    [Min(0.1f)]
    [SerializeField] private float pitchEntryDuration = 1f;
    [SerializeField] private AnimationCurve pitchEntryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Min(0f)]
    [SerializeField] private float targetPitch = 1f;

    [Header("Environment Lighting")]
    [Tooltip("Initial intensity multiplier for environment lighting")]
    [Min(0f)]
    [SerializeField] private float envLightingIntensityMin = 0f;
    [Min(0f)]
    [SerializeField] private float envLightingIntensityMax = 1f;
    
    [Tooltip("Initial intensity multiplier for environment reflections")]
    [Min(0f)]
    [SerializeField] private float envReflectionsIntensityMin = 0f;
    [Min(0f)]
    [SerializeField] private float envReflectionsIntensityMax = 1f;

    [Header("Ceiling Material")]
    [Tooltip("Ceiling material with emissive color")]
    [SerializeField] private Material ceilingMaterial;
    [SerializeField] private string emissivePropertyName = "_EmissionColor";
    [Tooltip("Emissive intensity range")]
    [SerializeField] private float ceilingEmissiveMin = -10f;
    [SerializeField] private float ceilingEmissiveMax = 5f;
    [SerializeField, ColorUsage(false, true)] private Color baseEmissiveColor = Color.white;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    //==================== STATE =====================
    private Tween _pitchTween;
    private Tween _lightingTween;
    private Tween _emissiveTween;
    private bool _isPlaying;
    private bool _wasDetected;
    
    private float _currentLightingIntensity;
    private float _currentReflectionsIntensity;
    private float _currentEmissiveIntensity;

    //==================== LIFECYCLE =====================
    private void Awake()
    {
        InitializeMaterial();
    }

    private void Update()
    {
        if (!triggerSensor) return;

        bool isDetected = triggerSensor.HasDetections;

        // Transition from no detection to detection
        if (isDetected && !_wasDetected)
        {
            Play();
        }

        _wasDetected = isDetected;
    }

    //==================== PUBLIC =====================
    /// <summary>Play the interaction feedback.</summary>
    public void Play()
    {
        if (_isPlaying) return;

        _isPlaying = true;
        PlayAudioAnimation();
        PlayLightingAnimation();
        PlayEmissiveAnimation();
    }

    /// <summary>Stop the interaction feedback.</summary>
    public void Stop()
    {
        _isPlaying = false;
        KillAllTweens();
    }

    //==================== PRIVATE =====================
    private void InitializeMaterial()
    {
        if (!ceilingMaterial)
        {
            Debug.LogWarning("[Room1Controller] No ceiling material assigned", gameObject);
            return;
        }

        if (!ceilingMaterial.HasProperty(emissivePropertyName))
        {
            Debug.LogWarning($"[Room1Controller] Material does not have property '{emissivePropertyName}'", gameObject);
            return;
        }

        ceilingMaterial.EnableKeyword("_EMISSION");
    }

    private void PlayAudioAnimation()
    {
        if (!audioSource)
        {
            Debug.LogWarning("[Room1Controller] No Audio Source assigned", gameObject);
            return;
        }

        KillTween(ref _pitchTween);

        // Animate pitch from 0 to target
        _pitchTween = DOTween.To(
            () => 0f,
            x => audioSource.pitch = x,
            targetPitch,
            pitchEntryDuration
        )
        .SetEase(pitchEntryCurve)
        .OnComplete(() => 
        {
            if (_pitchTween == null || !_pitchTween.active) _isPlaying = false;
        });
    }

    private void PlayLightingAnimation()
    {
        // Animate environment lighting intensity
        _currentLightingIntensity = envLightingIntensityMin;
        KillTween(ref _lightingTween);

        _lightingTween = DOTween.To(
            () => _currentLightingIntensity,
            x => 
            {
                _currentLightingIntensity = x;
                ApplyLightingIntensity(x);
            },
            envLightingIntensityMax,
            pitchEntryDuration
        )
        .SetEase(pitchEntryCurve);

        // Animate environment reflections intensity (parallel)
        _currentReflectionsIntensity = envReflectionsIntensityMin;
        var reflectionsTween = DOTween.To(
            () => _currentReflectionsIntensity,
            x => 
            {
                _currentReflectionsIntensity = x;
                ApplyReflectionsIntensity(x);
            },
            envReflectionsIntensityMax,
            pitchEntryDuration
        )
        .SetEase(pitchEntryCurve);
    }

    private void PlayEmissiveAnimation()
    {
        if (!ceilingMaterial)
            return;

        _currentEmissiveIntensity = ceilingEmissiveMin;
        KillTween(ref _emissiveTween);

        _emissiveTween = DOTween.To(
            () => _currentEmissiveIntensity,
            x => 
            {
                _currentEmissiveIntensity = x;
                ApplyEmissiveIntensity(x);
            },
            ceilingEmissiveMax,
            pitchEntryDuration
        )
        .SetEase(pitchEntryCurve);
    }

    private void ApplyLightingIntensity(float intensity)
    {
        RenderSettings.ambientIntensity = intensity;
    }

    private void ApplyReflectionsIntensity(float intensity)
    {
        RenderSettings.reflectionIntensity = intensity;
    }

    private void ApplyEmissiveIntensity(float intensity)
    {
        if (!ceilingMaterial) return;

        Color emissionColor = baseEmissiveColor * intensity;
        ceilingMaterial.SetColor(emissivePropertyName, emissionColor);
    }

    private void KillTween(ref Tween tween)
    {
        if (tween is { active: true })
        {
            tween.Kill();
        }
        tween = null;
    }

    private void KillAllTweens()
    {
        KillTween(ref _pitchTween);
        KillTween(ref _lightingTween);
        KillTween(ref _emissiveTween);
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    //==================== DEBUG =====================
    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUI.Label(new Rect(10, 70, 300, 20), $"[Room1Controller] Playing: {_isPlaying}");
        
        if (triggerSensor)
        {
            GUI.Label(new Rect(10, 90, 300, 20), $"Detected: {triggerSensor.HasDetections}");
        }

        if (audioSource)
        {
            GUI.Label(new Rect(10, 110, 300, 20), $"Audio Pitch: {audioSource.pitch:F2}");
        }

        GUI.Label(new Rect(10, 130, 300, 20), $"Lighting: {_currentLightingIntensity:F2}");
        GUI.Label(new Rect(10, 150, 300, 20), $"Reflections: {_currentReflectionsIntensity:F2}");
        GUI.Label(new Rect(10, 170, 300, 20), $"Emissive: {_currentEmissiveIntensity:F2}");
    }
}