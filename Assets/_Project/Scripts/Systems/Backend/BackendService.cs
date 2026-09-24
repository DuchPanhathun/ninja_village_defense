using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaVillage.Core.Config;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Backend
{
    /// <summary>
    /// Orchestrates the backend (EPIC 22 + EPIC 0/16 "Cloud Save"): picks Firebase on Android/iOS device
    /// builds and the offline provider everywhere else, then on startup:
    /// initialize → anonymous sign-in → trusted server time (GameClock, blocks clock-change cheating
    /// on dailies) → Remote Config into <see cref="RemoteValues"/> → cloud-save reconcile
    /// (<see cref="BackendRules.Decide"/>) → public profile and public village. Afterwards every local save
    /// is uploaded (and the village re-published when it changed), throttled, and immediately when the app is
    /// backgrounded. Created automatically; persistent.
    /// </summary>
    public class BackendService : MonoBehaviour
    {
        public const float UploadIntervalSeconds = 30f;

        public static BackendService Instance { get; private set; }
        public static IBackendProvider Provider { get; private set; }

        public BackendStatus Status { get; private set; } = BackendStatus.Offline;
        public string StatusMessage { get; private set; } = "Offline";
        public DateTime? LastCloudSyncUtc { get; private set; }

        private bool _uploadPending;
        private float _lastUploadTime = float.MinValue;
        private bool _uploading;
        private string _publishedVillage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[BackendService]");
            DontDestroyOnLoad(go);
            go.AddComponent<BackendService>();
        }

        private static IBackendProvider CreateProvider()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            return new FirebaseBackendProvider();
#else
            return new OfflineBackendProvider();
#endif
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Provider = CreateProvider();
        }

        private void OnEnable() => EventBus<SaveWrittenEvent>.SubscribePersistent(OnSaveWritten);
        private void OnDisable() => EventBus<SaveWrittenEvent>.UnsubscribePersistent(OnSaveWritten);

        private async void Start() => await ConnectAsync();

        /// <summary>Runs the full startup flow; safe to call again from "Sync now".</summary>
        public async Task ConnectAsync()
        {
            SetStatus(BackendStatus.Connecting, $"Connecting ({Provider.Name})...");
            try
            {
                if (!await Provider.InitializeAsync())
                {
                    SetStatus(BackendStatus.Offline, "Offline — progress is saved on this device");
                    return;
                }

                var user = await Provider.SignInAnonymouslyAsync();
                if (user == null)
                {
                    SetStatus(BackendStatus.Error, "Sign-in failed — playing offline");
                    return;
                }
                Provider.SetUserId(user.UserId);
                AnalyticsService.ApplyConsent();
                EventBus<SignedInEvent>.Raise(new SignedInEvent(user.UserId, user.IsAnonymous));

                var serverNow = await Provider.GetServerTimeAsync(user.UserId);
                if (serverNow.HasValue) GameClock.SetServerTime(serverNow.Value);

                var remote = await Provider.FetchRemoteConfigAsync(BackendRules.RemoteConfigDefaults());
                if (remote != null && remote.Count > 0) RemoteValues.Apply(remote);

                if (Provider.IsOnline && RemoteValues.GetBool("cloud_save_enabled", true))
                    await ReconcileCloudSaveAsync(user.UserId);

                var save = SaveService.Data;
                await Provider.WritePublicProfileAsync(user.UserId, save.Profile.DisplayName, BackendRules.Summarize(save));
                await PublishVillageAsync(user.UserId);

                SetStatus(Provider.IsOnline ? BackendStatus.Online : BackendStatus.Offline,
                    Provider.IsOnline ? "Online — progress backed up" : "Offline — progress is saved on this device");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                SetStatus(BackendStatus.Error, "Backend error — playing offline");
            }
        }

        private async Task ReconcileCloudSaveAsync(string userId)
        {
            var local = SaveService.Data;
            var cloud = await Provider.LoadSaveAsync(userId);
            var decision = BackendRules.Decide(local.LastSavedUtcTicks, BackendRules.Summarize(local), cloud);

            switch (decision)
            {
                case CloudSaveDecision.UseCloud:
                    var cloudSave = SaveSystem.FromJson(cloud.Json);
                    if (cloudSave != null)
                    {
                        SaveService.Replace(cloudSave);
                        Debug.Log("[Backend] Adopted the cloud save.");
                    }
                    break;
                case CloudSaveDecision.UseLocal:
                    await UploadAsync(userId);
                    break;
            }
            LastCloudSyncUtc = DateTime.UtcNow;
            EventBus<CloudSaveSyncedEvent>.Raise(new CloudSaveSyncedEvent(decision));
        }

        private void OnSaveWritten(SaveWrittenEvent evt) => _uploadPending = true;

        private void Update()
        {
            if (BackendRules.ShouldUpload(_uploadPending, Time.unscaledTime, _lastUploadTime, UploadIntervalSeconds, force: false))
                _ = UploadPendingAsync();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _uploadPending) _ = UploadPendingAsync();
        }

        private async Task UploadPendingAsync()
        {
            var user = Provider.CurrentUser;
            if (_uploading || user == null || !Provider.IsOnline) return;
            await UploadAsync(user.UserId);
        }

        private async Task UploadAsync(string userId)
        {
            if (_uploading) return;
            _uploading = true;
            _uploadPending = false;
            _lastUploadTime = Time.unscaledTime;
            try
            {
                var save = SaveService.Data;
                var snapshot = new CloudSaveSnapshot
                {
                    Json = SaveSystem.ToJson(save),
                    LastSavedUtcTicks = save.LastSavedUtcTicks,
                    Version = save.Version,
                    Progress = BackendRules.Summarize(save),
                };
                if (await Provider.WriteSaveAsync(userId, snapshot)) LastCloudSyncUtc = DateTime.UtcNow;
                else _uploadPending = true; // retry on the next interval
                await PublishVillageAsync(userId);
            }
            finally
            {
                _uploading = false;
            }
        }

        /// <summary>Publishes this player's village for visitors when it changed since the last publish.</summary>
        private async Task PublishVillageAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            var village = BackendRules.BuildPublicVillage(SaveService.Data, userId);
            string key = village.DisplayName + "|" + village.SnapshotJson;
            if (key == _publishedVillage) return;
            if (await Provider.WriteVillageAsync(village)) _publishedVillage = key;
        }

        /// <summary>Someone's published village (null when offline, unknown, or not published yet).</summary>
        public static Task<PublicVillage> LoadVillageAsync(string userId) =>
            Provider != null ? Provider.LoadVillageAsync(userId) : Task.FromResult<PublicVillage>(null);

        /// <summary>Recently active villages to visit, newest first.</summary>
        public static Task<List<PublicVillage>> ListVillagesAsync(int count) =>
            Provider != null ? Provider.ListVillagesAsync(count) : Task.FromResult(new List<PublicVillage>());

        /// <summary>Account screen "Link email": keeps the same uid, so the cloud save follows the player.</summary>
        public async Task<bool> LinkEmailAsync(string email, string password)
        {
            bool ok = await Provider.LinkEmailAsync(email, password);
            if (ok) SetStatus(Status, "Account linked to " + email);
            return ok;
        }

        private void SetStatus(BackendStatus status, string message)
        {
            Status = status;
            StatusMessage = message;
            EventBus<BackendStatusChangedEvent>.Raise(new BackendStatusChangedEvent(status, message));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
