using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.Audio;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Profile;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class SystemsTests
    {
        [Test]
        public void Settings_SanitizeClampsCorruptValues()
        {
            var settings = new SettingsSaveData
            {
                MasterVolume = 7f,
                MusicVolume = float.NaN,
                SfxVolume = -1f,
                QualityLevel = 99,
                TargetFrameRate = 0,
                Language = "",
            };
            settings.Sanitize(qualityLevelCount: 3);

            Assert.AreEqual(1f, settings.MasterVolume);
            Assert.AreEqual(1f, settings.MusicVolume);
            Assert.AreEqual(0f, settings.SfxVolume);
            Assert.AreEqual(2, settings.QualityLevel);
            Assert.AreEqual(SettingsSaveData.DefaultFrameRate, settings.TargetFrameRate);
            Assert.AreEqual("en", settings.Language);
            Assert.AreEqual(SettingsSaveData.BatterySaverFrameRate, SettingsSaveData.NormalizeFrameRate(24));
        }

        [Test]
        public void SfxThrottle_CooldownAndVoiceCap()
        {
            var throttle = new SfxThrottle();
            Assert.IsTrue(throttle.TryPlay("hit", 0f, 0.1f, 0, 3));
            Assert.IsFalse(throttle.TryPlay("hit", 0.05f, 0.1f, 0, 3), "inside cooldown");
            Assert.IsTrue(throttle.TryPlay("hit", 0.2f, 0.1f, 0, 3));
            Assert.IsFalse(throttle.TryPlay("hit", 5f, 0f, 3, 3), "voice cap reached");
            Assert.IsTrue(throttle.TryPlay("other", 0.05f, 0.1f, 0, 3), "cooldowns are per cue");
        }

        [Test]
        public void MusicDirector_TrackPerScene()
        {
            Assert.AreEqual(NinjaVillage.Core.Audio.AudioCueIds.MusicMenu, MusicDirector.TrackForScene("MainMenu"));
            Assert.AreEqual(NinjaVillage.Core.Audio.AudioCueIds.MusicBattle, MusicDirector.TrackForScene("Battle"));
            Assert.IsNull(MusicDirector.TrackForScene("SomeTestScene"));
        }

        [Test]
        public void ProfileName_Sanitized()
        {
            Assert.AreEqual("Ninja", ProfileScreen.SanitizeName("   "));
            Assert.AreEqual("bbold/b", ProfileScreen.SanitizeName("<b>bold</b>"), "angle brackets are stripped so names can't inject rich text");
            Assert.AreEqual("Kai", ProfileScreen.SanitizeName("  Kai  "));
            Assert.AreEqual(ProfileScreen.MaxNameLength, ProfileScreen.SanitizeName(new string('x', 50)).Length);
            Assert.IsFalse(ProfileScreen.SanitizeName("<color=red>Hax</color>").Contains("<"));
        }

        [Test]
        public void Villagers_GrowWithVillage()
        {
            Assert.AreEqual(2, VillageMap.VillagerCountFor(0, 2, 14));
            Assert.AreEqual(5, VillageMap.VillagerCountFor(9, 2, 14));
            Assert.AreEqual(14, VillageMap.VillagerCountFor(500, 2, 14));
        }
    }
}
