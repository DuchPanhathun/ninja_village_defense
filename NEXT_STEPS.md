# Ninja Village Defense — Next Steps

Generated after the 2026-07-08 coding session.  
Current progress: **129 done / 221 total** (~58%).

---

## Part 1 — Manual Unity Editor Work (from This Session)

These items have code written. You just need to wire them up in the Unity Editor.

### A. Boss Prefabs — Swap Script Component

The three new boss prefabs currently use the base `BossController` script as a placeholder (the correct boss-specific `.cs` files exist but Unity hasn't generated their GUIDs yet — it does this on first compile after opening the project).

After opening the project:

1. Open `Assets/_Project/Prefabs/SpiderQueen.prefab`
   - Remove the `BossController` MonoBehaviour component
   - Add `SpiderQueenBoss` instead
   - Assign `SpiderlingPrefab` field → drag in the `Spider.prefab`
   - Assign `PlayerMask` → the player layer mask

2. Open `Assets/_Project/Prefabs/ShadowNinja.prefab`
   - Swap `BossController` → `ShadowNinjaBoss`
   - Create a simple projectile prefab (copy `KunaiProjectile`, rename it `ShadowProjectile`) and add a `ShadowProjectile` component to it
   - Assign `ShadowProjectilePrefab` field

3. Open `Assets/_Project/Prefabs/NineTailedFox.prefab`
   - Swap `BossController` → `NineTailedFoxBoss`
   - Assign `PlayerMask`

### B. Add Evolution Components to Player

In `Battle.unity`, select the Player GameObject:
- Add component `EvolutionManager` — drag in recipe assets (see C below) into the `Recipes` list
- Add component `EvolutionJournal` — no configuration needed

### C. Create Evolution Recipe Assets

Right-click in `Assets/_Project/Data/` → **Create → Ninja Village → Evolution Recipe**.
Create these four:

| Asset name | Required Skills | Required Weapon | Result Skill |
|---|---|---|---|
| `Evolution_Firestorm` | `Skill_FireBlade` | — | *(create a Firestorm skill asset — see note)* |
| `Evolution_ThunderKunai` | `Skill_LightningStrike` | `Kunai` weapon | *(create a Thunder Kunai skill asset)* |
| `Evolution_ShadowArmy` | — | `Katana` weapon | `Ultimate_ShadowCloneArmy` *(use the ultimate SO as result, or create a passive version)* |
| `Evolution_InvisibleAssassin` | `Skill_SmokeBomb` | — | *(create an Invisible Assassin skill asset)* |

> **Note:** The evolved result skills (Firestorm, Thunder Kunai, Invisible Assassin) don't exist yet as `SkillDefinition` assets. You'll need to create them — either as new `SkillDefinition` subclasses or as composites of existing ones. This is the one recipe item that still needs code.

### D. Create Equipment Definition Assets

Right-click → **Create → Ninja Village → Equipment**.
Create as many as you want. Suggested starter set:

| Name | Rarity | Notable Stat |
|---|---|---|
| Iron Ring | Common | +0.1 Attack Damage |
| Ninja Headband | Common | +0.05 Attack Speed |
| Shadow Cloak | Rare | +0.08 Move Speed |
| Jade Charm | Rare | +0.05 Crit Chance |
| Blood Talisman | Epic | +0.15 Attack Damage, +0.05 Crit |
| Dragon Scale | Legendary | +0.25 Attack Damage, +0.1 Heal/s |

After creating them, assign them to the `EquipmentPool` array on the `LootSpawner` in Battle.unity.

### E. Equipment & Chest Prefabs

1. Create an `EquipmentPickup.prefab`:
   - GameObject with `SpriteRenderer`, `Collider2D` (trigger), `EquipmentPickup` script

2. Assign the `EquipmentPickupPrefab` field on `LootSpawner` in Battle.unity

3. (Optional) Create a `Chest.prefab`:
   - `SpriteRenderer`, `CapsuleCollider2D` (trigger, tag = "Player"), `ChestPickup` script
   - Spawn it from `WaveManager` as a bonus spawn or from specific wave events

### F. Animations

All these items are Editor-only — the Animator hooks (`Speed`, `Attack`, `Die` triggers) are already wired in code and will work as soon as you assign clips:

| What needs an Animator Controller + sprite clips |
|---|
| Player (movement, idle) |
| All enemies (walk, attack, die) |
| Katana slash animation |

Process per enemy:
1. Select prefab → Add `Animator` component if missing
2. Create an `AnimatorController` asset in `Assets/_Project/Art/Animations/`
3. Add states: `Idle`, `Walk` (blend via `Speed` float), `Attack` (trigger), `Die` (trigger)
4. Assign sprite animation clips to each state

---

## Part 2 — Remaining Code Work by EPIC

Priority order is suggested — do higher-priority EPICs first as they unlock later ones.

### High Priority (core loop gaps)

**EPIC 17 — Main Menu UI**
- Home screen (Play button, version label)
- Settings panel (volume, graphics)
- Inventory screen
- Village entry button

**EPIC 16 — Save System (partial)**
- Player profile (name, total coins, run history)
- Inventory save (persist collected equipment between runs)
- Cloud save (Firebase Firestore) — setup is done, code not written yet

**EPIC 11 — Village System** *(entire EPIC)*
- Village scene with 2D camera navigation
- NPC interaction system
- Building data model (unlockable, upgradeable)
- Dojo: hero stat upgrades
- Forge: weapon crafting/upgrade table
- Shrine: permanent passive blessings
- Pet House: unlock/manage pets
- Market: daily rotating shop
- Castle: main progression gating

**EPIC 13 — Hero System** *(entire EPIC)*
- Hero data SO (base stats, starting weapon, passive ability)
- Hero unlock + selection screen
- Heroes: Assassin, Samurai, Monk, Mage Ninja, Beast Ninja

**EPIC 12 — Pet System** *(entire EPIC)*
- Pet follow AI (orbit player, avoid enemies)
- Pet abilities: Fox (XP vacuum), Wolf (melee bite), Hawk (radar), Monkey (banana stun), Dragon (fire breath)
- Pet leveling + equipment

### Medium Priority

**EPIC 14 — Talent Tree**
- Tree data model (nodes with prerequisites)
- Unlock/reset logic (costs coins/gems)
- UI: node graph renderer

**EPIC 18 — Audio**
- AudioManager singleton (background music, SFX channels)
- Background music (menu, battle, boss)
- Attack SFX (kunai throw, katana slash, shuriken, bow, chain sickle)
- Skill SFX (lightning, explosion, burn, poison)
- UI SFX (button click, level up, skill select)

**EPIC 19 — Daily Content**
- Daily login reward (incremental calendar)
- Daily quests (kill X enemies, survive Y seconds, etc.)
- Weekly quests
- Achievement system + trigger hooks

**EPIC 15 — Economy (remaining)**
- Upgrade cost curves (Dojo, Forge, Shrine)
- Reward balance pass (XP curve, coin drop rates per wave)

### Lower Priority (soft launch features)

**EPIC 22 — Firebase Backend**
- Authentication (anonymous → account link)
- Cloud Save (Firestore read/write for SaveData)
- Remote Config (balance values, event toggles)
- Analytics (session start, run complete, skill picked, purchase)
- Crashlytics (already imported, needs `Firebase.Crashlytics.Initialize()` call)
- Leaderboard (top run scores)

**EPIC 20 — Live Ops**
- Battle Pass (season progress + reward track)
- Seasonal events (wave modifier + exclusive rewards)
- Limited-time shop entries
- Event mission system

**EPIC 21 — Monetization**
- Cosmetic skin SO + skin selection UI
- Premium hero unlock (gem purchase gate on Hero screen)
- Remove Ads IAP flag
- Ad revival (watch ad → revive once per run)
- Reward ad (watch ad → 2x end-of-run coins)
- Starter pack bundles
- Gem purchase SKUs

**EPIC 23 — Polish**
- VFX prefabs: kunai trail, shuriken spin, katana slash arc, explosion burst, lightning bolt, web zone
- Object pool for projectiles + XP orbs
- Level-up particle burst
- Performance: enemy count cap, spatial grid for TargetFinder at high enemy density
- Battery optimization: target 30fps on mid-range Android, reduce physics tick rate
- Asset compression: texture atlasing for enemies + UI, ASTC for Android/iOS

---

## Part 3 — Small Unchecked Items in Existing EPICs

| EPIC | Item | Notes |
|---|---|---|
| EPIC 0 | Configure Dependency Injection | Optional — skip if architecture is working fine without it |
| EPIC 0 | Cloud Save | Firestore package imported; write `FirebaseCloudSave.cs` that wraps `SaveSystem.cs` |
| EPIC 0 | Analytics | Package imported; instrument key events (run start, boss kill, run end) |
| EPIC 1 | Movement animation | Animator Controller + sprite clips only |
| EPIC 1 | Dash movement | Optional — code + Editor |
| EPIC 2 | Weapon interface | Low priority — current design works fine without a formal `IWeapon` |
| EPIC 2 | Slash animation (Katana) | Animator Controller + sprite clips |
| EPIC 3 | Enemy animation | Animator Controller + sprite clips per enemy |
| EPIC 5 | Level animation | Particle effect or animator clip on XP bar level-up |
| EPIC 7 | Evolution recipes (4) | Assets in Editor (see Part 1-C) |
| EPIC 7 | Unlock animation | Play on `EvolutionUnlockedEvent`; can reuse a level-up particle |
| EPIC 7 | Collection UI | Panel listing all evolution journal entries |
| EPIC 15 | Upgrade costs | Write cost curves in Economy system |
| EPIC 15 | Reward balancing | Tune XP/coin values in enemy definition assets |

---

## Quick-Start Checklist for Next Session

If you want the game to be more playable immediately, do these in order:

1. Open project in Unity → let it compile (boss scripts, evolution scripts get GUIDs)
2. Swap boss scripts on SpiderQueen / ShadowNinja / NineTailedFox prefabs (Part 1-A)
3. Add EvolutionManager + EvolutionJournal to player (Part 1-B)
4. Create 3–5 EquipmentDefinition assets, assign to LootSpawner (Part 1-D/E)
5. Add movement + idle sprites to the player so it doesn't look invisible
6. Build the Main Menu scene (EPIC 17) — this unblocks the full core loop
7. Build the Village scene (EPIC 11) — this makes meta-progression feel real
