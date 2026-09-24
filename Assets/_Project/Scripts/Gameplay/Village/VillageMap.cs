using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// Builds the Village scene's world from data (EPIC 11 "Village map"): ground, paths, one
    /// <see cref="BuildingView"/> per BuildingDefinition at its plot, cherry trees and walls that appear
    /// as the village grows, and villagers whose number scales with total building levels. No art or
    /// hand-placed scene objects are required — the scene only needs this component and a camera.
    /// </summary>
    public class VillageMap : MonoBehaviour
    {
        public static VillageMap Instance { get; private set; }

        [SerializeField] private Vector2 groundSize = new(30f, 22f);
        [SerializeField] private Color groundColor = new(0.36f, 0.55f, 0.30f, 1f);
        [SerializeField] private Color pathColor = new(0.72f, 0.62f, 0.45f, 1f);
        [SerializeField] private int minVillagers = 2;
        [SerializeField] private int maxVillagers = 14;

        /// <summary>World-space rect the camera may show (the ground plus a margin).</summary>
        public Rect Bounds => new(-groundSize * 0.5f, groundSize);

        private readonly Dictionary<string, BuildingView> _views = new();
        private readonly List<VillagerNpc> _villagers = new();
        private Transform _decorations;
        private Transform _npcRoot;

        private static readonly Color[] ClothesColors =
        {
            new(0.25f, 0.35f, 0.65f), new(0.65f, 0.25f, 0.3f), new(0.3f, 0.55f, 0.35f),
            new(0.55f, 0.45f, 0.2f), new(0.45f, 0.3f, 0.6f), new(0.2f, 0.2f, 0.25f),
        };

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => EventBus<BuildingUpgradedEvent>.Subscribe(OnBuildingUpgraded);
        private void OnDisable() => EventBus<BuildingUpgradedEvent>.Unsubscribe(OnBuildingUpgraded);

        private void Start()
        {
            // Touching Data applies fresh-save defaults (the Castle hut exists at Lv1).
            _ = VillageService.Data;

            GeneratedSprites.CreateRenderer(transform, "Ground", GeneratedSprites.Square, groundColor, -100, Vector2.zero, groundSize);
            GeneratedSprites.CreateRenderer(transform, "PathH", GeneratedSprites.Square, pathColor, -90, new Vector2(0f, -1f), new Vector2(groundSize.x * 0.85f, 1.2f));
            GeneratedSprites.CreateRenderer(transform, "PathV", GeneratedSprites.Square, pathColor, -90, new Vector2(0f, 0f), new Vector2(1.2f, groundSize.y * 0.8f));

            var catalog = VillageService.Catalog;
            if (catalog != null)
            {
                foreach (var def in catalog.All)
                {
                    if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                    var go = new GameObject();
                    go.transform.SetParent(transform, false);
                    var view = go.AddComponent<BuildingView>();
                    view.Initialize(def);
                    _views[def.Id] = view;
                }
            }
            else
            {
                Debug.LogWarning("VillageMap: no BuildingCatalog — run Ninja Village → Generate Default Content.");
            }

            _npcRoot = new GameObject("Villagers").transform;
            _npcRoot.SetParent(transform, false);
            RebuildDecorations();
            SyncVillagers();
        }

        private void OnBuildingUpgraded(BuildingUpgradedEvent evt)
        {
            RebuildDecorations();
            SyncVillagers();
        }

        /// <summary>How many villagers live here: 2 + one per three building levels, capped.</summary>
        public static int VillagerCountFor(int totalBuildingLevels, int min, int max) =>
            Mathf.Clamp(min + totalBuildingLevels / 3, min, max);

        private void SyncVillagers()
        {
            int wanted = VillagerCountFor(VillageService.TotalBuildingLevels, minVillagers, maxVillagers);
            var area = new Rect(Bounds.xMin + 2f, Bounds.yMin + 2f, Bounds.width - 4f, Bounds.height - 4f);
            while (_villagers.Count < wanted)
            {
                int i = _villagers.Count;
                Vector2 spawn = new(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                _villagers.Add(VillagerNpc.Spawn(_npcRoot, spawn, area, ClothesColors[i % ClothesColors.Length], i));
            }
        }

        /// <summary>Cherry trees grow in number with the village; walls appear once the Castle is Lv 3+.</summary>
        private void RebuildDecorations()
        {
            if (_decorations != null) Destroy(_decorations.gameObject);
            _decorations = new GameObject("Decorations").transform;
            _decorations.SetParent(transform, false);

            int total = VillageService.TotalBuildingLevels;
            int trees = Mathf.Clamp(4 + total, 4, 40);
            var rng = new System.Random(1234); // fixed seed: the same trees stay in the same place
            int placed = 0, attempts = 0;
            while (placed < trees && attempts++ < trees * 20)
            {
                var p = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * (groundSize.x - 2f),
                    (float)(rng.NextDouble() - 0.5) * (groundSize.y - 2f));
                if (OverlapsBuilding(p) || Mathf.Abs(p.x) < 1.2f || Mathf.Abs(p.y + 1f) < 1.2f) continue; // keep paths clear
                bool blossom = rng.NextDouble() < 0.6;
                int order = Mathf.RoundToInt(-p.y * 4f);
                GeneratedSprites.CreateRenderer(_decorations, "Trunk", GeneratedSprites.Square, new Color(0.4f, 0.26f, 0.15f), order, p + new Vector2(0f, -0.35f), new Vector2(0.25f, 0.6f));
                GeneratedSprites.CreateRenderer(_decorations, "Leaves", GeneratedSprites.Circle,
                    blossom ? new Color(1f, 0.72f, 0.82f) : new Color(0.2f, 0.45f, 0.22f), order + 1, p + new Vector2(0f, 0.25f), new Vector2(1.3f, 1.1f));
                placed++;
            }

            if (VillageService.CastleLevel >= 3)
            {
                var wall = new Color(0.55f, 0.52f, 0.5f);
                float w = groundSize.x - 1f, h = groundSize.y - 1f;
                GeneratedSprites.CreateRenderer(_decorations, "WallN", GeneratedSprites.Square, wall, -80, new Vector2(0f, h * 0.5f), new Vector2(w, 0.4f));
                GeneratedSprites.CreateRenderer(_decorations, "WallS", GeneratedSprites.Square, wall, 400, new Vector2(0f, -h * 0.5f), new Vector2(w, 0.4f));
                GeneratedSprites.CreateRenderer(_decorations, "WallW", GeneratedSprites.Square, wall, -80, new Vector2(-w * 0.5f, 0f), new Vector2(0.4f, h));
                GeneratedSprites.CreateRenderer(_decorations, "WallE", GeneratedSprites.Square, wall, -80, new Vector2(w * 0.5f, 0f), new Vector2(0.4f, h));
            }
        }

        private bool OverlapsBuilding(Vector2 p)
        {
            foreach (var view in _views.Values)
            {
                var def = VillageService.Get(view.BuildingId);
                if (def == null) continue;
                Vector2 half = def.Footprint * 0.8f + Vector2.one;
                if (Mathf.Abs(p.x - def.PlotPosition.x) < half.x && Mathf.Abs(p.y - def.PlotPosition.y) < half.y + 0.8f) return true;
            }
            return false;
        }
    }
}
