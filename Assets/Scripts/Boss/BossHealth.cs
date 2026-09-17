using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// The boss's health pool. Mirrors EnemyHealth's hit-flash feel but the boss has
/// super-armor (no knockback, no AI interruption from taking damage) and does not
/// get destroyed on death - BossCombat plays a death-out instead.
/// </summary>
public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 320;

    [Header("Hit Flash")]
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f);

    [Header("VFX")]
    [SerializeField] private ParticleSystem hitVfx;
    [SerializeField] private ParticleSystem deathVfx;
    [SerializeField] private float deathTrauma = 0.7f;

    [Header("References")]
    [SerializeField] private BossCombat bossCombat;
    [SerializeField] private DemoObjectiveManager demoObjectiveManager;

    private Renderer bossRenderer;
    private Color originalColor;
    private Coroutine hitFlashCoroutine;

    private int currentHealth;
    private bool isDead;
    private bool halfHealthReached;
    private bool isInvulnerable;

    /// <summary>Normalized 0..1 fill, damage taken.</summary>
    public event Action<float> OnHealthChanged;

    /// <summary>Fires once, the first time health crosses 50%.</summary>
    public event Action OnHalfHealthReached;

    public event Action OnBossDefeated;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public float Normalized =>
        maxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / maxHealth)
            : 0f;

    private void Awake()
    {
        bossRenderer = GetComponentInChildren<Renderer>();

        if (bossRenderer != null)
        {
            originalColor = bossRenderer.material.color;
        }

        if (bossCombat == null)
        {
            bossCombat = GetComponent<BossCombat>();
        }

        if (demoObjectiveManager == null)
        {
            demoObjectiveManager =
                FindFirstObjectByType<DemoObjectiveManager>();
        }

        currentHealth = maxHealth;
    }

    public void TakeDamage(
        int damage,
        Vector3 hitDirection,
        float knockbackMultiplier,
        float reactionDuration)
    {
        if (isDead || isInvulnerable)
            return;

        currentHealth -= Mathf.Max(0, damage);
        currentHealth = Mathf.Max(currentHealth, 0);

        CombatVfx.Play(
            hitVfx,
            transform.position + Vector3.up * 1.2f,
            hitDirection.sqrMagnitude > 0.001f ? -hitDirection : Vector3.up
        );

        StartHitFlash();

        Debug.Log(
            $"[Boss] took {damage} damage. " +
            $"HP: {currentHealth}/{maxHealth}"
        );

        OnHealthChanged?.Invoke(Normalized);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (!halfHealthReached &&
            currentHealth <= maxHealth * 0.5f)
        {
            halfHealthReached = true;
            OnHalfHealthReached?.Invoke();
        }
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    /// <summary>
    /// Instantly mark the boss defeated from a save file - no death VFX/camera shake
    /// (there's nothing to react to, the fight already happened in a prior session).
    /// Still runs the same cleanup as a live death so the boss stays inert and the
    /// slice's completion state is consistent.
    /// </summary>
    public void RestoreDefeated()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;

        if (bossCombat != null)
        {
            bossCombat.OnDefeated();
        }

        if (demoObjectiveManager != null)
        {
            demoObjectiveManager.RegisterBossDefeated();
        }

        gameObject.SetActive(false);
    }

    /// <summary>Restore a fraction (0..1) of max health. Used by the boss heal ability.</summary>
    public void Heal(float fractionOfMax)
    {
        if (isDead)
            return;

        int healAmount =
            Mathf.RoundToInt(maxHealth * Mathf.Clamp01(fractionOfMax));

        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);

        Debug.Log(
            $"[Boss] healed {healAmount}. HP: {currentHealth}/{maxHealth}"
        );

        OnHealthChanged?.Invoke(Normalized);
    }

    private void StartHitFlash()
    {
        if (bossRenderer == null)
            return;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine = StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        bossRenderer.material.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        bossRenderer.material.color = originalColor;

        hitFlashCoroutine = null;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("[Boss] defeated.");

        CombatVfx.Play(
            deathVfx,
            transform.position + Vector3.up
        );

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.AddTrauma(deathTrauma);
        }

        OnBossDefeated?.Invoke();

        if (bossCombat != null)
        {
            bossCombat.OnDefeated();
        }

        if (demoObjectiveManager != null)
        {
            demoObjectiveManager.RegisterBossDefeated();
        }
    }
}
