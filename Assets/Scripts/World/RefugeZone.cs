using UnityEngine;

/// <summary>
/// Refuge V1's safe zone: a recognizable trigger boundary that marks the
/// warm/safe/quiet counterpoint to the dark/dangerous/unpredictable outside.
/// Owns the respawn anchor, basic passive recovery while inside, and is the
/// sole place that kindles a HearthEmber charge (the refuge's one lightweight
/// benefit - see that class for why it isn't a Valheim-style timed buff).
///
/// One refuge exists in the vertical slice, so this mirrors WorldRegion's
/// ActiveRegion pattern with a single static Main rather than a multi-refuge
/// registry - trivially extendable later (a List, or per-refuge respawn
/// selection) if the game ever needs more than one safe camp.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RefugeZone : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Where the player is placed on death/respawn. Defaults to this transform if left empty.")]
    [SerializeField] private Transform respawnPoint;

    [Header("Recovery (while inside)")]
    [SerializeField] private float healthRegenPerSecond = 8f;
    [SerializeField] private float staminaRegenMultiplier = 2.5f;

    [Header("Hearth Ember (the refuge's one lightweight benefit)")]
    [Tooltip("Continuous seconds standing inside before a fresh Ember charge is kindled.")]
    [SerializeField] private float emberGrantDelay = 4f;

    [Header("Feel")]
    [SerializeField] private AudioClip enterSfx;
    [SerializeField] private AudioClip exitSfx;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.4f;

    public static RefugeZone Main { get; private set; }

    public Transform RespawnPoint => respawnPoint != null ? respawnPoint : transform;
    public bool IsPlayerInside { get; private set; }

    /// <summary>Fired the moment the player crosses into/out of the refuge - hook
    /// for ambience, lighting, or UI reacting to the safe/unsafe transition.</summary>
    public event System.Action Entered;
    public event System.Action Exited;

    private PlayerHealth playerHealth;
    private PlayerStamina playerStamina;
    private HearthEmber hearthEmber;
    private float healAccumulator;
    private float continuousInsideTime;

    private void Awake()
    {
        if (Main == null) Main = this;
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || IsPlayerInside) return;

        IsPlayerInside = true;
        continuousInsideTime = 0f;
        healAccumulator = 0f;

        playerHealth = other.GetComponentInParent<PlayerHealth>();
        playerStamina = other.GetComponentInParent<PlayerStamina>();
        hearthEmber = other.GetComponentInParent<HearthEmber>();

        CombatAudio.Play(enterSfx, transform.position, sfxVolume);
        Debug.Log("[RefugeZone] Entered the refuge - safe.");
        Entered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !IsPlayerInside) return;

        IsPlayerInside = false;

        CombatAudio.Play(exitSfx, transform.position, sfxVolume);
        Debug.Log("[RefugeZone] Left the refuge.");
        Exited?.Invoke();
    }

    private void Update()
    {
        if (!IsPlayerInside) return;

        continuousInsideTime += Time.deltaTime;

        ApplyRecovery();

        if (hearthEmber != null && !hearthEmber.HasCharge && continuousInsideTime >= emberGrantDelay)
            hearthEmber.Grant();
    }

    private void ApplyRecovery()
    {
        if (playerHealth != null && !playerHealth.IsDead && playerHealth.CurrentHealth < playerHealth.MaxHealth)
        {
            healAccumulator += healthRegenPerSecond * Time.deltaTime;
            int whole = Mathf.FloorToInt(healAccumulator);
            if (whole > 0)
            {
                playerHealth.Heal(whole);
                healAccumulator -= whole;
            }
        }

        /*
         * Reuses PlayerStamina's existing consumable-buff API instead of adding a
         * second regen system - refreshing a short-lived buff every frame while
         * inside keeps it perpetually active, and it naturally decays out ~1s
         * after leaving instead of cutting off abruptly at the boundary.
         */
        if (playerStamina != null)
            playerStamina.ApplyRegenBuff(staminaRegenMultiplier, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0.2f, 0.35f);

        if (TryGetComponent<SphereCollider>(out var sc))
            Gizmos.DrawWireSphere(transform.TransformPoint(sc.center), sc.radius * transform.lossyScale.x);
    }
}
