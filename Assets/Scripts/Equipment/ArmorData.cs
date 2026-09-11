using UnityEngine;

[CreateAssetMenu(
    fileName = "NewArmorData",
    menuName = "Project M/Equipment/Armor Data"
)]
public class ArmorData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string armorName = "New Armor";

    [TextArea]
    [SerializeField] private string description;

    [Tooltip("Which mesh tier on the Axe Warrior model this shows - the built-in " +
             "SkinnedMeshRenderers under /Player/Axe_Warrior are named with an " +
             "\"A1_\"/\"A2_\"/\"A3_\" prefix per tier. Must match exactly.")]
    [SerializeField] private string meshTierPrefix = "A1_";

    [Header("Defense")]
    [Tooltip("Fraction of incoming damage absorbed before block/parry, e.g. 0.1 = 10% reduction.")]
    [Range(0f, 0.75f)]
    [SerializeField] private float damageReduction = 0.1f;

    public string ArmorName => armorName;
    public string Description => description;
    public string MeshTierPrefix => meshTierPrefix;
    public float DamageReduction => damageReduction;

    private void OnValidate()
    {
        damageReduction = Mathf.Clamp(damageReduction, 0f, 0.75f);
    }
}
