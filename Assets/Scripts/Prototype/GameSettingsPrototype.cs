using System;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(6500)]
    public sealed class GameSettingsPrototype : MonoBehaviour
    {
        private const string Prefix = "PeanutWarrior.Settings.";

        private float bgmVolume = 0.8f;
        private float sfxVolume = 0.9f;
        private bool vibrationEnabled = true;
        private int targetFrameRate = 60;
        private bool powerSavingMode;
        private string lastMessage = "설정 준비";

        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public bool VibrationEnabled => vibrationEnabled;
        public int TargetFrameRate => targetFrameRate;
        public bool PowerSavingMode => powerSavingMode;
        public string LastMessage => lastMessage;

        public event Action SettingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<GameSettingsPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorGameSettings");
            DontDestroyOnLoad(root);
            root.AddComponent<GameSettingsPrototype>();
        }

        private void Awake()
        {
            Load();
            Apply();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            lastMessage = $"BGM 음량 {Mathf.RoundToInt(bgmVolume * 100f)}%";
            SaveAndApply();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            lastMessage = $"효과음 음량 {Mathf.RoundToInt(sfxVolume * 100f)}%";
            SaveAndApply();
        }

        public void SetVibration(bool enabled)
        {
            vibrationEnabled = enabled;
            lastMessage = enabled ? "진동 ON" : "진동 OFF";
            SaveAndApply();
        }

        public void SetFrameRate(int value)
        {
            targetFrameRate = value <= 30 ? 30 : 60;
            powerSavingMode = targetFrameRate <= 30;
            lastMessage = powerSavingMode ? "30 FPS 절전 모드" : "60 FPS 일반 모드";
            SaveAndApply();
        }

        public void ToggleVibration()
        {
            SetVibration(!vibrationEnabled);
        }

        public void SaveNow()
        {
            Save();
        }

        private void SaveAndApply()
        {
            Apply();
            Save();
            SettingsChanged?.Invoke();
        }

        private void Apply()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            AudioListener.volume = Mathf.Clamp01(Mathf.Max(bgmVolume, sfxVolume));
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(Prefix + "BgmVolume", bgmVolume);
            PlayerPrefs.SetFloat(Prefix + "SfxVolume", sfxVolume);
            PlayerPrefs.SetInt(Prefix + "Vibration", vibrationEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "FrameRate", targetFrameRate);
            PlayerPrefs.SetInt(Prefix + "PowerSaving", powerSavingMode ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "BgmVolume", 0.8f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "SfxVolume", 0.9f));
            vibrationEnabled = PlayerPrefs.GetInt(Prefix + "Vibration", 1) == 1;
            targetFrameRate = PlayerPrefs.GetInt(Prefix + "FrameRate", 60) <= 30 ? 30 : 60;
            powerSavingMode = PlayerPrefs.GetInt(Prefix + "PowerSaving", targetFrameRate <= 30 ? 1 : 0) == 1;
            if (powerSavingMode) targetFrameRate = 30;
        }
    }
}
