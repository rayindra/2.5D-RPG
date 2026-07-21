using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private const string VOLUME_KEY = "SettingVolume";
    private const string QUALITY_KEY = "SettingQuality";
    private const string FULLSCREEN_KEY = "SettingFullscreen";

    private void Start()
    {
        LoadSettings();

        volumeSlider.onValueChanged.AddListener(SetVolume);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    private void LoadSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        int savedQuality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
        bool savedFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;

        volumeSlider.value = savedVolume;
        qualityDropdown.value = savedQuality;
        fullscreenToggle.isOn = savedFullscreen;

        ApplyVolume(savedVolume);
        ApplyQuality(savedQuality);
        ApplyFullscreen(savedFullscreen);
    }

    public void SetVolume(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
    }

    public void SetQuality(int index)
    {
        ApplyQuality(index);
        PlayerPrefs.SetInt(QUALITY_KEY, index);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        ApplyFullscreen(isFullscreen);
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = value;
    }

    private void ApplyQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    private void ApplyFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
