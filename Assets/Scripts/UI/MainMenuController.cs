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

    private void Start()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowMainMenu();
    }

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

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quit requested.");

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