# Battle Scene Wiring Guide

How to assemble the first playable battle scene from the scripts in `Assets/_Project/Scripts/`. Do this **after** finishing `EDITOR_SETUP.md`. Budget ~30-45 min the first time. Placeholder art (colored squares/circles from **GameObject → 2D Object → Sprites**) is fine — gameplay first, art later.

## 0. Tags & Layers (once, 2 min)

**Edit → Project Settings → Tags and Layers → Layers**, add:
- `Player`
- `Enemy`
- `PlayerProjectile`

Then **Edit → Project Settings → Physics 2D → Layer Collision Matrix**: uncheck `PlayerProjectile` × `Player` (projectiles shouldn't hit their owner). Optionally uncheck `Enemy` × `Enemy` if you want enemies to overlap instead of pushing each other.

## 1. Scene

**File → New Scene** (2D) → save as `Assets/_Project/Scenes/Battle.unity`. Add it to **File → Build Settings → Scenes in Build**.

## 2. Player

Create a GameObject `Player`, layer **Player**:
- **SpriteRenderer** (placeholder square)
- **Rigidbody2D** — Body Type: Dynamic, Gravity Scale: **0**, Freeze Rotation Z
- **CapsuleCollider2D** (or Circle)
- Scripts: **Health** (set Max Health 100, Invulnerability Duration ~0.5), **PlayerStats**, **PlayerReference**, **PlayerController**, **AutoAttackController**, **SkillManager**, **UltimateController**, **KeyboardMoveInputProvider** (for Editor testing), **StatusEffectReceiver**, **HitFlash**
- On **PlayerController** → drag the `KeyboardMoveInputProvider` component into *Move Input Source* (swap to the VirtualJoystick later for device builds)
- On **AutoAttackController** → *Enemy Mask* = `Enemy`

## 3. Data assets (right-click in `Assets/_Project/Data/`)

- **Weapons/**: Create → Ninja Village → **Weapon Definition** → name `Kunai`. Set range ~6, damage ~10, 2 attacks/sec.
- **Enemies/**: Create → Ninja Village → **Enemy Definition** → name `Bandit` (HP 30, speed 2, damage 10, XP 5, coins 1). Make a second one `GiantOni` with big numbers and **Is Boss ✓**.
- **Skills/**: create one asset per skill from Create → Ninja Village → Skills → … (Attack Speed, Triple Throw, Movement Speed, XP Magnet, Gold Bonus, Lucky Drop, Dodge Chance, Auto Heal, Fire Blade, Poison Kunai, Lightning Strike, Explosive Bomb, Shield). Fill in Display Name + Description — the level-up cards show these.
- **Ultimates/**: Create → Ninja Village → Ultimates → **Dragon Slash** (or Heavenly Storm).
- **Waves/**: Create → Ninja Village → **Wave Definition** × 3-5. Wave 1: 5× Bandit. Ramp up. Final wave: **Is Boss Wave ✓**, Boss Definition = `GiantOni`.

Assign on the Player: AutoAttackController → *Starting Weapon* = `Kunai`; SkillManager → *Skill Pool* = all skill assets; UltimateController → *Equipped Ultimate*.

## 4. Prefabs (drag to `Assets/_Project/Prefabs/`, delete from scene after)

**KunaiProjectile**: SpriteRenderer + Rigidbody2D (Dynamic, Gravity 0) + small CircleCollider2D **Is Trigger ✓** + **Projectile** script. Layer **PlayerProjectile**. → assign to the `Kunai` weapon asset's *Projectile Prefab*.

**Bandit**: SpriteRenderer + Rigidbody2D (Dynamic, Gravity 0, Freeze Rotation) + CapsuleCollider2D + **Health** + **EnemyController** + **StatusEffectReceiver** + **HitFlash**. Layer **Enemy**. → assign to `Bandit` asset's *Enemy Prefab*.

**GiantOni**: same components but **GiantOniBoss** instead of EnemyController, bigger sprite, *Player Mask* = `Player`. → assign to `GiantOni` asset.

**XpOrb**: small SpriteRenderer + **XpOrb** script (no collider needed — it uses distance checks).

**Coin**: small SpriteRenderer + **CoinPickup** script.

**DamageNumber**: empty GO → add **TextMeshPro - Text** (the 3D/world one, NOT UI) → font size ~4 → add **DamageNumber** script.

## 5. Managers (empty GameObjects in the scene)

- `GameManager` → **GameManager**
- `EconomyManager` → **EconomyManager**
- `LevelSystem` → **LevelSystem**
- `SpawnManager` → **SpawnManager**
- `WaveManager` → **WaveManager** (assign *Waves* array + *Spawn Manager*)
- `XpOrbSpawner` → **XpOrbSpawner** (assign XpOrb prefab)
- `LootSpawner` → **LootSpawner** (assign Coin prefab)
- `DamageNumberSpawner` → **DamageNumberSpawner** (assign DamageNumber prefab)

## 6. Camera

On the **Main Camera**: add **CameraFollow** (Target = Player) and **CameraShake**. Projection: Orthographic, size ~6.

## 7. Battle HUD (Canvas)

**GameObject → UI → Canvas** (Scale With Screen Size, 1080×1920 reference). Add an **EventSystem** if Unity didn't. Children:

| Element | Build | Script |
|---|---|---|
| HP bar (top-left) | Image background + child **Filled** Image (Horizontal) | **PlayerHealthBarUI** → assign fill (+ optional TMP text) |
| XP bar (top, full width) | same pattern | **XpBarUI** → assign fill + LevelSystem + TMP level text |
| Coin counter (top-right) | coin Image + TMP text | **CoinCounterUI** |
| Boss bar (top-center, big) | container + fill + TMP name | **BossHealthBarUI** (assign *Bar Root* = container) |
| Ultimate button (bottom-right) | **Button** + child Filled Image | **UltimateButtonUI** on the Button |
| Pause button (top corner) + panel | Button + full-screen panel w/ Resume button | **PauseMenu**; wire both buttons to *TogglePause* |
| Skill choice panel | full-screen dimmed panel + **3 Buttons**, each with 2 TMP texts (name, description) | **SkillChoicePanel** → assign panel root + 3 buttons + 3+3 texts |
| Game over panel | full-screen panel + title TMP + wave TMP + Retry Button | **GameOverPanel**; Retry → GameManager.**RestartRun** |
| Virtual joystick (later, for device) | background Image + child handle Image under a stretched touch zone | **VirtualJoystick**; then set it as PlayerController's input source |

## 8. Press Play

Expected: WASD moves the ninja; bandits stream in from off-screen and chase; kunai auto-fire at the nearest one; kills drop XP orbs + coins that magnet to you; the XP bar fills; level-up pauses and offers 3 skill cards; waves escalate; the Giant Oni arrives with a boss bar, charges at you, and ground-smashes with a shockwave; dying (or clearing all waves) shows the run summary; Retry restarts cleanly.

If something misbehaves, check the Console first — most wiring mistakes log a clear warning (missing prefab on a definition asset, missing input source, etc.).
