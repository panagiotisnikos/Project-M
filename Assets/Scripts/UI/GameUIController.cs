using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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

    public static bool IsPaused { get; private set; }

    private Coroutine controlsRoutine;
    private bool controlsVisible;   
        private void Awake()
        {
            FindMissingReferences();
        }

    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
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
}