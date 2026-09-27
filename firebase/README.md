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
- `users/{uid}` — cloud save JSON + progress summary + display name (private to the owner). The `save` string and
  `villages/{uid}.snapshot` are exempt from indexing (`firestore.indexes.json`) — nothing queries them.
- `villages/{uid}` — the public village for visiting: `displayName`, `castleLevel`, `highestWave`,
  `chaptersCleared`, `achievementTiers`, `snapshot` (VillageSnapshot JSON: buildings, decorations, heroes,
  pets, gear, talents), `updatedAt`. Readable by every signed-in player; published on connect and after
  each cloud-save upload when the village changed. Listed newest first (single-field index, automatic).
  Also `likes` / `likesWeek` (this week's likes, counted by the owner from the visits below) and `rankKey`
  (`week × 1,000,000 + likes`) for the weekly Best Village ranking — one range + order on one field, so
  the automatic single-field index is enough.
- `villages/{uid}/visits/{visitorUid}` — the visitors' book: `name`, `visitDay`, `likedWeek`, `giftDay`,
  `waterDay`, `waterTicks`, `at`. Written only by that visitor (never on their own village), read by the
  owner, who turns new gifts into coins, applies waterings to the farm and counts likes.
- `leaderboard/{uid}` — `displayName`, `bestWave`, `bestKills`, `updatedAt`
- `server_time/{uid}` — scratch document used to read trusted server time

### Web platform (task_web.text) — written only by the Cloud Functions in `functions/`
- `users/{uid}` also holds server-only moderation fields the client can't change: `banned`, `bannedUntil`,
  `banReason`, `nameOverride`, `leaderboardHidden`, `villageHidden`, `reviewFlag`. The leaderboard, village and
  visitors'-book rules read them (banned or hidden players can't publish; a renamed player must use `nameOverride`).
- `users/{uid}/grants/{grantId}` — rewards waiting in the in-game inbox: `kind` (purchase / redeem / gift /
  compensation), `rewards` [{type, id, amount}], `title`, `message`, `createdAt`, `claimedAt`, `revokedAt`, `source`.
  The owner may only set `claimedAt` (once, to the server time). Contract: `functions/src/shared/types.ts`.
- `users/{uid}/redemptions/{code}`, `users/{uid}/moderationLog/{id}`
- `codes/{CODE}` (+ `redemptions/{uid}`), `products/{id}`, `orders/{id}`, `mail/{id}` (gift to everyone),
  `deletionRequests/{uid}`, `audit/{id}`, `staff/{uid}`, `stats/{yyyy-mm-dd}`, `rateLimits/{key}`

Functions (callable unless noted): `redeemCode`, `collectMail` (the game calls it on sign-in/resume),
`requestAccountDeletion` / `cancelAccountDeletion`, `createCheckout`, `sandboxPay`, `paymentWebhook` (HTTP),
the `admin*` staff functions, `onAccountCreated` (Auth trigger) and `checkOrders` (daily).

Deploy everything: `firebase deploy --only firestore:rules,firestore:indexes,functions,hosting` — see
[`../web/README.md`](../web/README.md) for configuration, the first admin, local emulators and tests.

## Hardening later
Leaderboard scores and village likes are client-reported (validated for type/range only). For a competitive launch,
move submission into a Cloud Function that checks run records, and turn on App Check (`ENFORCE_APP_CHECK`).

**Backups:** schedule daily Firestore backups once on Blaze:
`gcloud firestore backups schedules create --database='(default)' --recurrence=daily --retention=14d`

## Firebase in the Editor (optional)
Re-import the Firebase Unity SDK packages on this machine to restore
`game/Assets/Firebase/Plugins/x86_64/` (~220 MB, git-ignored), then change `BackendService.CreateProvider`
to return `FirebaseBackendProvider` in the Editor as well.
