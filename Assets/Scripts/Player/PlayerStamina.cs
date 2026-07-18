using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenerationPerSecond = 22f;
    [SerializeField] private float regenerationDelay = 0.75f;

    [Header("Blocking Regeneration")]
    [Range(0f, 1f)]
    [SerializeField] private float blockingRegenerationMultiplier = 0.25f;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Runtime")]
    [SerializeField] private float currentStamina;

    private float regenerationResumeTime;

    public float CurrentStamina =>
        currentStamina;

    public float MaxStamina =>
        maxStamina;

    public float NormalizedStamina =>
        maxStamina > 0f
            ? currentStamina / maxStamina
            : 0f;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        currentStamina = maxStamina;
    }

    private void Update()
    {
        RegenerateStamina();
    }

    public bool TrySpend(float amount)
    {
        amount = Mathf.Max(0f, amount);

        if (amount <= 0f)
            return true;

        if (currentStamina < amount)
            return false;

        currentStamina -= amount;

        currentStamina =
            Mathf.Max(
                currentStamina,
                0f
            );

        regenerationResumeTime =
            Time.time + regenerationDelay;

        return true;
    }

    public bool HasEnough(float amount)
    {
        return currentStamina >=
               Mathf.Max(0f, amount);
    }

    private void RegenerateStamina()
    {
        if (currentStamina >= maxStamina)
            return;

        if (Time.time < regenerationResumeTime)
            return;

        float regenerationMultiplier = 1f;

        if (playerMovement != null &&
            playerMovement.IsBlocking)
        {
            regenerationMultiplier =
                blockingRegenerationMultiplier;
        }

        float regenerationAmount =
            regenerationPerSecond *
            regenerationMultiplier *
            Time.deltaTime;

        currentStamina =
            Mathf.MoveTowards(
                currentStamina,
                maxStamina,
                regenerationAmount
            );
    }

    private void OnValidate()
    {
        maxStamina =
            Mathf.Max(0f, maxStamina);

        regenerationPerSecond =
            Mathf.Max(
                0f,
                regenerationPerSecond
            );

        regenerationDelay =
            Mathf.Max(
                0f,
                regenerationDelay
            );

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );
    }
}