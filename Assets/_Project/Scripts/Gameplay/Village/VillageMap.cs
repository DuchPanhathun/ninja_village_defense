using System.Collections.Generic;
using System.Text;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.Requests;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// Builds the Village scene's world (EPIC 11 "Village map") in pack pixel art from a
    /// <see cref="VillageSnapshot"/> — your own village from the save, or someone else's when
    /// <see cref="VillageVisit"/> has one: grass with dirt roads and a plaza inside a forest ring, one
    /// <see cref="BuildingView"/> per building, the heroes you own training by the Dojo (the selected one
    /// tagged, with the active pet at heel), the other pets in the meadow by the Pet House, the Armory rack
    /// with your gear, the Talent Tree, your bought decorations, the houses on the residential lane and the treasury
    /// on the plaza, and townsfolk and farm animals whose numbers grow with the village and its houses, plus today's request-givers on the plaza with a "!" over their heads.
    /// <see cref="RefreshFromSave"/> redraws only what changed.
    /// </summary>
    public class VillageMap : MonoBehaviour
    {
        public static VillageMap Instance { get; private set; }

        [SerializeField] private int minVillagers = 2;
        [SerializeField] private int maxVillagers = 14;

        private static readonly string[] Tips =
        {
            "The Dojo trains your heroes — every level adds attack power!",
            "Blessings from the Shrine stay with you in every battle.",
            "The Market has new wares every day. Don't miss a bargain!",
            "Upgrade the Castle to let every other building grow taller.",
            "The Forge can reforge weapons into Steel, Gold... even Legendary!",
            "The Talent Tree blooms a little more with every talent you learn.",
            "The Pet House gives companions room to grow stronger.",
            "They say certain skills combine into forbidden techniques...",
            "Clear a chapter's final boss and a new land opens up.",
            "Dash through enemies — you can't be hurt mid-dash!",
            "A village with lanterns and flowers? Try the Decorate shop!",
            "Your heroes train here between battles. Tap one to manage them.",
            "Our village was just a hut once. Look at it now!",
            "Onigiri before a battle? The Kitchen makes you tougher for the whole fight!",
            "Rice, radish, carrots... the Kitchen turns the farm's harvest into battle meals.",
            "Every family in a house pays into the Treasury. Don't forget to collect it!",
            "The Treasury on the plaza fills up by itself. Once it's full, it stops!",
            "They say a Golden Koi lives in the pond. Only the best timing lands it!",
            "No spare gear to reforge with? The Forge takes bars from the mine too.",
        };

        private static readonly Dictionary<string, string> AnimalSounds = new()
        {
            ["animal_cat"] = "Meow!", ["animal_chicken"] = "Cluck cluck!", ["animal_dog"] = "Woof woof!",
            ["animal_pig"] = "Oink!", ["animal_frog"] = "Ribbit.", ["animal_cow"] = "Moooo~",
        };

        /// <summary>What the camera may show: the village plus a strip of its forest ring.</summary>
        public Rect Bounds
        {
            get
            {
                var b = VillageLayout.Bounds;
                return new Rect(b.xMin - 1.5f, b.yMin - 1.5f, b.width + 3f, b.height + 3f);
            }
        }

        /// <summary>The village being shown (yours, or the one being visited).</summary>
        public VillageSnapshot Snapshot { get; private set; }
        public bool IsOwnVillage => !VillageVisit.IsVisiting;

        private readonly Dictionary<string, BuildingView> _buildings = new();
        private readonly Dictionary<int, DecorationView> _decorations = new();
        private readonly List<VillageResident> _townsfolk = new();
        private Transform _residentRoot, _townRoot, _decorationRoot, _requestRoot;
        private string _requestsKey;
        private ArmoryDisplay _armory;
        private TalentTreeDisplay _talentTree;
        private ProfileBoardDisplay _profileBoard;
        private string _profileKey;
        private VillageArt _art;
        private string _residentsKey, _armoryKey;
        private int _talentRanks = -1;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus<BuildingUpgradedEvent>.Subscribe(OnBuildingUpgraded);
            EventBus<DecorationsChangedEvent>.Subscribe(OnDecorationsChanged);
            EventBus<RequestsChangedEvent>.Subscribe(OnRequestsChanged);
            EventBus<HousesChangedEvent>.Subscribe(OnHousesChanged);
            EventBus<Systems.Pets.PetCaredEvent>.Subscribe(OnPetCared);
        }

        private void OnDisable()
        {
            EventBus<BuildingUpgradedEvent>.Unsubscribe(OnBuildingUpgraded);
            EventBus<DecorationsChangedEvent>.Unsubscribe(OnDecorationsChanged);
            EventBus<RequestsChangedEvent>.Unsubscribe(OnRequestsChanged);
            EventBus<HousesChangedEvent>.Unsubscribe(OnHousesChanged);
            EventBus<Systems.Pets.PetCaredEvent>.Unsubscribe(OnPetCared);
        }

        private void Start()
        {
            _art = VillageArt.Load();
            HeroService.EnsureDefaults();
            _ = VillageService.Data; // fresh-save defaults (the Castle hut exists at Lv1)
            Snapshot = VillageVisit.Target ?? VillageSnapshot.FromSave(SaveService.Data);

            BuildGround();
            BuildForest();
            BuildBuildings();

            _armory = new GameObject("Armory").AddComponent<ArmoryDisplay>();
            _armory.transform.SetParent(transform, false);
            _armory.transform.position = VillageLayout.Armory;
            _talentTree = new GameObject("TalentTree").AddComponent<TalentTreeDisplay>();
            _talentTree.transform.SetParent(transform, false);
            _talentTree.transform.position = VillageLayout.TalentTree;
            _profileBoard = new GameObject("ProfileBoard").AddComponent<ProfileBoardDisplay>();
            _profileBoard.transform.SetParent(transform, false);
            _profileBoard.transform.position = VillageLayout.ProfileBoard;
            BuildFarm();
            BuildHousesAndTreasury();

            _decorationRoot = new GameObject("Decorations").transform;
            _decorationRoot.SetParent(transform, false);
            _residentRoot = new GameObject("Residents").transform;
            _residentRoot.SetParent(transform, false);
            _townRoot = new GameObject("Townsfolk").transform;
            _townRoot.SetParent(transform, false);
            _requestRoot = new GameObject("RequestGivers").transform;
            _requestRoot.SetParent(transform, false);

            if (IsOwnVillage) gameObject.AddComponent<DecorationPlacer>();
            gameObject.AddComponent<VillageAtmosphere>(); // day / night by the phone's clock, weather, lantern glow
            FrameCamera();
            Redraw();
        }

        /// <summary>Re-reads your save and redraws whatever changed (called when the village HUD comes back into view).</summary>
        public void RefreshFromSave()
        {
            if (!IsOwnVillage || Snapshot == null) return;
            Snapshot = VillageSnapshot.FromSave(SaveService.Data);
            Redraw();
        }

        private void OnBuildingUpgraded(BuildingUpgradedEvent evt) => RefreshFromSave();
        private void OnDecorationsChanged(DecorationsChangedEvent evt) => RefreshFromSave();
        private void OnRequestsChanged(RequestsChangedEvent evt) => SyncRequestGivers();
        private void OnHousesChanged(HousesChangedEvent evt) => RefreshFromSave(); // new families move in

        private void Redraw()
        {
            int castle = Mathf.Max(1, Snapshot.BuildingLevel(BuildingIds.Castle));
            foreach (var (id, view) in _buildings) view.Refresh(Snapshot.BuildingLevel(id), castle);

            SyncDecorations();

            string residents = ResidentsKey(Snapshot);
            if (residents != _residentsKey)
            {
                _residentsKey = residents;
                BuildResidents();
            }
            string armory = Snapshot.WeaponId + "|" + string.Join(",", Snapshot.EquipmentIds);
            if (armory != _armoryKey)
            {
                _armoryKey = armory;
                _armory.Show(_art, Snapshot);
            }
            if (Snapshot.TalentRanks != _talentRanks)
            {
                _talentRanks = Snapshot.TalentRanks;
                _talentTree.Show(_art, Snapshot);
            }
            string profile = $"{Snapshot.DisplayName}|{Snapshot.HighestWave}|{Snapshot.ChaptersCleared}|{Snapshot.AchievementTiers}|{Snapshot.TotalKills}";
            if (profile != _profileKey)
            {
                _profileKey = profile;
                _profileBoard.Show(_art, Snapshot);
            }
            SyncTownsfolk();
            SyncRequestGivers();
        }

        // ------------------------------------------------------------------ ground, roads, forest

        private void BuildGround()
        {
            var b = VillageLayout.Bounds;
            Tiled("Ground", _art != null ? _art.Ground : null, new Color(0.36f, 0.55f, 0.30f), b.center, b.size + new Vector2(16f, 16f), VillageSorting.Ground);
            var dirt = new Color(0.72f, 0.62f, 0.45f);
            foreach (var (center, size) in VillageLayout.Paths)
                Tiled("Path", _art != null ? _art.Path : null, dirt, center, size, VillageSorting.Paths);
            Tiled("Plaza", _art != null ? _art.Path : null, dirt, VillageLayout.Plaza, VillageLayout.PlazaSize, VillageSorting.Paths);
        }

        private void Tiled(string name, Sprite sprite, Color fallback, Vector2 center, Vector2 size, int order)
        {
            if (sprite == null)
            {
                GeneratedSprites.CreateRenderer(transform, name, GeneratedSprites.Square, fallback, order, center, size);
                return;
            }
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.transform.position = center;
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = order;
        }

        /// <summary>A ring of trees (and bushes in front) framing the village; fixed seed so it never changes.</summary>
        private void BuildForest()
        {
            if (_art == null || _art.ForestTrees.Length == 0) return;
            var root = new GameObject("Forest").transform;
            root.SetParent(transform, false);
            var rng = new System.Random(4242);
            var b = VillageLayout.Bounds;

            // Keep the forest off the farm field so it never hides a bed.
            var keepClear = new Rect(VillageLayout.FarmField.xMin - 0.3f, VillageLayout.FarmField.yMin - 0.5f,
                VillageLayout.FarmField.width + 0.6f, VillageLayout.FarmField.height + 1f);

            void Plant(Vector2 feet, Sprite[] pool)
            {
                if (pool.Length == 0) return;
                var sprite = pool[rng.Next(pool.Length)];
                Vector2 size = sprite.bounds.size;
                if (keepClear.Overlaps(new Rect(feet.x - size.x * 0.5f, feet.y, size.x, size.y))) return;
                var r = GeneratedSprites.CreateRenderer(root, "Tree", sprite, Color.white, VillageSorting.Order(feet.y),
                    feet + new Vector2(0f, sprite.bounds.extents.y));
                r.flipX = rng.NextDouble() < 0.5;
            }

            float Jitter() => (float)(rng.NextDouble() - 0.5) * 0.9f;
            for (float x = b.xMin - 2f; x <= b.xMax + 2f; x += 2.1f)
            {
                for (int row = 0; row < 3; row++)
                {
                    Plant(new Vector2(x + Jitter(), b.yMax + 0.3f + row * 1.4f + Jitter() * 0.5f), _art.ForestTrees);
                    Plant(new Vector2(x + Jitter(), b.yMin - 1.4f - row * 1.4f + Jitter() * 0.5f), _art.ForestTrees);
                }
                Plant(new Vector2(x + Jitter(), b.yMin - 0.3f), _art.ForestUndergrowth);
            }
            for (float y = b.yMin; y <= b.yMax; y += 1.9f)
                for (int col = 0; col < 3; col++)
                {
                    Plant(new Vector2(b.xMin - 0.6f - col * 1.8f + Jitter(), y + Jitter()), _art.ForestTrees);
                    Plant(new Vector2(b.xMax + 0.6f + col * 1.8f + Jitter(), y + Jitter()), _art.ForestTrees);
                }
        }

        private void BuildBuildings()
        {
            var catalog = VillageService.Catalog;
            if (catalog == null)
            {
                Debug.LogWarning("VillageMap: no BuildingCatalog — run Ninja Village → Generate Default Content.");
                return;
            }
            foreach (var def in catalog.All)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                var go = new GameObject();
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<BuildingView>();
                view.Initialize(def);
                _buildings[def.Id] = view;
            }
        }

        private void FrameCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            // Start looking at the castle and the plaza, with room for the HUD bars above and below.
            cam.transform.position = new Vector3(0f, 2f, cam.transform.position.z);
            cam.orthographicSize = Mathf.Max(cam.orthographicSize, 9.5f);
        }

        // ------------------------------------------------------------------ farm

        private void BuildFarm()
        {
            var farm = new GameObject("Farm").transform;
            farm.SetParent(transform, false);
            for (int plot = 0; plot < NinjaVillage.Systems.Farm.FarmRules.MaxPlots; plot++)
            {
                var view = new GameObject().AddComponent<FarmPlotView>();
                view.transform.SetParent(farm, false);
                view.Initialize(plot, _art, IsOwnVillage ? null : Snapshot);
            }
            var sign = new GameObject("FarmSign").AddComponent<FarmSignDisplay>();
            sign.transform.SetParent(farm, false);
            sign.transform.position = VillageLayout.FarmSign;
            sign.Build(_art);
        }

        // ------------------------------------------------------------------ houses, treasury, pond, mine

        private void BuildHousesAndTreasury()
        {
            var root = new GameObject("Houses").transform;
            root.SetParent(transform, false);
            for (int plot = 0; plot < HousingRules.MaxPlots; plot++)
            {
                var view = new GameObject().AddComponent<HouseView>();
                view.transform.SetParent(root, false);
                view.Initialize(plot, _art, IsOwnVillage ? null : Snapshot);
            }
            var treasury = new GameObject().AddComponent<TreasuryView>();
            treasury.transform.SetParent(transform, false);
            treasury.Initialize(_art, !IsOwnVillage);

            var pond = new GameObject().AddComponent<PondView>();
            pond.transform.SetParent(transform, false);
            pond.Initialize(_art, !IsOwnVillage);
            var mine = new GameObject().AddComponent<MineDisplay>();
            mine.transform.SetParent(transform, false);
            mine.Initialize(_art, !IsOwnVillage);
        }

        // ------------------------------------------------------------------ decorations

        private void SyncDecorations()
        {
            var wanted = new HashSet<int>();
            foreach (var placed in Snapshot.Decorations)
            {
                var definition = DecorationService.Get(placed.Id);
                if (definition == null) continue;
                wanted.Add(placed.Uid);
                if (_decorations.TryGetValue(placed.Uid, out var view))
                {
                    view.gameObject.SetActive(true);
                    view.MoveTo(new Vector2(placed.X, placed.Y));
                    view.Renderer.flipX = placed.Flip;
                }
                else
                {
                    _decorations[placed.Uid] = DecorationView.Create(_decorationRoot, definition, placed);
                }
            }
            var gone = new List<int>();
            foreach (var uid in _decorations.Keys)
                if (!wanted.Contains(uid)) gone.Add(uid);
            foreach (var uid in gone)
            {
                Destroy(_decorations[uid].gameObject);
                _decorations.Remove(uid);
            }
        }

        public DecorationView FindDecoration(int uid) => _decorations.TryGetValue(uid, out var view) ? view : null;

        // ------------------------------------------------------------------ heroes & pets

        private static string ResidentsKey(VillageSnapshot s)
        {
            var sb = new StringBuilder(s.SelectedHeroId).Append('|').Append(s.ActivePetId).Append('|');
            foreach (var h in s.Heroes) sb.Append(h.Id).Append(':').Append(h.Level).Append(':').Append(h.SkinId).Append(',');
            sb.Append('|');
            foreach (var p in s.Pets) sb.Append(p.Id).Append(':').Append(p.Level).Append(',');
            return sb.ToString();
        }

        private readonly Dictionary<string, VillageResident> _pets = new();

        private void BuildResidents()
        {
            foreach (Transform child in _residentRoot) Destroy(child.gameObject);
            _pets.Clear();

            Transform leader = null;
            var yard = VillageLayout.HeroYard;
            foreach (var hero in Snapshot.Heroes)
            {
                var set = (hero.SkinId != null ? CharacterSpriteLibrary.Find(hero.SkinId) : null)
                          ?? CharacterSpriteLibrary.Find(CharacterSpriteLibrary.HeroKey(hero.Id));
                var definition = HeroService.Get(hero.Id);
                string name = definition != null ? definition.NameOrId : hero.Id;
                var spawn = new Vector2(Random.Range(yard.xMin, yard.xMax), Random.Range(yard.yMin, yard.yMax));
                var resident = VillageResident.Spawn(_residentRoot, $"Hero_{hero.Id}", set, spawn, yard, 1.6f)
                    .Says(() => $"{name} · Lv {hero.Level}")
                    .OnTap(() => TappedDisplay(VillageDisplayKind.Heroes));
                if (hero.Id == Snapshot.SelectedHeroId)
                {
                    resident.WithTag(name, new Color(1f, 0.85f, 0.3f)); // gold name = your selected hero
                    leader = resident.transform;
                }
            }

            var meadow = VillageLayout.PetMeadow;
            foreach (var pet in Snapshot.Pets)
            {
                var set = CharacterSpriteLibrary.Find(CharacterSpriteLibrary.PetKey(pet.Id));
                var spawn = new Vector2(Random.Range(meadow.xMin, meadow.xMax), Random.Range(meadow.yMin, meadow.yMax));
                string petId = pet.Id;
                var resident = VillageResident.Spawn(_residentRoot, $"Pet_{pet.Id}", set, spawn, meadow, 1.3f)
                    .Says(() => $"{PetName(petId)} · Lv {pet.Level}")
                    .OnTap(() => TappedPet(petId));
                _pets[petId] = resident;
                if (IsOwnVillage && _art != null && Systems.Pets.PetCareService.IsHappy(petId)) resident.SetBadge(_art.Heart);
                if (pet.Id == Snapshot.ActivePetId && leader != null)
                {
                    resident.transform.position = (Vector2)leader.position + new Vector2(-1f, -0.3f);
                    resident.Follow(leader, new Vector2(-1f, -0.3f));
                }
            }
        }

        /// <summary>Tapping one of your pets opens its care bar (visitors just see it say its name).</summary>
        private void TappedPet(string petId)
        {
            if (!IsOwnVillage) return;
            EventBus<PetTappedEvent>.Raise(new PetTappedEvent(petId));
        }

        public VillageResident FindPet(string petId) => _pets.TryGetValue(petId, out var pet) ? pet : null;

        private void OnPetCared(Systems.Pets.PetCaredEvent evt)
        {
            var pet = FindPet(evt.PetId);
            if (pet == null || _art == null) return;
            pet.Cheer(_art.Heart);
            pet.SetBadge(_art.Heart);
        }

        private static string PetName(string petId)
        {
            var pet = Systems.Pets.PetService.Get(petId);
            return pet != null && !string.IsNullOrEmpty(pet.DisplayName) ? pet.DisplayName : petId;
        }

        private void TappedDisplay(VillageDisplayKind kind)
        {
            if (!IsOwnVillage) return;
            EventBus<VillageDisplayTappedEvent>.Raise(new VillageDisplayTappedEvent(kind));
        }

        // ------------------------------------------------------------------ villager requests

        private readonly List<VillageResident> _requestGivers = new();
        private float _nextRequestCheck;

        public IReadOnlyList<VillageResident> RequestGivers => _requestGivers;

        /// <summary>
        /// Today's request-givers on the plaza: a gold "!" while the favour is open, a green one once it can be
        /// handed in, nothing after. Only in your own village; rebuilt when requests are posted.
        /// </summary>
        private void SyncRequestGivers()
        {
            if (_requestRoot == null || !IsOwnVillage) return;
            var active = RequestService.Active;
            var key = new StringBuilder();
            foreach (var state in active) key.Append(state.Id).Append(':').Append(state.VillagerKey).Append(',');
            if (key.ToString() != _requestsKey)
            {
                _requestsKey = key.ToString();
                foreach (var giver in _requestGivers) if (giver != null) Destroy(giver.gameObject);
                _requestGivers.Clear();
                var square = VillageLayout.RequestSquare;
                for (int i = 0; i < active.Count; i++)
                {
                    var state = active[i];
                    int index = i;
                    var spawn = new Vector2(Mathf.Lerp(square.xMin, square.xMax, (i + 0.5f) / active.Count), Random.Range(square.yMin, square.yMax));
                    var giver = VillageResident.Spawn(_requestRoot, $"Requester_{state.VillagerKey}", CharacterSpriteLibrary.Find(state.VillagerKey),
                            spawn, square, 0.6f)
                        .Says(() => RequestLine(index))
                        .OnTap(() => TappedRequest(index));
                    _requestGivers.Add(giver);
                }
            }
            for (int i = 0; i < _requestGivers.Count && i < active.Count; i++)
            {
                var state = active[i];
                if (state.Delivered) _requestGivers[i].SetAlert(null, Color.white);
                else if (RequestService.CanDeliver(state)) _requestGivers[i].SetAlert("!", new Color(0.55f, 1f, 0.45f));
                else _requestGivers[i].SetAlert("!", new Color(1f, 0.82f, 0.25f));
            }
        }

        private static string RequestLine(int index)
        {
            var active = RequestService.Active;
            if (index < 0 || index >= active.Count) return null;
            var state = active[index];
            if (state.Delivered) return "Thank you so much!";
            var request = RequestService.Get(state.Id);
            return request != null ? request.Line : null;
        }

        private void TappedRequest(int index)
        {
            if (!IsOwnVillage) return;
            EventBus<VillagerRequestTappedEvent>.Raise(new VillagerRequestTappedEvent(index));
        }

        private void Update()
        {
            // Storehouse counts and the day change don't raise request events: re-check the marks now and then.
            if (!IsOwnVillage || Time.unscaledTime < _nextRequestCheck) return;
            _nextRequestCheck = Time.unscaledTime + 1f;
            SyncRequestGivers();
        }

        // ------------------------------------------------------------------ townsfolk

        /// <summary>How many townsfolk live here: 2 + one per three building levels, capped.</summary>
        public static int VillagerCountFor(int totalBuildingLevels, int min, int max) =>
            Mathf.Clamp(min + totalBuildingLevels / 3, min, max);

        private void SyncTownsfolk()
        {
            if (_art == null) return;
            // Every house level is a family: one more townsperson each.
            int people = VillagerCountFor(Snapshot.TotalBuildingLevels, minVillagers, maxVillagers) + Snapshot.HouseLevels;
            int animals = Mathf.Clamp(1 + Snapshot.BuildingLevel(BuildingIds.Castle) / 2, 1, 6);
            if (_art.VillagerKeys.Length == 0) people = 0;
            var b = VillageLayout.Bounds;
            var area = new Rect(b.xMin + 2f, b.yMin + 2f, b.width - 4f, b.height - 4f);
            // People and animals are counted apart, so a new family adds a person even after the animals arrived.
            while (_townPeople < people) AddTownsfolk(false, _townPeople++, area);
            while (_townAnimals < animals && _art.AnimalKeys.Length > 0) AddTownsfolk(true, _townAnimals++, area);
        }

        private int _townPeople, _townAnimals;

        private void AddTownsfolk(bool animal, int index, Rect area)
        {
            var keys = animal ? _art.AnimalKeys : _art.VillagerKeys;
            string key = keys[index % keys.Length];
            var set = CharacterSpriteLibrary.Find(key);
            var roam = animal ? VillageLayout.PetMeadow : area;
            var spawn = new Vector2(Random.Range(roam.xMin, roam.xMax), Random.Range(roam.yMin, roam.yMax));
            var resident = VillageResident.Spawn(_townRoot, key, set, spawn, roam, animal ? 0.8f : 1.1f);
            resident.Says(animal ? () => AnimalSounds.TryGetValue(key, out var sound) ? sound : "..." : () => Tips[Random.Range(0, Tips.Length)]);
            _townsfolk.Add(resident);
        }
    }
}
