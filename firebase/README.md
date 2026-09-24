# Firebase setup — Ninja Village Defense

The game talks to Firebase **only on Android/iOS builds**. In the Unity Editor it uses an offline
backend (`OfflineBackendProvider`), because the Firebase desktop native libraries are not committed.
Everything works offline; Firebase adds cloud backup, trusted time, remote tuning, analytics,
crash reports and the global leaderboard.

## One-time console setup (project `bookknhom`)

1. **Authentication → Sign-in method**: enable **Anonymous**. Enable **Email/Password** too if you
   want the Account screen's "Link email" button to work.
2. **Firestore Database → Create database** (production mode, pick a region near your players).
3. **Deploy rules and indexes** (from this folder, with the Firebase CLI):
   ```bash
   npm install -g firebase-tools
   firebase login
   firebase use bookknhom
   firebase deploy --only firestore:rules,firestore:indexes
   ```
   The leaderboard query (`bestWave desc, bestKills desc`) needs the composite index in
   `firestore.indexes.json` — without it the query fails and the screen stays empty.
4. **Remote Config**: create the parameters in `remote_config_defaults.json` (same names and values),
   then publish. The app ships with the same defaults, so this is only needed when you want to tune
   them live. Keys:
   | Key | Meaning |
   |---|---|
   | `xp_multiplier`, `coin_multiplier` | Global reward tuning (read through `RemoteValues`) |
   | `battle_pass_season_id` | Force a specific battle pass season (empty = by date) |
   | `interstitial_every_n_runs` | Ad pacing for non-"Remove Ads" players |
   | `min_supported_version` | Future force-update gate |
   | `cloud_save_enabled`, `leaderboard_enabled` | Kill switches |
   | `event_<id>_enabled` | Toggle a seasonal event without a build |
5. **Crashlytics**: open Crashlytics in the console once; the first crash report appears after the
   app is launched on a device, crashes, and is relaunched.
6. **Analytics**: nothing to configure. Events: `run_start`, `run_end`, `boss_defeated`, `level_up`,
   `skill_picked`, `evolution_discovered`, `screen_view`, and `stat_<id>` for every progress stat.

## Data layout
- `users/{uid}` — cloud save JSON + progress summary + display name (private to the owner)
- `leaderboard/{uid}` — `displayName`, `bestWave`, `bestKills`, `updatedAt`
- `server_time/{uid}` — scratch document used to read trusted server time

## Hardening later
Leaderboard scores are client-reported (validated for type/range only). For a competitive launch,
move submission into a Cloud Function that checks run records, or add App Check.

## Firebase in the Editor (optional)
Re-import the Firebase Unity SDK packages on this machine to restore
`Assets/Firebase/Plugins/x86_64/` (~220 MB, git-ignored), then change `BackendService.CreateProvider`
to return `FirebaseBackendProvider` in the Editor as well.
