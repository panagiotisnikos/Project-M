using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "Prototype_01";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private OptionsMenuUI optionsMenu;

    /// <summary>True if a save exists to continue from. Not yet bound to any UI -
    /// a future "Continue" control can gate its interactable/visibility on this
    /// instead of always showing alongside "New Game".</summary>
    public bool HasSaveToContinue => SaveSystem.HasSave;

    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowMainMenu();
    }

    /// <summary>Loads the gameplay scene. If a save exists, GameSaveController resumes it
    /// automatically - this button doubles as "Continue" with no separate state to track.</summary>
    public void PlayGame()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError(
                "[MainMenu] Gameplay scene name is missing."
            );

            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>Wipes any existing save before loading, for a deliberate fresh start.
    /// Not yet wired to a button - hook this up when a "New Game" control is added.</summary>
    public void NewGame()
    {
        SaveSystem.DeleteSave();
        PlayGame();
    }

    public void ShowControls()
    {
        SetPanelStates(
            false,
            true,
            false
        );
    }

    public void ShowCredits()
    {
        SetPanelStates(
            false,
            false,
            true
        );
    }

    public void ShowMainMenu()
    {
        SetPanelStates(
            true,
            false,
            false
        );
    }

    public void ShowOptions()
    {
        SetPanelStates(false, false, false);
        if (optionsMenu != null) optionsMenu.Open(ShowMainMenu);
    }

    public void QuitGame()
    {
        DevLog.Log("[MainMenu] Quit requested.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPanelStates(
        bool showMain,
        bool showControls,
        bool showCredits)
    {
        if (mainPanel != null)
            mainPanel.SetActive(showMain);

        if (controlsPanel != null)
            controlsPanel.SetActive(showControls);

        if (creditsPanel != null)
            creditsPanel.SetActive(showCredits);
    }
}