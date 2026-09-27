using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Systems.Save;
using NUnit.Framework;
using UnityEngine;

namespace NinjaVillage.Tests
{
    /// <summary>Battle zoom: its levels, and spawns staying off-screen however wide the view.</summary>
    public class BattleZoomTests
    {
        [Test]
        public void Levels_StartAtTheNormalView_GetWider_AndWrap()
        {
            var scales = SettingsSaveData.BattleZoomScales;
            Assert.AreEqual(1f, scales[0], "Normal is the scene's own view");
            for (int i = 1; i < scales.Length; i++) Assert.Greater(scales[i], scales[i - 1]);
            Assert.AreEqual(scales.Length, SettingsSaveData.BattleZoomNames.Length);
            Assert.AreEqual(1, SettingsSaveData.NextBattleZoom(0));
            Assert.AreEqual(0, SettingsSaveData.NextBattleZoom(scales.Length - 1), "Widest wraps back to Normal");

            var settings = new SettingsSaveData { BattleZoom = 42 };
            settings.Sanitize(3);
            Assert.AreEqual(scales.Length - 1, settings.BattleZoom, "a corrupt level is clamped");
            settings.BattleZoom = -3;
            settings.Sanitize(3);
            Assert.AreEqual(0, settings.BattleZoom);
        }

        private GameObject _camera, _spawner;

        [TearDown]
        public void TearDown()
        {
            if (_camera != null) Object.DestroyImmediate(_camera);
            if (_spawner != null) Object.DestroyImmediate(_spawner);
        }

        [Test]
        public void Spawns_LandOutsideTheView_AtEveryZoom()
        {
            _camera = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = _camera.AddComponent<Camera>();
            cam.orthographic = true;
            cam.aspect = 1080f / 2340f; // a portrait phone
            var spawner = (_spawner = new GameObject("Spawner")).AddComponent<SpawnManager>();
            spawner.ViewCamera = cam;
            Random.InitState(7);

            foreach (float scale in SettingsSaveData.BattleZoomScales)
            {
                cam.orthographicSize = 6f * scale;
                float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
                for (int i = 0; i < 1000; i++)
                {
                    Vector2 p = spawner.GetSpawnPositionAroundPlayer(); // the player stands at the origin here
                    Assert.IsTrue(Mathf.Abs(p.x) > halfW || Mathf.Abs(p.y) > halfH, $"spawned on screen at {p} (zoom ×{scale})");
                    Assert.GreaterOrEqual(p.magnitude, 8f - 1e-3f, "never closer than before");
                }
            }
            Assert.AreEqual(9f + 1.5f, SpawnManager.OffScreenDistance(Vector2.up, cam), 1e-3f, "Widest: the top edge (9) plus the margin");
        }
    }
}
