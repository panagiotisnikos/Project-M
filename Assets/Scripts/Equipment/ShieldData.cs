using UnityEngine;

[CreateAssetMenu(
    fileName = "NewShieldData",
    menuName = "Project M/Equipment/Shield Data"
)]
public class ShieldData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string shieldName = "New Shield";

    [TextArea]
    [SerializeField] private string description;

    [Header("Blocking")]
    [Range(0f, 360f)]
    [SerializeField] private float blockAngle = 120f;

    [Range(0f, 1f)]
    [SerializeField] private float blockedDamageMultiplier = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float blockedKnockbackMultiplier = 0.2f;

    [Range(0f, 1f)]
    [SerializeField] private float blockingMoveSpeedMultiplier = 0.45f;

    [Header("Parry")]
    [SerializeField] private float parryWindowDuration = 0.2f;
    [SerializeField] private float parryStaggerDuration = 1.25f;

    public string ShieldName => shieldName;
    public string Description => description;

    public float BlockAngle => blockAngle;
    public float BlockedDamageMultiplier => blockedDamageMultiplier;
    public float BlockedKnockbackMultiplier => blockedKnockbackMultiplier;
    public float BlockingMoveSpeedMultiplier => blockingMoveSpeedMultiplier;
    public float BlockStaminaMultiplier =>
        blockStaminaMultiplier;

    public float ParryStaminaCost =>
        parryStaminaCost;
    public float ParryWindowDuration => parryWindowDuration;
    public float ParryStaggerDuration => parryStaggerDuration;
    [Header("Stamina Costs")]
    [Min(0f)]
    [SerializeField] private float blockStaminaMultiplier = 1.2f;

    [Min(0f)]
    [SerializeField] private float parryStaminaCost = 3f;
    private void OnValidate()
    {
        parryWindowDuration =
            Mathf.Max(0f, parryWindowDuration);

        parryStaggerDuration =
            Mathf.Max(0f, parryStaggerDuration);
        blockStaminaMultiplier =
        Mathf.Max(
            0f,
            blockStaminaMultiplier
        );

        parryStaminaCost =
            Mathf.Max(
                0f,
                parryStaminaCost
            );    
    }
}