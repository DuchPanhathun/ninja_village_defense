using System.Linq;
using NinjaVillage.Core;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Inventory;
using UnityEditor;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Weapons (Kunai exists; Shuriken, Katana, Bow, Chain Sickle), the WeaponCatalog, the six starter
    /// equipment pieces from NEXT_STEPS.md + EquipmentCatalog, and the EconomyConfig. Runs first
    /// because heroes, the Forge, the Market and the loot tables reference these.
    /// </summary>
    public static class WeaponEquipmentGenerator
    {
        private const string WeaponFolder = ContentGen.DataRoot + "/Weapons";
        private const string EquipmentFolder = ContentGen.DataRoot + "/Equipment";

        [ContentGenerator("Weapons, equipment & economy config", 0)]
        public static void Generate()
        {
            GenerateWeapons();
            GenerateEquipment();
            ContentGen.CreateOrLoad<EconomyConfig>($"{ContentGen.CatalogRoot}/EconomyConfig.asset");
        }

        private static void GenerateWeapons()
        {
            var kunaiProjectile = AssetDatabase.LoadAssetAtPath<GameObject>($"{ContentGen.PrefabRoot}/KunaiProjectile.prefab");
            var shurikenProjectile = EnsureShurikenProjectile(kunaiProjectile);

            var kunai = AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{WeaponFolder}/Kunai.asset");
            if (kunai != null && string.IsNullOrEmpty(kunai.DisplayName))
                ContentGen.Set(kunai, ("displayName", "Kunai"), ("description", "Fast, reliable throwing knife aimed at the nearest demon."));

            ContentGen.Define<ShurikenWeaponDefinition>($"{WeaponFolder}/Shuriken.asset",
                ("id", "Shuriken"), ("displayName", "Shuriken"),
                ("description", "Throws a full ring of piercing stars — weak alone, deadly in a crowd."),
                ("rarity", Rarity.Common), ("baseDamage", 6f), ("baseAttacksPerSecond", 0.8f), ("range", 7f),
                ("damagePerLevel", 1.2f), ("attacksPerSecondPerLevel", 0.04f),
                ("projectilePrefab", shurikenProjectile != null ? shurikenProjectile : kunaiProjectile),
                ("projectileSpeed", 10f), ("baseProjectileCount", 6));

            ContentGen.Define<KatanaWeaponDefinition>($"{WeaponFolder}/Katana.asset",
                ("id", "Katana"), ("displayName", "Katana"),
                ("description", "Wide sword arcs with high damage and critical hits, but short reach."),
                ("rarity", Rarity.Rare), ("baseDamage", 22f), ("baseAttacksPerSecond", 0.9f), ("range", 2.3f),
                ("baseCritChance", 0.15f), ("baseCritMultiplier", 2f), ("knockbackForce", 4f),
                ("damagePerLevel", 4f), ("attacksPerSecondPerLevel", 0.03f), ("arcDegrees", 120f));

            ContentGen.Define<BowWeaponDefinition>($"{WeaponFolder}/Bow.asset",
                ("id", "Bow"), ("displayName", "Bow"),
                ("description", "Very long range. Every third shot is a charged arrow."),
                ("rarity", Rarity.Rare), ("baseDamage", 14f), ("baseAttacksPerSecond", 0.9f), ("range", 11f),
                ("damagePerLevel", 2.5f), ("attacksPerSecondPerLevel", 0.03f),
                ("projectilePrefab", kunaiProjectile), ("projectileSpeed", 18f),
                ("shotsPerCharge", 3), ("chargedDamageMultiplier", 2.5f), ("chargedSpeedMultiplier", 1.5f));

            ContentGen.Define<ChainSickleWeaponDefinition>($"{WeaponFolder}/ChainSickle.asset",
                ("id", "ChainSickle"), ("displayName", "Chain Sickle"),
                ("description", "Hooks the nearest demon and yanks it toward you."),
                ("rarity", Rarity.Epic), ("baseDamage", 16f), ("baseAttacksPerSecond", 0.8f), ("range", 4.5f),
                ("damagePerLevel", 3f), ("attacksPerSecondPerLevel", 0.03f), ("knockbackForce", 3f), ("pullForce", 6f));

            var catalog = ContentGen.CreateOrLoad<WeaponCatalog>($"{ContentGen.CatalogRoot}/WeaponCatalog.asset");
            var weapons = ContentGen.FindAll<WeaponDefinition>(ContentGen.DataRoot)
                .Where(w => !string.IsNullOrEmpty(w.Id))
                .OrderBy(w => (int)w.Rarity).ThenBy(w => w.Id)
                .ToList();
            catalog.EditorSetItems(weapons);
        }

        /// <summary>A copy of the kunai projectile that pierces 2 enemies (Shuriken task: "Piercing projectile").</summary>
        private static GameObject EnsureShurikenProjectile(GameObject kunaiProjectile)
        {
            string path = $"{ContentGen.PrefabRoot}/ShurikenProjectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null || kunaiProjectile == null) return existing;

            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(kunaiProjectile), path)) return null;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.TryGetComponent<Projectile>(out var projectile))
                    ContentGen.Set(projectile, ("pierceCount", 2));
                root.transform.localScale *= 1.2f;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void GenerateEquipment()
        {
            // name, rarity, attack, attackSpeed, moveSpeed, crit, critMult, heal/s, description
            var pieces = new (string id, string name, Rarity rarity, float atk, float aspd, float move, float crit, float critMult, float heal, string desc)[]
            {
                ("iron_ring", "Iron Ring", Rarity.Common, 0.10f, 0f, 0f, 0f, 0f, 0f, "+10% attack damage."),
                ("ninja_headband", "Ninja Headband", Rarity.Common, 0f, 0.05f, 0f, 0f, 0f, 0f, "+5% attack speed."),
                ("shadow_cloak", "Shadow Cloak", Rarity.Rare, 0f, 0f, 0.08f, 0f, 0f, 0f, "+8% move speed."),
                ("jade_charm", "Jade Charm", Rarity.Rare, 0f, 0f, 0f, 0.05f, 0f, 0f, "+5% critical chance."),
                ("blood_talisman", "Blood Talisman", Rarity.Epic, 0.15f, 0f, 0f, 0.05f, 0f, 0f, "+15% attack damage, +5% critical chance."),
                ("dragon_scale", "Dragon Scale", Rarity.Legendary, 0.25f, 0f, 0f, 0f, 0.25f, 0.1f, "+25% attack, +0.25× crit damage, heals 0.1 HP/s."),
            };

            foreach (var p in pieces)
            {
                ContentGen.Define<EquipmentDefinition>($"{EquipmentFolder}/Equipment_{p.id}.asset",
                    ("id", p.id), ("displayName", p.name), ("description", p.desc), ("rarity", p.rarity),
                    ("attackDamageBonus", p.atk), ("attackSpeedBonus", p.aspd), ("moveSpeedBonus", p.move),
                    ("critChanceBonus", p.crit), ("critMultiplierBonus", p.critMult), ("healPerSecondBonus", p.heal));
            }

            var catalog = ContentGen.CreateOrLoad<EquipmentCatalog>($"{ContentGen.CatalogRoot}/EquipmentCatalog.asset");
            catalog.EditorSetItems(ContentGen.FindAll<EquipmentDefinition>(ContentGen.DataRoot)
                .Where(e => !string.IsNullOrEmpty(e.Id))
                .OrderBy(e => (int)e.Rarity).ThenBy(e => e.Id));
        }
    }
}
