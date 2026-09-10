using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Motion trail on the player's weapon(s), only visible during an attack swing.
/// Creates its own TrailRenderers at runtime on the named blade transforms so no
/// prefab wiring is needed.
/// </summary>
public class PlayerWeaponTrail : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;
    [Tooltip("Child transform names to attach a trail to (the weapon meshes).")]
    [SerializeField] private string[] bladeNames = { "Axe_L", "Axe_R" };

    [SerializeField] private float trailTime = 0.16f;
    [SerializeField] private float startWidth = 0.28f;
    [SerializeField] private Color trailColor = new Color(0.85f, 0.9f, 1f, 0.55f);
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.25f, 0f);

    private readonly List<TrailRenderer> trails = new List<TrailRenderer>();

    private void Awake()
    {
        if (playerAttack == null) playerAttack = GetComponentInParent<PlayerAttack>();
        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();

        var mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        mat.mainTexture = Texture2D.whiteTexture;

        foreach (var name in bladeNames)
        {
            var blade = FindDeep(transform, name);
            if (blade == null) continue;

            var anchor = new GameObject("WeaponTrail_" + name).transform;
            anchor.SetParent(blade, false);
            anchor.localPosition = localOffset;

            var tr = anchor.gameObject.AddComponent<TrailRenderer>();
            tr.time = trailTime;
            tr.startWidth = startWidth;
            tr.endWidth = 0f;
            tr.material = mat;
            tr.numCapVertices = 3;
            tr.alignment = LineAlignment.View;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.emitting = false;
            tr.startColor = trailColor;
            tr.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            trails.Add(tr);
        }
    }

    private void LateUpdate()
    {
        bool on = playerAttack != null && playerAttack.IsAttacking;
        for (int i = 0; i < trails.Count; i++)
        {
            if (trails[i].emitting == on) continue;
            if (on) trails[i].Clear();
            trails[i].emitting = on;
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
}
