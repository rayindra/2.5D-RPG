using UnityEngine;

// Taruh script ini di 1 GameObject kosong di TIAP scene yang punya musik latar sendiri
// (misal: MainMenuScene, OverworldScene, BattleScene). Beda scene = beda AudioClip.
public class SceneMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip sceneMusic;
    [SerializeField] private bool loop = true;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(sceneMusic, loop);
        }
    }
}
