# Ninja Village Defense

A 2D roguelike action game (Unity) with a website for players and staff, sharing one Firebase backend.

```
ninja_village_defense/
├── game/        Unity 6 project (open this folder in Unity Hub) — see game/README.md
├── web/         Player website + admin site (Astro + TypeScript) — see web/README.md
├── firebase/    Backend shared by both: Firestore rules/indexes, Remote Config defaults,
│                Cloud Functions (functions/) — see firebase/README.md
├── task_web.text   Web platform checklist (WEB 0–7)
├── vercel.json     Website deploy on Vercel (run `vercel deploy --prod` from here)
└── .vercelignore   Uploads only web/ and the shared data contract, never the Unity project
```

How the parts talk: the game and the website use the same Firebase accounts and Firestore. The website never edits
a player's save — codes, web purchases and staff gifts become **grants** in `users/{uid}/grants`, written only by the
Cloud Functions, and the game collects them into its inbox. The data contract lives in
[`firebase/functions/src/shared/`](firebase/functions/src/shared) and is imported by both the functions and the
website.

## Quick start

| Part | Where | Commands |
|---|---|---|
| Game | `game/` | Open in Unity Hub (Unity 6000.0.84f1). `python3 Tools/compile_check.py --player`, `unity test . --mode EditMode` |
| Website | `web/` | `npm install`, `npm run dev:emulator` (needs the emulators below), `npm run build` |
| Backend | `firebase/` | `npm --prefix functions install && npm --prefix functions run build`, then `firebase emulators:start --project demo-ninja --only auth,firestore,functions` and `npm --prefix functions run seed` |
| Deploy site | repo root | `vercel deploy --prod` |
| Deploy backend | `firebase/` | `firebase deploy --only firestore:rules,firestore:indexes,functions` |

Game design and checklists: [`game/goal.text`](game/goal.text), [`game/task.text`](game/task.text),
[`game/NEXT_STEPS.md`](game/NEXT_STEPS.md).
