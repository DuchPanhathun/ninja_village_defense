# Editor Setup Checklist

One-time steps you do by hand in the Unity Editor to activate what's already staged in `Packages/manifest.json`. Do these in order — later steps assume earlier ones are done. Budget ~10-15 minutes.

Open the project first: **Unity Hub → Add → select this folder** → open with **Unity 6000.0.32f1** (or the closest 6000.0 LTS you have installed; if prompted to change version, accept). First open will take a few minutes while packages resolve.

---

## 1. Input System (~2 min)

1. **Edit → Project Settings → Player → Other Settings → Active Input Handling** → set to **Input System Package (New)**.
   - Unity will ask to restart the Editor — click **Yes**.
2. After restart: in `Assets/_Project/Settings/`, right-click → **Create → Input Actions**. Name it `PlayerControls`.
3. Double-click it to open the Input Actions editor:
   - Add an **Action Map** named `Gameplay`.
   - Add an action `Move` of type **Value**, Control Type **Vector2**.
   - Add a binding: **Composite → 2D Vector** with WASD, *and* a separate binding using the **Touch/On-Screen Stick** binding (or just leave WASD for now — we'll wire the mobile joystick via a UI package later in EPIC 1).
   - Click **Save Asset**.
4. Check the box **☑ Configure Input System** in `task.text` — I'll do this automatically once you tell me it's done.

- [ ]  Done

---

## 2. URP — Universal Render Pipeline (~3 min)

1. `Assets/_Project/Settings/` → right-click → **Create → Rendering → URP Asset (with Universal Renderer)**. Name it `NinjaVillage_URP`.
   - This creates two assets: `NinjaVillage_URP.asset` and `NinjaVillage_URP_Renderer.asset`.
2. **Edit → Project Settings → Graphics** → drag `NinjaVillage_URP` into the **Scriptable Render Pipeline Settings** slot.
3. **Edit → Project Settings → Quality** → for each quality level (or at least the default), set the **Render Pipeline Asset** to `NinjaVillage_URP` as well.
4. Since this is 2D: in the URP asset inspector, you can leave defaults — 2D Renderer is auto-selected when you create a 2D project template, but if colors look washed out later, check the Renderer's **2D Renderer Data** is assigned.

- [ ]  Done

---

## 3. Addressables (~2 min)

1. **Window → Asset Management → Addressables → Groups**.
2. Click **Create Addressables Settings** (first-time button in that window).
   - This generates `Assets/AddressableAssetsData/` with a `Default Local Group` and settings asset.
3. Leave defaults for now — we'll addressable-tag prefabs (enemies, weapons, skill data) as we build them in later EPICs.

- [ ]  Done

---

## 4. Firebase (~5 min)

Your committed `firebase_cred.tsx` is a **web** config for project `bookknhom` — Unity needs different files. Do this in the [Firebase Console](https://console.firebase.google.com/) for that same project:

### 4a. Register Android app
1. Project **bookknhom** → ⚙️ **Project settings** → **Your apps** → **Add app → Android**.
2. **Android package name**: pick your bundle id now, e.g. `com.yourstudio.ninjavillagedefense` (must be final before Play Store upload — can't change later without a new listing).
3. Download **`google-services.json`** → place it directly in `Assets/` (project root of Assets, not a subfolder). It's already git-ignored.

### 4b. Register iOS app
1. Same **Add app → iOS**.
2. **iOS bundle ID**: use the *same* string as the Android package name (e.g. `com.yourstudio.ninjavillagedefense`) — keeps both stores aligned.
3. Download **`GoogleService-Info.plist`** → place it in `Assets/`. Also git-ignored.

### 4c. Import Firebase Unity SDK
1. Download the **Firebase Unity SDK** (`.zip`) from https://firebase.google.com/download/unity.
2. In Unity: **Assets → Import Package → Custom Package** and import, in order:
   - `FirebaseAuth.unitypackage`
   - `FirebaseFirestore.unitypackage` (Cloud Save)
   - `FirebaseRemoteConfig.unitypackage`
   - `FirebaseAnalytics.unitypackage`
   - `FirebaseCrashlytics.unitypackage`
3. When prompted about **Android Resolver / External Dependency Manager**, let it run — it patches Gradle dependencies automatically. This can take a few minutes and needs internet.
4. Once resolved, **File → Build Settings → Android → Player Settings**: set the **Package Name** to match `google-services.json` exactly.

### 4d. Set your bundle id in Unity
1. **Edit → Project Settings → Player**:
   - **Android tab → Other Settings → Package Name** = your chosen id.
   - **iOS tab → Other Settings → Bundle Identifier** = the same id.

- [ ]  Done

---

## 5. Mobile Build target (~2 min)

1. **File → Build Settings → Android** → **Switch Platform**.
2. **Player Settings → Android**:
   - **Minimum API Level**: Android 8.0 (API 26) or higher.
   - **Target API Level**: Automatic (highest installed).
   - **Scripting Backend**: IL2CPP.
   - **Target Architectures**: ARM64 only (uncheck ARMv7 — Play Store requires 64-bit, ARMv7-only submissions are rejected).
3. iOS platform switch can wait until we're closer to an iOS build — Android first per your brief.

- [ ]  Done

---

## When you're done

Tell me which steps you completed (all, or a subset) and I'll tick the matching boxes in `task.text` and commit. If anything errors (e.g. Android Resolver failing, package version mismatch), paste the error and I'll help debug it.
