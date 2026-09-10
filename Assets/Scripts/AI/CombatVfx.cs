using UnityEngine;

/// <summary>
/// One-shot VFX spawner. Instantiates a self-cleaning copy of a ParticleSystem
/// prefab at a world point, optionally aligned to a surface normal.
///
/// Every combat script keeps its own serialized prefab reference and calls this;
/// a null prefab is a silent no-op, so effects are fully optional and the pack's
/// VFX can be dropped straight into the same Inspector slots later.
/// </summary>
public static class CombatVfx
{
    public static void Play(
        ParticleSystem prefab,
        Vector3 position)
    {
        Play(prefab, position, Vector3.up);
    }

    public static void Play(
        ParticleSystem prefab,
        Vector3 position,
        Vector3 forward)
    {
        if (prefab == null)
            return;

        Quaternion rotation =
            forward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(forward.normalized)
                : Quaternion.identity;

        ParticleSystem instance =
            Object.Instantiate(prefab, position, rotation);

        instance.Play();

        float lifetime =
            instance.main.duration +
            instance.main.startLifetime.constantMax +
            0.5f;

        Object.Destroy(instance.gameObject, lifetime);
    }
}
