using System;
using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// "Visit" from any screen: downloads the player's published village (<c>villages/{uid}</c>), hands its
    /// snapshot to <see cref="VillageVisit"/> and opens the Village scene read-only. The village HUD's back
    /// button brings you home again.
    /// </summary>
    public static class VillageVisitFlow
    {
        private static bool _busy;

        public static async void Visit(string userId, string displayName = null)
        {
            if (_busy || string.IsNullOrEmpty(userId)) return;
            _busy = true;
            try
            {
                Sfx.Play(AudioCueIds.UiClick);
                Toast($"Travelling to {(string.IsNullOrEmpty(displayName) ? "their" : displayName + "'s")} village...");
                var village = await BackendService.LoadVillageAsync(userId);
                var snapshot = Parse(village);
                if (snapshot == null)
                {
                    Toast("That ninja hasn't opened their village to visitors yet.");
                    return;
                }
                if (string.IsNullOrEmpty(snapshot.DisplayName)) snapshot.DisplayName = village.DisplayName ?? displayName;
                VillageVisit.Visit(snapshot);
                SceneLoader.LoadVillage();
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>The snapshot inside a published village, or null when missing or unreadable.</summary>
        public static VillageSnapshot Parse(PublicVillage village)
        {
            if (village == null || string.IsNullOrEmpty(village.SnapshotJson)) return null;
            try
            {
                var snapshot = JsonUtility.FromJson<VillageSnapshot>(village.SnapshotJson);
                if (snapshot == null) return null;
                snapshot.PlayerId = village.UserId;
                return snapshot;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Visit] Unreadable village for {village.UserId}: {e.Message}");
                return null;
            }
        }

        private static void Toast(string message)
        {
            if (UIScreenNavigator.Instance != null) UIScreenNavigator.Instance.Toast(message);
        }
    }
}
