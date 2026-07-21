using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private string battleSceneName = "BattleScene";

    // Dipanggil dari tombol "Retry" di panel Game Over
    // Catatan: party tidak direset di sini, jadi pastikan health party
    // di-reset dulu (lihat PartyManager.ReviveParty) sebelum reload battle,
    // atau arahkan Retry ke OverworldScene sesuai desain game kamu.
    public void OnRetryButton()
    {
        PartyManager partyManager = GameObject.FindFirstObjectByType<PartyManager>();
        if (partyManager != null)
        {
            partyManager.ReviveParty();
        }
        SceneManager.LoadScene(battleSceneName);
    }

    // Dipanggil dari tombol "Main Menu" di panel Game Over
    public void OnMainMenuButton()
    {
        PartyManager partyManager = GameObject.FindFirstObjectByType<PartyManager>();
        if (partyManager != null)
        {
            partyManager.ReviveParty();
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Dipanggil dari tombol "Quit" di panel Game Over
    public void OnQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
