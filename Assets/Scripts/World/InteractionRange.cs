using UnityEngine;

/// <summary>
/// Distance from a point (the player) to an interactable object, measured to the
/// nearest surface of its solid colliders instead of its root transform.
///
/// Why: stations are dressed by hand - their visuals get nudged around in the scene
/// while the root (where the script lives) stays put. Measuring to the colliders means
/// the interaction always follows what the player actually sees and bumps into, with
/// no separate "interact point" to keep in sync. Falls back to the root position when
/// the object has no colliders.
/// </summary>
public static class InteractionRange
{
    public static Collider[] CollectSolidColliders(Component owner)
    {
        var all = owner.GetComponentsInChildren<Collider>(true);
        var solid = new System.Collections.Generic.List<Collider>(all.Length);
        foreach (var c in all)
            if (!c.isTrigger)
                solid.Add(c);
        return solid.ToArray();
    }

    /// <summary>Horizontal (XZ) distance - height differences between the player's pivot
    /// and a low table or tall stone shouldn't change whether you're "next to" it.</summary>
    public static float Distance(Transform owner, Collider[] colliders, Vector3 point)
    {
        float best = float.MaxValue;

        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                Vector3 closest = c.ClosestPoint(point);
                best = Mathf.Min(best, FlatDistance(closest, point));
            }
        }

        return best < float.MaxValue ? best : FlatDistance(owner.position, point);
    }

    /// <summary>World-space top-centre of the object's visible renderers - where a
    /// floating label should sit. Falls back to the root position.</summary>
    public static Vector3 VisualTop(Component owner)
    {
        bool found = false;
        var bounds = new Bounds(owner.transform.position, Vector3.zero);
        foreach (var r in owner.GetComponentsInChildren<MeshRenderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
