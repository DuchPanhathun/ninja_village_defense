# Ninja Village — website (player site + admin)

The web platform from [`task_web.text`](../task_web.text): players redeem codes, buy in the web shop and manage their
account; staff run the admin site at `/admin/`. Built with **Astro 7 + TypeScript**, Preact islands for the
interactive parts, and the Firebase web SDK. The site is static (Firebase Hosting); everything that must be trusted
runs in the **Cloud Functions** in [`../firebase/functions`](../firebase/functions).

```
web/
  src/pages/            player pages (/, /signin/, /account/, /redeem/, /shop/, /delete-account/, legal) + /admin/*
  src/components/       islands/ (player UI), admin/ (admin pages), RewardChips, SignInPrompt
  src/lib/              firebase.ts (auth), db.ts (Firestore Lite), api.ts (typed callables), session.ts, appCheck.ts
  src/i18n/en.ts        every player-facing string (the game is English-only today)
  src/styles/           global.css (pixel theme), admin.css
  public/art/           sprites copied from the game (npm run art)
../firebase/functions/src/shared/   the data contract shared with the functions (imported here as @shared/...)
```

How it fits together: the website never edits a player's save. Codes, purchases and gifts become **grants**
(`users/{uid}/grants/{id}`) that the game collects into its inbox (WEB 1). Only the functions create grants,
orders and codes.

## Local development (no real project needed)

Requirements: Node 22+, Java 21 (for the Firestore emulator), the Firebase CLI (`npm i -g firebase-tools`).

```bash
# 1. Emulators: Auth, Firestore, Functions (from firebase/)
cd firebase
npm --prefix functions install
npm --prefix functions run build
firebase emulators:start --project demo-ninja --only auth,firestore,functions

# 2. Demo data (another terminal, from firebase/)
npm --prefix functions run seed

# 3. The website against the emulators (from web/)
npm install
npm run dev:emulator        # http://localhost:4321
```

Demo accounts (password `password123`):

| Email | Role | Notes |
|---|---|---|
| `player@example.com` | player | progress, inbox grants, a redeemed code, an order |
| `support@example.com` | support | search, gifts, bans, renames, moderation |
| `admin@example.com` | admin | 2-step sign-in by SMS to +1 555-555-0100 — the code appears in the emulator log / Emulator UI (http://localhost:4000/auth) |

Test codes: `WELCOME2026`, `STREAMNIGHT`. The shop runs in **sandbox mode**: checkout opens a test payment page.

## Tests

From `firebase/`:

```bash
npm --prefix functions test                       # unit tests (codes, rewards, money, signatures, save parsing)
firebase emulators:exec --project demo-ninja --only firestore "npm --prefix functions run test:rules"
npm --prefix functions run build && \
firebase emulators:exec --project demo-ninja --only auth,firestore,functions "npm --prefix functions run test:functions"
```

`npm run build` here runs `astro check` (types) and builds `dist/`.

## Configuration

- **Website:** copy `.env.example` to `.env` and fill in the web app config from the Firebase console (project
  `bookknhom` → Project settings → Your apps → Web). These values are public by design.
- **Functions:** `firebase/functions/.env` (region, site URL, payments provider, App Check, MFA, attempt limits).
  `PUBLIC_FUNCTIONS_REGION` must match `FUNCTIONS_REGION`. The webhook secret goes to Secret Manager:
  `firebase functions:secrets:set PAYMENT_WEBHOOK_SECRET`.

## Deploy the website on Vercel (current)

Live: **https://ninja-village-defense-seven.vercel.app** (project `ninja-village-defense`, team duchpanhathun's projects). From the repo root:

```bash
vercel deploy --prod      # production
vercel deploy             # preview (protected by Vercel login)
```

- Run it from the **repo root**, not `web/`: the site imports `firebase/functions/src/shared`. The root
  `.vercelignore` uploads only `web/` and that folder (~0.6 MB); the root `vercel.json` has the build commands,
  trailing-slash routing and headers.
- Settings are Vercel environment variables (`vercel env ls`), the same `PUBLIC_*` names as `.env.example`, plus
  `SITE_URL`. They're baked in at build time, so redeploy after changing one.
- For now the Firebase key/app id are the **Android** ones from `game/Assets/google-services.json` (they work from the
  web). Later: register a Web app in the Firebase console and replace `PUBLIC_FIREBASE_API_KEY` /
  `PUBLIC_FIREBASE_APP_ID` (needed for App Check).
- Firebase console → Authentication → Settings → **Authorized domains**: add `ninja-village-defense-seven.vercel.app`
  (and your own domain when you add one).
- The backend still deploys to Firebase: `firebase deploy --only firestore:rules,firestore:indexes,functions`
  (below). Until then sign-in and the account page work, but redeem, the shop and the admin tools can't reach the
  server.

## Deploy on Firebase Hosting (alternative) and the backend

One-time console steps (WEB 0): Blaze plan (with a budget alert), Authentication → Email/Password on, **Identity
Platform → multi-factor → TOTP** on (admins need it), then:

```bash
cd firebase
firebase use bookknhom
firebase deploy --only firestore:rules,firestore:indexes,functions,hosting
```

Hosting builds the site first (`predeploy`) and serves `web/dist`.

**First admin:** sign in once on the website (or link the email in the game), then with Google credentials for
the project (`gcloud auth application-default login`):

```bash
node functions/scripts/set-role.mjs --project bookknhom --email you@example.com --role admin
```

Then open `/admin/security/`, add an authenticator app, sign out and back in. After that, manage roles on the
Staff page. Roles: **admin** (everything, 2-step required), **support** (search, gifts, bans, renames,
moderation), **viewer** (read-only).

## Before real payments

- Pick a provider (WEB 0) and add an adapter in `firebase/functions/src/payments/providers.ts`: create the hosted
  checkout and verify the webhook signature. Order creation, exactly-once grants, refunds and chargebacks are
  shared and already tested. Then set `PAYMENTS_PROVIDER`, point the provider's webhook at the `paymentWebhook`
  function URL, and set `PUBLIC_PAYMENTS_TEST_MODE=false`.
- Turn on App Check: create a reCAPTCHA Enterprise key, set `PUBLIC_RECAPTCHA_ENTERPRISE_KEY`, watch the App Check
  metrics, then set `ENFORCE_APP_CHECK=true` in the functions.
- Have the legal pages reviewed, then set `PUBLIC_LEGAL_DRAFT=false`.

## Adding a language

Copy `src/i18n/en.ts` (same keys), register it in `src/i18n/index.ts`, and add locale routes (Astro i18n
routing). The legal pages are separate files per language.
