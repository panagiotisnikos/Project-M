using UnityEngine;

/// <summary>
/// Cheap fire/torch light flicker: layered noise on intensity plus a tiny
/// positional jitter so a static campfire light feels alive.
/// </summary>
[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 3.2f;
    [SerializeField] private float intensityJitter = 0.9f;
    [SerializeField] private float positionJitter = 0.06f;
    [SerializeField] private float speed = 9f;

    private Light lite;
    private Vector3 basePos;
    private float seed;

    private void Awake()
    {
        lite = GetComponent<Light>();
        basePos = transform.localPosition;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        float t = Time.time * speed + seed;
        // two octaves of Perlin, biased so it mostly sits near base and dips
        float n = Mathf.PerlinNoise(t, 0f) * 0.6f + Mathf.PerlinNoise(t * 2.3f, 5f) * 0.4f;
        lite.intensity = baseIntensity + (n - 0.5f) * 2f * intensityJitter;

        transform.localPosition = basePos + new Vector3(
            (Mathf.PerlinNoise(t * 0.7f, 11f) - 0.5f),
            (Mathf.PerlinNoise(t * 0.9f, 23f) - 0.5f) * 0.5f,
            (Mathf.PerlinNoise(t * 0.6f, 37f) - 0.5f)) * positionJitter * 2f;
    }
}
