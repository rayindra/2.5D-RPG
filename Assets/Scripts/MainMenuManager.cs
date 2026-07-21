using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene To Load")]
    [SerializeField] private string overworldSceneName = "OverworldScene";

    // Dipanggil dari tombol "Start Game"
    public void StartGame()
    {
        SceneManager.LoadScene(overworldSceneName);
    }

    // Dipanggil dari tombol "Settings"
    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // Dipanggil dari tombol "Back" di dalam panel Settings
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // Dipanggil dari tombol "Exit"
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
