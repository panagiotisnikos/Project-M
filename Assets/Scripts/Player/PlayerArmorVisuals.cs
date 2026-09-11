using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows the right armor tier on the Axe Warrior model. The model ships with 3
/// full armor sets built in (SkinnedMeshRenderers named "A1_*"/"A2_*"/"A3_*" under
/// /Player/Axe_Warrior) all enabled at once by default. This toggles exactly one
/// tier's renderers on - the rest of the body (unprefixed meshes: face, hair,
/// hands, base torso/limbs, etc.) is never touched.
///
/// With no armor equipped the model still shows <see cref="defaultTierPrefix"/>
/// (A1 - the warrior's own starting gear); looting a better ArmorData and
/// equipping it swaps the tier and adds its passive damage reduction
/// (see PlayerHealth.ApplyArmorReduction).
/// </summary>
public class PlayerArmorVisuals : MonoBehaviour
{
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private string defaultTierPrefix = "A1_";
    [Tooltip("Every mesh tier prefix that exists on the model, so an unequipped tier is always hidden.")]
    [SerializeField] private string[] knownTierPrefixes = { "A1_", "A2_", "A3_" };
    [Tooltip("Top-level gear-slot folders on the model (Body/Hand/Head/Leg_Equipments) that ship " +
             "inactive by default - every tier mesh lives under one of these, so they must be force-" +
             "activated once; per-tier visibility is then controlled by each mesh's own renderer.enabled.")]
    [SerializeField] private string[] equipmentContainerNames =
        { "Body_Equipments", "Hand_Equipments", "Head_Equipments", "Leg_Equipments" };

    private SkinnedMeshRenderer[] renderers;

    private void Awake()
    {
        if (playerEquipment == null) playerEquipment = GetComponentInParent<PlayerEquipment>();
        if (modelRoot == null) modelRoot = FindAxeWarrior();

        if (modelRoot != null)
        {
            foreach (var name in equipmentContainerNames)
            {
                var container = modelRoot.Find(name);
                if (container != null) container.gameObject.SetActive(true);
            }
        }

        renderers = modelRoot != null
            ? modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            : new SkinnedMeshRenderer[0];
    }

    private Transform FindAxeWarrior()
    {
        var t = transform.Find("Axe_Warrior");
        return t != null ? t : transform;
    }

    private void OnEnable()
    {
        if (playerEquipment != null) playerEquipment.OnArmorEquipped += HandleArmorEquipped;
    }

    private void OnDisable()
    {
        if (playerEquipment != null) playerEquipment.OnArmorEquipped -= HandleArmorEquipped;
    }

    private void Start()
    {
        ApplyTier(CurrentTierPrefix());
    }

    private void HandleArmorEquipped(ArmorData armor)
    {
        ApplyTier(CurrentTierPrefix());
    }

    private string CurrentTierPrefix()
    {
        var armor = playerEquipment != null ? playerEquipment.EquippedArmor : null;
        return armor != null && !string.IsNullOrEmpty(armor.MeshTierPrefix)
            ? armor.MeshTierPrefix
            : defaultTierPrefix;
    }

    private void ApplyTier(string tierPrefix)
    {
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            string n = r.gameObject.name;
            bool isTierMesh = false;
            foreach (var prefix in knownTierPrefixes)
            {
                if (n.StartsWith(prefix)) { isTierMesh = true; break; }
            }

            if (!isTierMesh) continue; // base body mesh - always shown
            r.enabled = n.StartsWith(tierPrefix);
        }
    }
}
