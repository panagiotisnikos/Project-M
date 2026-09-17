using UnityEngine;

/// <summary>
/// Plays the gameplay music as sparse, ambient swells (Valheim-style: mostly
/// quiet, an occasional low musical phrase rather than a constant loud loop),
/// and crossfades to a more continuous boss loop while the fight is active.
/// Polls BossCombat.FightActive so it needs no hooks into the boss code. Both
/// tracks are scaled by GameAudioSettings.Master * .Music - the options menu's
/// music slider. One instance per gameplay scene.
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

    [Header("Mix (peak volume during a swell/the fight)")]
    [Range(0f, 1f)] [SerializeField] private float gameplayVolume = 0.22f;
    [Range(0f, 1f)] [SerializeField] private float bossVolume = 0.35f;
    [Tooltip("Volume units per second for the boss crossfade - kept snappy, combat starting should read as immediate.")]
    [SerializeField] private float bossFadeSpeed = 0.6f;

    [Header("Ambience (Valheim-style: soft swells over a quiet floor - never fully silent)")]
    [Tooltip("Fraction of gameplayVolume kept audible during the quiet phase. 0 would read as " +
             "the music having stopped; a floor keeps a faint constant presence.")]
    [Range(0f, 1f)] [SerializeField] private float quietFloorFraction = 0.35f;
    [Tooltip("Seconds to ease between the quiet floor and a full swell (and back). This is a fixed " +
             "duration (via SmoothDamp), not a rate, so the breathe-in/out always takes about this " +
             "long regardless of how big the jump is - that's the 'smoother loop transition'.")]
    [SerializeField] private float ambientSmoothTime = 4f;
    [SerializeField] private float swellDurationMin = 20f;
    [SerializeField] private float swellDurationMax = 35f;
    [SerializeField] private float quietDurationMin = 18f;
    [SerializeField] private float quietDurationMax = 32f;

    private bool swelling = true;
    private float phaseEndTime;
    private float ambientLevel = 1f; // 0 = quiet floor, 1 = full swell
    private float ambientLevelVelocity;

    private void Awake()
    {
        if (bossCombat == null) bossCombat = FindFirstObjectByType<BossCombat>();
        if (bossHealth == null) bossHealth = FindFirstObjectByType<BossHealth>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();

        Prepare(gameplaySource, 0f);
        Prepare(bossSource, 0f);

        swelling = true;
        phaseEndTime = Time.time + Random.Range(swellDurationMin, swellDurationMax);
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

        // Inside the Refuge the music settles to its quiet floor and stays there -
        // one more small signal (with lighting/fence/fire) that this place is
        // calm, not just another patch of ground the swell cycle ignores.
        bool inRefuge = RefugeZone.Main != null && RefugeZone.Main.IsPlayerInside;
        if (inRefuge) swelling = false;

        // The ambient swell/quiet cycle only runs during ordinary exploration -
        // a boss fight, death, or the refuge holds/overrides it rather than
        // ticking underneath.
        if (!bossActive && !playerDead && !inRefuge && Time.time >= phaseEndTime)
        {
            swelling = !swelling;
            phaseEndTime = Time.time + (swelling
                ? Random.Range(swellDurationMin, swellDurationMax)
                : Random.Range(quietDurationMin, quietDurationMax));
        }

        float mix = GameAudioSettings.Master * GameAudioSettings.Music;

        // Smoothly ease between the quiet floor and a full swell over a fixed duration
        // (SmoothDamp), so the breathing is always gentle regardless of how far it has to move.
        float ambientTarget = swelling ? 1f : 0f;
        ambientLevel = Mathf.SmoothDamp(
            ambientLevel, ambientTarget, ref ambientLevelVelocity, ambientSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        float ambientVolume = Mathf.Lerp(gameplayVolume * quietFloorFraction, gameplayVolume, ambientLevel);
        float targetGameplay = (playerDead || bossActive) ? 0f : ambientVolume * mix;
        float targetBoss = (playerDead || !bossActive) ? 0f : bossVolume * mix;

        float bossStep = bossFadeSpeed * Time.unscaledDeltaTime;

        if (gameplaySource != null)
        {
            gameplaySource.volume = (bossActive || playerDead)
                // A fight starting (or dying) should still cut in/out snappily, not breathe.
                ? Mathf.MoveTowards(gameplaySource.volume, targetGameplay, bossStep)
                : targetGameplay;
        }

        if (bossSource != null)
            bossSource.volume = Mathf.MoveTowards(bossSource.volume, targetBoss, bossStep);
    }
}
