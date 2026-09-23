using System;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// Player-facing options (EPIC 17 Settings). Applied by the Settings / Audio systems
    /// (<c>SettingsService</c>, <c>AudioManager</c>). The static rules below are the single
    /// place values are validated, so a hand-edited or corrupted save can never push e.g. a
    /// volume of 7 or a frame rate of 0 into the engine.
    /// </summary>
    [Serializable]
    public class SettingsSaveData
    {
        public const int BatterySaverFrameRate = 30;
        public const int DefaultFrameRate = 60;

        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool Vibration = true;
        /// <summary>Index into QualitySettings.names; -1 = leave the platform default.</summary>
        public int QualityLevel = -1;
        /// <summary>30 saves battery on mid-range Android (EPIC 23); 60 for smoother play.</summary>
        public int TargetFrameRate = 60;
        public bool ShowDamageNumbers = true;
        public string Language = "en";

        public bool BatterySaver => TargetFrameRate == BatterySaverFrameRate;

        /// <summary>Clamps every field into its valid range. <paramref name="qualityLevelCount"/> = QualitySettings.names.Length.</summary>
        public void Sanitize(int qualityLevelCount)
        {
            MasterVolume = ClampVolume(MasterVolume);
            MusicVolume = ClampVolume(MusicVolume);
            SfxVolume = ClampVolume(SfxVolume);
            QualityLevel = ClampQualityLevel(QualityLevel, qualityLevelCount);
            TargetFrameRate = NormalizeFrameRate(TargetFrameRate);
            if (string.IsNullOrEmpty(Language)) Language = "en";
        }

        /// <summary>0..1; NaN (corrupt save) falls back to full volume.</summary>
        public static float ClampVolume(float volume)
        {
            if (float.IsNaN(volume)) return 1f;
            if (volume < 0f) return 0f;
            return volume > 1f ? 1f : volume;
        }

        /// <summary>Only 30 (battery saver) and 60 are offered; anything else snaps to the nearer one.</summary>
        public static int NormalizeFrameRate(int frameRate)
        {
            if (frameRate <= 0) return DefaultFrameRate;
            return frameRate < 45 ? BatterySaverFrameRate : DefaultFrameRate;
        }

        /// <summary>-1 (platform default) stays -1; out-of-range levels clamp to the highest valid one.</summary>
        public static int ClampQualityLevel(int level, int qualityLevelCount)
        {
            if (level < 0 || qualityLevelCount <= 0) return -1;
            return Math.Min(level, qualityLevelCount - 1);
        }
    }
}
