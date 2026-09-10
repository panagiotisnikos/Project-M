using UnityEngine;

/// <summary>
/// Plays the gameplay music loop and crossfades to the boss loop while the boss
/// fight is active, then back. Polls BossCombat.FightActive so it needs no hooks
/// into the boss code. One instance per gameplay scene.
/// </summary>
public class MusicManager : MonoBehaviour
{
    [Header("Sources (looped, play on awake)")]
    [SerializeField] private AudioSource gameplaySource;
    [SerializeField] private AudioSource bossSource;

    [Header("Refs")]
    [SerializeField] private BossCombat bossCombat;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Mix")]
    [Range(0f, 1f)] [SerializeField] private float gameplayVolume = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float bossVolume = 0.6f;
    [Tooltip("Volume units per second for the crossfade.")]
    [SerializeField] private float fadeSpeed = 0.6f;

    private void Awake()
    {
        if (bossCombat == null) bossCombat = FindFirstObjectByType<BossCombat>();
        if (bossHealth == null) bossHealth = FindFirstObjectByType<BossHealth>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();

        Prepare(gameplaySource, 0f);
        Prepare(bossSource, 0f);
    }

    private static void Prepare(AudioSource s, float startVolume)
    {
        if (s == null) return;
        s.loop = true;
        s.playOnAwake = false;
        s.volume = startVolume;
        s.Play();
    }

    private void Update()
    {
        bool bossActive =
            bossCombat != null && bossCombat.FightActive &&
            (bossHealth == null || !bossHealth.IsDead);

        bool playerDead = playerHealth != null && playerHealth.IsDead;

        float targetGameplay = playerDead ? 0f : (bossActive ? 0f : gameplayVolume);
        float targetBoss = (playerDead || !bossActive) ? 0f : bossVolume;

        float step = fadeSpeed * Time.unscaledDeltaTime;
        if (gameplaySource != null)
            gameplaySource.volume = Mathf.MoveTowards(gameplaySource.volume, targetGameplay, step);
        if (bossSource != null)
            bossSource.volume = Mathf.MoveTowards(bossSource.volume, targetBoss, step);
    }
}
