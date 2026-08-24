using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class GameUIController : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private DemoObjectiveManager demoObjectiveManager;

    [Header("Health UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthValueText;

    [Header("Stamina UI")]
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TMP_Text staminaValueText;

    [Header("Objective UI")]
    [SerializeField] private TMP_Text objectiveText;

    [Header("Loadout UI")]
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text shieldText;

    [Header("Controls Overlay")]
    [SerializeField] private CanvasGroup controlsOverlay;
    [SerializeField] private KeyCode controlsKey = KeyCode.F1;
    [SerializeField] private float controlsInitialDuration = 7f;
    [SerializeField] private float controlsFadeDuration = 0.5f;
    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [Header("Death Screen")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private float deathScreenDelay = 0.6f;
    [Header("Completion Screen")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TMP_Text completionStatsText;
    [SerializeField] private TMP_Text worldStateText;
    [SerializeField] private TMP_Text worldDescriptionText;
    [SerializeField] private TMP_Text bossProfileText;
    [SerializeField] private float completionScreenDelay = 0.5f;

private Coroutine completionRoutine;
    private bool gameEnded;
    private Coroutine deathRoutine;
    public static bool IsPaused { get; private set; }

    private Coroutine controlsRoutine;
    private bool controlsVisible;   
        private void Awake()
        {
            FindMissingReferences();
        }
    [SerializeField]
    private PlayerPerformanceTracker performanceTracker;

    [SerializeField]
    private WorldAdaptationManager worldAdaptationManager;

    [SerializeField]
private BossController bossController;
    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        gameEnded = false;


        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        RefreshUI();

        ShowControlsTemporarily();
    }

    private void Update()
    {
        RefreshUI();
        if (!gameEnded &&
            demoObjectiveManager != null &&
            demoObjectiveManager.IsDemoCompleted)
        {
            BeginCompletionSequence();
            return;
        }
        if (!gameEnded &&
            playerHealth != null &&
            playerHealth.IsDead)
        {
            BeginDeathSequence();
            return;
        }

        if (gameEnded)
            return;

        if (Input.GetKeyDown(pauseKey))
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }

            return;
        }

        if (Input.GetKeyDown(controlsKey))
        {
            ToggleControlsOverlay();
        }
    }

    private void FindMissingReferences()
    {
        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }

        if (playerStamina == null)
        {
            playerStamina =
                FindFirstObjectByType<PlayerStamina>();
        }

        if (playerEquipment == null)
        {
            playerEquipment =
                FindFirstObjectByType<PlayerEquipment>();
        }

        if (demoObjectiveManager == null)
        {
            demoObjectiveManager =
                FindFirstObjectByType<DemoObjectiveManager>();
        }
        if (performanceTracker == null)
        {
            performanceTracker =
                FindFirstObjectByType<PlayerPerformanceTracker>();
        }

        if (worldAdaptationManager == null)
        {
            worldAdaptationManager =
                FindFirstObjectByType<WorldAdaptationManager>();
        }

        if (bossController == null)
        {
            bossController =
                FindFirstObjectByType<BossController>();
        }
    }

    private void RefreshUI()
    {
        RefreshHealth();
        RefreshStamina();
        RefreshObjective();
        RefreshLoadout();
    }

    private void RefreshHealth()
    {
        if (playerHealth == null)
            return;

        float normalizedHealth =
            playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth /
                  playerHealth.MaxHealth
                : 0f;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount =
                Mathf.Clamp01(normalizedHealth);
        }

        if (healthValueText != null)
        {
            healthValueText.text =
                $"{playerHealth.CurrentHealth} / " +
                $"{playerHealth.MaxHealth}";
        }
    }

    private void RefreshStamina()
    {
        if (playerStamina == null)
            return;

        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount =
                playerStamina.NormalizedStamina;
        }

        if (staminaValueText != null)
        {
            staminaValueText.text =
                $"{playerStamina.CurrentStamina:0} / " +
                $"{playerStamina.MaxStamina:0}";
        }
    }

    private void RefreshObjective()
    {
        if (objectiveText == null ||
            demoObjectiveManager == null)
        {
            return;
        }

        objectiveText.text =
            demoObjectiveManager.CurrentObjective;
    }

    private void RefreshLoadout()
    {
        if (playerEquipment == null)
            return;

        if (weaponText != null)
        {
            WeaponData weapon =
                playerEquipment.EquippedWeapon;

            weaponText.text =
                weapon != null
                    ? weapon.WeaponName.ToUpper()
                    : "NO WEAPON";
        }

        if (shieldText != null)
        {
            ShieldData shield =
                playerEquipment.EquippedShield;

            shieldText.text =
                shield != null
                    ? shield.ShieldName
                    : "No Shield";
        }
    }
    private void ShowControlsTemporarily()
{
    if (controlsOverlay == null)
        return;

    if (controlsRoutine != null)
    {
        StopCoroutine(controlsRoutine);
    }

    controlsRoutine =
        StartCoroutine(
            ShowControlsRoutine()
        );
}

private IEnumerator ShowControlsRoutine()
{
    SetControlsVisible(true);

    yield return new WaitForSecondsRealtime(
        controlsInitialDuration
    );

    yield return FadeControls(
        1f,
        0f
    );

    controlsVisible = false;
    controlsOverlay.interactable = false;
    controlsOverlay.blocksRaycasts = false;

    controlsRoutine = null;
}

private void ToggleControlsOverlay()
{
    if (controlsOverlay == null)
        return;

    if (controlsRoutine != null)
    {
        StopCoroutine(controlsRoutine);
        controlsRoutine = null;
    }

    SetControlsVisible(
        !controlsVisible
    );
}

private void SetControlsVisible(
    bool visible)
{
    controlsVisible = visible;

    controlsOverlay.alpha =
        visible ? 1f : 0f;

    controlsOverlay.interactable =
        false;

    controlsOverlay.blocksRaycasts =
        false;
}

private IEnumerator FadeControls(
    float from,
    float to)
{
    float elapsed = 0f;

    controlsOverlay.alpha = from;

    while (elapsed < controlsFadeDuration)
    {
        elapsed +=
            Time.unscaledDeltaTime;

        float t =
            controlsFadeDuration > 0f
                ? elapsed /
                  controlsFadeDuration
                : 1f;

        controlsOverlay.alpha =
            Mathf.Lerp(
                from,
                to,
                t
            );

        yield return null;
    }

    controlsOverlay.alpha = to;
}
public void PauseGame()
{
    if (IsPaused)
        return;

    IsPaused = true;

    if (controlsRoutine != null)
    {
        StopCoroutine(controlsRoutine);
        controlsRoutine = null;
    }

    SetControlsVisible(false);

    if (pausePanel != null)
    {
        pausePanel.SetActive(true);
    }

    Time.timeScale = 0f;

    Cursor.lockState =
        CursorLockMode.None;

    Cursor.visible = true;

    Debug.Log(
        "[GameUI] Game paused."
    );
}

public void ResumeGame()
{
    if (!IsPaused)
        return;

    IsPaused = false;

    SetControlsVisible(false);

    if (pausePanel != null)
    {
        pausePanel.SetActive(false);
    }

    Time.timeScale = 1f;

    Cursor.lockState =
        CursorLockMode.Locked;

    Cursor.visible = false;

    Debug.Log(
        "[GameUI] Game resumed."
    );
}

public void ShowPauseControls()
{
    if (controlsOverlay == null)
        return;

    /*
     * PausePanel is later in the Canvas hierarchy,
     * so move the controls overlay to the front.
     */
    controlsOverlay.transform.SetAsLastSibling();

    ToggleControlsOverlay();
}

public void RestartSlice()
{
    PrepareForSceneChange();

    SceneManager.LoadScene(
        SceneManager.GetActiveScene().buildIndex
    );
}

public void ReturnToMainMenu()
{
    PrepareForSceneChange();

    SceneManager.LoadScene(
        mainMenuSceneName
    );
}

public void QuitGame()
{
    PrepareForSceneChange();

    Debug.Log(
        "[GameUI] Quit requested."
    );

#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying =
        false;
#else
    Application.Quit();
#endif
}

private void PrepareForSceneChange()
{
    IsPaused = false;

    Time.timeScale = 1f;

    Cursor.lockState =
        CursorLockMode.None;

    Cursor.visible = true;
}

private void BeginDeathSequence()
{
    if (gameEnded)
        return;

    gameEnded = true;

    /*
     * Reuse the pause flag so movement, attacks
     * and camera input are immediately blocked.
     */
    IsPaused = true;

    if (controlsRoutine != null)
    {
        StopCoroutine(controlsRoutine);
        controlsRoutine = null;
    }

    SetControlsVisible(false);

    if (pausePanel != null)
    {
        pausePanel.SetActive(false);
    }

    Time.timeScale = 0f;

    deathRoutine =
        StartCoroutine(
            ShowDeathScreenRoutine()
        );
}

private IEnumerator ShowDeathScreenRoutine()
{
    yield return new WaitForSecondsRealtime(
        deathScreenDelay
    );

    if (deathPanel != null)
    {
        deathPanel.SetActive(true);
    }

    Cursor.lockState =
        CursorLockMode.None;

    Cursor.visible = true;

    deathRoutine = null;

    Debug.Log(
        "[GameUI] Death screen shown."
    );
}
private void BeginCompletionSequence()
{
    if (gameEnded)
        return;

    gameEnded = true;
    IsPaused = true;

    if (controlsRoutine != null)
    {
        StopCoroutine(controlsRoutine);
        controlsRoutine = null;
    }

    SetControlsVisible(false);

    if (pausePanel != null)
    {
        pausePanel.SetActive(false);
    }

    Time.timeScale = 0f;

    completionRoutine =
        StartCoroutine(
            ShowCompletionScreenRoutine()
        );
}

private IEnumerator ShowCompletionScreenRoutine()
{
    yield return new WaitForSecondsRealtime(
        completionScreenDelay
    );

    PopulateCompletionReport();

    if (completionPanel != null)
    {
        completionPanel.SetActive(true);
    }

    Cursor.lockState =
        CursorLockMode.None;

    Cursor.visible = true;

    completionRoutine = null;

    Debug.Log(
        "[GameUI] Vertical slice complete."
    );
}
private void PopulateCompletionReport()
{
    if (performanceTracker != null &&
        completionStatsText != null)
    {
        completionStatsText.text =
            $"Enemies Defeated        " +
            $"{performanceTracker.EnemiesKilled}\n" +

            $"Damage Taken             " +
            $"{performanceTracker.DamageTaken}\n" +

            $"Completion Time          " +
            $"{FormatTime(performanceTracker.TimeAlive)}\n" +

            $"Adaptive Score           " +
            $"{performanceTracker.GetPerformanceScore():0.0}";
    }

    if (worldAdaptationManager != null)
    {
        WorldAdaptationManager.WorldState state =
            worldAdaptationManager.CurrentState;

        if (worldStateText != null)
        {
            worldStateText.text =
                state.ToString().ToUpper();
        }

        if (worldDescriptionText != null)
        {
            worldDescriptionText.text =
                GetWorldResponseDescription(state);
        }
    }

    if (bossProfileText != null)
    {
        bossProfileText.text =
            bossController != null
                ? $"Boss Response: " +
                  $"{bossController.CurrentProfile}"
                : "";
    }
}
private string FormatTime(float seconds)
{
    int totalSeconds =
        Mathf.Max(
            0,
            Mathf.FloorToInt(seconds)
        );

    int minutes =
        totalSeconds / 60;

    int remainingSeconds =
        totalSeconds % 60;

    return
        $"{minutes:00}:{remainingSeconds:00}";
}
private string GetWorldResponseDescription(
    WorldAdaptationManager.WorldState state)
{
    switch (state)
    {
        case WorldAdaptationManager.WorldState.Stable:
            return
                "The adaptive system shifted toward " +
                "a more forgiving world response.";

        case WorldAdaptationManager.WorldState.Decaying:
            return
                "The adaptive system shifted toward " +
                "a more aggressive world response.";

        case WorldAdaptationManager.WorldState.Balanced:
        default:
            return
                "Your performance kept the adaptive " +
                "world response in a balanced state.";
    }
}
}