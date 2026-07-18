using UnityEngine;

[CreateAssetMenu(
    fileName = "NewWeaponData",
    menuName = "Project M/Equipment/Weapon Data"
)]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponName = "New Weapon";

    [TextArea]
    [SerializeField] private string description;

    [Header("Base Attack")]
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float lightAttackRange = 1.8f;
    [SerializeField] private float lightAttackRadius = 0.7f;

    [Header("Light Attack Movement")]
    [Range(0f, 1f)]
    [SerializeField] private float lightMovementMultiplier = 0.35f;

    [SerializeField] private float firstForwardStep = 0.22f;
    [SerializeField] private float secondForwardStep = 0.28f;
    [SerializeField] private float thirdForwardStep = 0.38f;

    [Header("Light Combo Step 1")]
    [SerializeField] private float firstWindupDuration = 0.15f;
    [SerializeField] private float firstRecoveryDuration = 0.25f;
    [SerializeField] private float firstDamageMultiplier = 1f;

    [Header("Light Combo Step 2")]
    [SerializeField] private float secondWindupDuration = 0.12f;
    [SerializeField] private float secondRecoveryDuration = 0.25f;
    [SerializeField] private float secondDamageMultiplier = 1f;

    [Header("Light Combo Step 3")]
    [SerializeField] private float thirdWindupDuration = 0.18f;
    [SerializeField] private float thirdRecoveryDuration = 0.45f;
    [SerializeField] private float thirdDamageMultiplier = 1.25f;

    [Header("Heavy Attack")]
    [SerializeField] private float heavyDamageMultiplier = 2f;
    [SerializeField] private float heavyRange = 2.1f;
    [SerializeField] private float heavyRadius = 0.85f;
    [SerializeField] private float heavyWindupDuration = 0.55f;
    [SerializeField] private float heavyRecoveryDuration = 0.75f;
    [SerializeField] private float heavyKnockbackMultiplier = 1.8f;
    [SerializeField] private float heavyHitReactionDuration = 0.35f;

    [Header("Heavy Attack Movement")]
    [Range(0f, 1f)]
    [SerializeField] private float heavyMovementMultiplier = 0.1f;

    [SerializeField] private float heavyForwardStep = 0.65f;

    [Header("Stamina Costs")]
    [Min(0f)]
    [SerializeField] private float firstLightStaminaCost = 8f;

    [Min(0f)]
    [SerializeField] private float secondLightStaminaCost = 9f;

    [Min(0f)]
    [SerializeField] private float thirdLightStaminaCost = 12f;

    [Min(0f)]
    [SerializeField] private float heavyStaminaCost = 22f;

    [Header("Hit Stop")]
    [SerializeField] private float lightHitStopDuration = 0.06f;
    [SerializeField] private float heavyHitStopDuration = 0.09f;

    public string WeaponName => weaponName;
    public string Description => description;

    public int BaseDamage => baseDamage;

    public float LightAttackRange => lightAttackRange;
    public float LightAttackRadius => lightAttackRadius;
    public float LightMovementMultiplier => lightMovementMultiplier;

    public float FirstForwardStep => firstForwardStep;
    public float SecondForwardStep => secondForwardStep;
    public float ThirdForwardStep => thirdForwardStep;

    public float FirstWindupDuration => firstWindupDuration;
    public float FirstRecoveryDuration => firstRecoveryDuration;
    public float FirstDamageMultiplier => firstDamageMultiplier;

    public float SecondWindupDuration => secondWindupDuration;
    public float SecondRecoveryDuration => secondRecoveryDuration;
    public float SecondDamageMultiplier => secondDamageMultiplier;

    public float ThirdWindupDuration => thirdWindupDuration;
    public float ThirdRecoveryDuration => thirdRecoveryDuration;
    public float ThirdDamageMultiplier => thirdDamageMultiplier;

    public float HeavyDamageMultiplier => heavyDamageMultiplier;
    public float HeavyRange => heavyRange;
    public float HeavyRadius => heavyRadius;
    public float HeavyWindupDuration => heavyWindupDuration;
    public float HeavyRecoveryDuration => heavyRecoveryDuration;
    public float HeavyKnockbackMultiplier => heavyKnockbackMultiplier;
    public float HeavyHitReactionDuration => heavyHitReactionDuration;

    public float HeavyMovementMultiplier => heavyMovementMultiplier;
    public float HeavyForwardStep => heavyForwardStep;

    public float LightHitStopDuration => lightHitStopDuration;
    public float HeavyHitStopDuration => heavyHitStopDuration;
    public float FirstLightStaminaCost =>
        firstLightStaminaCost;

    public float SecondLightStaminaCost =>
        secondLightStaminaCost;

    public float ThirdLightStaminaCost =>
        thirdLightStaminaCost;

    public float HeavyStaminaCost =>
        heavyStaminaCost;
    private void OnValidate()
    {
        baseDamage = Mathf.Max(1, baseDamage);

        lightAttackRange = Mathf.Max(0f, lightAttackRange);
        lightAttackRadius = Mathf.Max(0f, lightAttackRadius);

        firstWindupDuration = Mathf.Max(0f, firstWindupDuration);
        firstRecoveryDuration = Mathf.Max(0f, firstRecoveryDuration);

        secondWindupDuration = Mathf.Max(0f, secondWindupDuration);
        secondRecoveryDuration = Mathf.Max(0f, secondRecoveryDuration);

        thirdWindupDuration = Mathf.Max(0f, thirdWindupDuration);
        thirdRecoveryDuration = Mathf.Max(0f, thirdRecoveryDuration);

        heavyRange = Mathf.Max(0f, heavyRange);
        heavyRadius = Mathf.Max(0f, heavyRadius);
        heavyWindupDuration = Mathf.Max(0f, heavyWindupDuration);
        heavyRecoveryDuration = Mathf.Max(0f, heavyRecoveryDuration);

        firstForwardStep = Mathf.Max(0f, firstForwardStep);
        secondForwardStep = Mathf.Max(0f, secondForwardStep);
        thirdForwardStep = Mathf.Max(0f, thirdForwardStep);
        heavyForwardStep = Mathf.Max(0f, heavyForwardStep);

        lightHitStopDuration = Mathf.Max(0f, lightHitStopDuration);
        heavyHitStopDuration = Mathf.Max(0f, heavyHitStopDuration);
        firstLightStaminaCost =
        Mathf.Max(
            0f,
            firstLightStaminaCost
        );

        secondLightStaminaCost =
            Mathf.Max(
                0f,
                secondLightStaminaCost
            );

        thirdLightStaminaCost =
            Mathf.Max(
                0f,
                thirdLightStaminaCost
            );

        heavyStaminaCost =
            Mathf.Max(
                0f,
                heavyStaminaCost
            );
    }
}