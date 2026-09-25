using UnityEngine;

/// <summary>
/// Trauma-based camera shake. One 0..1 knob that decays over time; the felt shake
/// is trauma squared so small amounts are subtle and big hits really kick.
///
/// It does NOT move the camera itself - CameraFollow owns the transform and adds
/// CurrentPositionOffset / CurrentRotationOffset at the end of its LateUpdate, so
/// there is no script-execution-order dependency.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake")]
    [SerializeField] private float maxPositionOffset = 0.5f;
    [SerializeField] private float maxRotationOffset = 4f;
    [SerializeField] private float traumaDecayPerSecond = 1.7f;

    private float trauma;

    public Vector3 CurrentPositionOffset { get; private set; }
    public Vector3 CurrentRotationOffset { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Add trauma (0..1). Small ~0.15 for a light hit, ~0.5+ for a heavy impact.</summary>
    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));
    }

    private void Update()
    {
        if (GameUIController.IsPaused)
        {
            CurrentPositionOffset = Vector3.zero;
            CurrentRotationOffset = Vector3.zero;
            return;
        }

        trauma = Mathf.Max(
            0f,
            trauma - traumaDecayPerSecond * Time.unscaledDeltaTime
        );

        if (trauma <= 0f)
        {
            CurrentPositionOffset = Vector3.zero;
            CurrentRotationOffset = Vector3.zero;
            return;
        }

        // Accessibility: player-controlled intensity knob, plus an extra cut when
        // "reduce camera motion" is on (kept non-zero rather than a hard disable, so a
        // parry/hit still reads as something happened without the full-strength shake).
        float shake = trauma * trauma * GameSettings.ShakeIntensity * (GameSettings.ReduceCameraMotion ? 0.3f : 1f);

        CurrentPositionOffset = new Vector3(
            Random.Range(-1f, 1f) * maxPositionOffset * shake,
            Random.Range(-1f, 1f) * maxPositionOffset * shake,
            0f
        );

        CurrentRotationOffset = new Vector3(
            0f,
            0f,
            Random.Range(-1f, 1f) * maxRotationOffset * shake
        );
    }
}
