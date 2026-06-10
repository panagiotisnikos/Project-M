using UnityEngine;

/// <summary>
/// Changes simple visual elements based on the adaptive world state.
/// This gives the player clear feedback that the game reacts to performance.
/// </summary>
public class WorldVisualAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldAdaptationManager worldAdaptationManager;
    [SerializeField] private Renderer targetRenderer;

    [Header("Materials")]
    [SerializeField] private Material stableMaterial;
    [SerializeField] private Material balancedMaterial;
    [SerializeField] private Material decayingMaterial;

    private WorldAdaptationManager.WorldState lastState;

    private void Start()
    {
        ApplyVisualState();
    }

    private void Update()
    {
        if (worldAdaptationManager == null || targetRenderer == null)
            return;

        if (worldAdaptationManager.CurrentState == lastState)
            return;

        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        lastState = worldAdaptationManager.CurrentState;

        switch (lastState)
        {
            case WorldAdaptationManager.WorldState.Stable:
                targetRenderer.material = stableMaterial;
                break;

            case WorldAdaptationManager.WorldState.Balanced:
                targetRenderer.material = balancedMaterial;
                break;

            case WorldAdaptationManager.WorldState.Decaying:
                targetRenderer.material = decayingMaterial;
                break;
        }

        Debug.Log($"[WorldVisualAdapter] Ground material changed to {lastState}");
    }
}