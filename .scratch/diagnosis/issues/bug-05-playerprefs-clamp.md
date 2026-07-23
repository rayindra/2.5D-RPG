# BUG #5 — SettingsManager PlayerPrefs (volume + quality) tidak di-clamp

**Severity:** MEDIUM-HIGH (bisa crash on tampered registry)
**File:** `Assets/Scripts/SettingsManager.cs:27-65`
**Blocks:** none (independen dari battle bugs)
**Test mode:** EditMode (PlayerPrefs survival antar test perlu SetUp/TearDown untuk clear)

## Root cause

```csharp
float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);   // L27 - no Clamp01
int savedQuality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());  // L28 - no Clamp
...
private void ApplyQuality(int index)
{
    QualitySettings.SetQualityLevel(index);   // out-of-range → ArgumentOutOfRangeException
}
```

`QualitySettings.asset` mendefinisikan hanya 2 level: `Mobile=0`, `PC=1`. Jadi `SetQualityLevel(9999)` akan crash.
Volume tanpa `Mathf.Clamp01` → `AudioListener.volume = -1f` atau `999f` → silent broken UX.

User dengan akses ke registry (Windows: `HK_CURRENT_USER\Software\<Company>\<Product>`) atau sysadmin bisa lempar out-of-range value ke trigger crash.

## Repro test

```csharp
[Test]
public void LoadSettings_OutOfRangeQuality_ClampsInsteadOfCrash()
{
    PlayerPrefs.SetInt("SettingQuality", 9999);
    PlayerPrefs.SetFloat("SettingVolume", 999f);
    var mgr = new GameObject().AddComponent<SettingsManager>();
    InvokeMethod(mgr, "LoadSettings");
    Assert.AreEqual(1, QualitySettings.GetQualityLevel());   // clamped ke max valid
    Assert.AreEqual(1f, AudioListener.volume, 0.001f);         // Clamp01
}
```

## Plan

**Phase 3:** H1 (winner) missing `Mathf.Clamp` di ApplyVolume + `Mathf.Clamp(index, 0, QualitySettings.names.Length - 1)` di ApplyQuality.
**Phase 5:** Fix di `Apply*` methods (caller-side safe, tidak cukup hanya di LoadSettings karena UI slider bisa paksa value abnormal via code):

```csharp
private void ApplyVolume(float value)
{
    AudioListener.volume = Mathf.Clamp01(value);
}

private void ApplyQuality(int index)
{
    int clamped = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
    QualitySettings.SetQualityLevel(clamped);
}
```

**Phase 6:** Commit msg:
> `SettingsManager.Apply*: clamp volume/quality to safe range; prevent crash on tampered PlayerPrefs`.

Rekomendasi: tambah `OnApplicationQuit` → `PlayerPrefs.Save()` agar flush deterministik.
