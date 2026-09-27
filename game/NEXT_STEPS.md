# Ninja Village Defense — Next Steps

Updated 2026-09-24. **220 / 221 tasks done** (`task.text`). The only open item is the optional
Dependency Injection setup, skipped on purpose.

## What's verified

| Check | Result |
|---|---|
| Compile: runtime, Editor, tests, **Android player build** (`python3 Tools/compile_check.py --player`) | 0 errors |
| EditMode unit tests (57): save, economy, village, heroes, pets, talents, daily, live ops, backend, monetization | all pass |
| PlayMode smoke tests (4): every Main Menu screen, Village map + every building menu + an upgrade, 12 s of real battle with level-ups, death → revive prompt → run recorded | all pass |
| Content generation (9 generators) + scene building, headless | 0 failures |

**Not verified yet:** anything that needs a phone or a live account. That covers Firebase (it only
runs on device builds), real in-app purchases, real ads, and an actual Android build/install.

## What changed in the project

- **Unity 6000.0.84f1** (was 6000.0.32f1, same 6.0 LTS line). Opening it upgraded some packages to the
  versions this Editor pins (Input System 1.20, Addressables 2.10, Test Framework 1.6), and
  **Unity IAP 5.4.3** was added.
- **New scenes:** `MainMenu` (first in the build), `Village`; `Battle` was upgraded in place. That
  means run-start bonuses, dash, touch joystick, evolutions and a default ultimate.
- **All content is generated from code.** Menu **Ninja Village → Setup Everything (Content + Scenes)**
  re-runs every generator and rebuilds the scenes. It's safe to re-run: existing assets keep your edits
  and GUIDs. Headless:
  `unity run . -- -nographics -executeMethod NinjaVillage.EditorTools.ProjectSetup.RunAllBatch`
- **Tests:** `unity test . --mode EditMode` and `unity test . --mode PlayMode`.

## Only you can do these

1. **Put the Firebase config files back.** They're git-ignored and weren't on this machine, so Unity
   removed their `.meta` files. Copy `google-services.json` and `GoogleService-Info.plist` into
   `Assets/` (from the Firebase console, project `bookknhom`).
2. **Firebase console:** enable Anonymous (+ Email/Password) auth, create Firestore, and deploy
   `firebase/firestore.rules` and the indexes. The steps are in [`../firebase/README.md`](../firebase/README.md).
   Without this the game still works fully; it just stays offline.
3. **Store products:** create these in Play Console and App Store Connect:
   `com.thun.ninjavillagedefense.` + `gems_100`, `gems_550`, `gems_1200` (consumable), `starter_pack`,
   `remove_ads` (non-consumable), `battle_pass_premium` (consumable). Set prices there. For local
   Google receipt validation, run *Services → In-App Purchasing → Receipt Validation Obfuscator*.
4. **Ad network:** ads currently use a simulated provider (a full-screen placeholder ad) so the revive /
   double-coins / free-gems flows are testable. Plug a real network (e.g. LevelPlay) in by implementing
   `IAdsProvider` and setting `AdsService.Provider`.
5. **Android build tools:** the Editor was installed with Android Build Support only; the SDK, NDK and
   OpenJDK are not on disk yet, even though `unity install-modules --list` reports them as installed.
   Add them before building an APK, either in Unity Hub (*Installs → 6000.0.84f1 → Add modules →
   Android SDK & NDK Tools + OpenJDK*) or with:
   `unity install-modules -e 6000.0.84f1 -m android-sdk-ndk-tools android-open-jdk-17.0.18+8 --reinstall --accept-eula`.
   Then set up a keystore.
6. **Art and audio:** everything currently uses code-generated placeholders:
   - Sprites go in `Assets/_Project/Art/Sprites/`. They're compressed automatically (ASTC 6×6), and
     *Ninja Village → Build → Create Sprite Atlas* packs them.
   - Animator Controllers still work: assign one and the procedural animation steps aside.
   - Audio: drag clips onto the matching entries in `Resources/Catalogs/AudioLibrary.asset`.
   - Skins: give `SkinDefinition` assets a sprite.
7. **Balance pass after playtests:** `Resources/Catalogs/EconomyConfig.asset`, plus the building,
   hero, pet and talent assets under `Assets/_Project/Data/`.
8. **Live-ops calendar:** season/event/offer dates live in `Data/LiveOps/`. Add new assets as the
   calendar moves on; Remote Config can switch events off without a build.

## Known limitations

- The leaderboard is client-authoritative: rules check field types and ranges only. Before a
  competitive launch, move score submission into a Cloud Function or add App Check.
- Pets, clones, NPCs, VFX and buildings are placeholder shapes, and music and SFX are procedural
  placeholders. It's all functional, just not final.
- A few `Systems` scripts reference UI (the simulated ad overlay, the pause-on-background hook). Worth
  splitting if the systems move to their own assembly.

## Where things live (new systems)

| Area | Code |
|---|---|
| Save, migration, cloud reconcile | `Systems/Save`, `Systems/Backend` |
| Run-start bonuses (hero, gear, village, talents, pets, events, skins) | `Systems/Meta/RunStartModifiers` + each system's `*RunModifier` |
| Village, Forge, Shrine, Market, Castle | `Systems/Village`, `Systems/Inventory`, `Gameplay/Village`, `UI/Village` |
| Heroes / Pets / Talents | `Systems/Heroes`, `Systems/Pets`, `Systems/Talents` (+ `Gameplay/`, `UI/`) |
| Daily login, quests, achievements | `Systems/Daily`, `UI/Daily` |
| Battle pass, events, limited shop | `Systems/LiveOps`, `UI/LiveOps` |
| IAP, ads, skins, revive | `Systems/Monetization`, `UI/Store`, `UI/Battle/RevivePromptUI` |
| Evolutions | `Systems/Evolution`, `Gameplay/Skills/*Skill` + `Behaviors/`, `UI/Collection` |
| Audio | `Systems/Audio` (AudioManager, MusicDirector, procedural placeholders) |
| VFX, animation, pooling, performance | `Gameplay/Vfx`, `Gameplay/Animation`, `Core/Utilities/PrefabPool`, `Systems/Performance` |
| Code-built UI kit | `UI/Common` (UIBuilder, UIScreen, UIScreenNavigator) |
| Editor tooling | `Scripts/Editor` (ContentGen + generators, SceneBuilder, PackageSetup, TextureImportRules) |
