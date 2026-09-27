# Ninja Village Defense

> This folder is the **Unity project** — open `game/` in Unity Hub, and run the commands below from here.
> The repo also holds the website ([`../web`](../web)) and the Firebase backend ([`../firebase`](../firebase));
> see the [repository overview](../README.md).

A 2D roguelike action game (in the vein of *Survivor.io* / *Archero*) built in **Unity 6 (6000.0.84f1 LTS)** with **C#**, backed by **Firebase**. Target platforms: **Android first, then iOS** from the same codebase.

> Become the last ninja protecting your hidden village against endless demon invasions. Every battle makes your village stronger, unlocks new ninjas, and reveals ancient forbidden techniques.

See [`goal.text`](goal.text) for the full design vision and [`task.text`](task.text) for the living build checklist.

## Tech Stack
- **Engine:** Unity 6000.0.84f1 LTS — URP (Universal Render Pipeline), new Input System, Addressables, Cinemachine, 2D packages, Unity IAP 5.
- **Language:** C# (`NinjaVillage.*` namespaces, single `NinjaVillage` assembly for now).
- **Backend:** Firebase — Auth, Cloud Save (Firestore), Remote Config, Analytics, Crashlytics.

## Architecture
Systems are **decoupled through events** rather than direct references, and **data-driven via ScriptableObjects**, so new heroes / skills / enemies / modes can be added without rewiring core code.

```
Assets/_Project/
├── Scripts/
│   ├── Core/
│   │   ├── Events/            # EventBus<T> + IGameEvent + core game events
│   │   ├── ScriptableObjects/ # Data-definition base classes
│   │   ├── Utilities/
│   │   └── DI/
│   ├── Gameplay/              # Player, Enemies, Weapons, Skills, Waves, Combat
│   ├── Systems/               # Save, Economy, Audio, Firebase
│   └── UI/
├── Scenes/  Prefabs/  Art/  Audio/
├── Data/                     # ScriptableObject asset instances
└── Settings/                 # URP + Input assets
```

### Event Bus
Type-safe, allocation-free pub/sub. Events are `readonly struct : IGameEvent`.
```csharp
EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);   // OnEnable
EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged); // OnDisable
EventBus<PlayerDamagedEvent>.Raise(new PlayerDamagedEvent(amount, hp));
```

## Firebase setup (required before backend tasks)
The committed `firebase_cred.tsx` is a **web** config — Unity does **not** use it. Before EPIC 22:
1. In the Firebase console (project `bookknhom`) add an **Android** app and download `google-services.json` → place at `Assets/`.
2. Add an **iOS** app and download `GoogleService-Info.plist` → place at `Assets/`.
3. Import the **Firebase Unity SDK** packages (Auth, Firestore, RemoteConfig, Analytics, Crashlytics).

Both credential files are git-ignored — never commit them.

## Status
All EPICs are implemented (220/221 tasks; optional DI skipped). The core loop runs end to end:
Main Menu → Village → Battle → back. Placeholder art and audio are generated in code. See
[`NEXT_STEPS.md`](NEXT_STEPS.md) for what's verified and what still needs you: Firebase console, store
products, an ad network, real art/audio, and device testing.

### Working on the project
- **Regenerate content + scenes:** Unity menu *Ninja Village → Setup Everything (Content + Scenes)*.
- **Fast compile check (no Editor needed after one import):** `python3 Tools/compile_check.py --player`
- **Tests:** `unity test . --mode EditMode` / `unity test . --mode PlayMode`
