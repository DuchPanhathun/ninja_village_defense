using System.Collections.Generic;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Mounts;
using NinjaVillage.Systems.Talents;
using UnityEditor;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Mounts: the pack's side-view horses, lions, lioness and donkey (from import_mounts() in the art importer), with
    /// their price, bonuses and where the rider sits. The two S-class mounts (gold / void recolours) come only from
    /// Surprise Boxes. Tune the table here.
    /// </summary>
    public static class MountGenerator
    {
        private const string Folder = ContentGen.DataRoot + "/Mounts";
        private const string Art = "Assets/_Project/Art/Sprites/";
        /// <summary>One pack pixel in world units (8x textures at 106.67 px per unit).</summary>
        private const float Px = 0.075f;

        // id, name, description, bonuses, currency, cost, S-class, rider offset in pack pixels from the rider's standing spot
        // (x back, y up; tuned so the face clears lions' manes and the legs hide behind the body)
        private static readonly (string id, string name, string desc, (TalentStat stat, float value)[] bonuses, CurrencyType currency, int cost,
            bool special, Vector2 rider)[] Mounts =
        {
            ("donkey", "Loyal Donkey", "Slow to anger, quick to trot. Cheap and cheerful.",
                new[] { (TalentStat.MoveSpeed, 0.05f), (TalentStat.MaxHealth, 0.06f) }, CurrencyType.Coins, 800, false, new Vector2(-2, 10)),
            ("horse_brown", "Brown Horse", "A steady steed for every ninja.",
                new[] { (TalentStat.MoveSpeed, 0.08f), (TalentStat.MaxHealth, 0.04f) }, CurrencyType.Coins, 1500, false, new Vector2(-2, 10)),
            ("lioness", "Sand Lioness", "Stalks the dunes; strikes where it hurts.",
                new[] { (TalentStat.MoveSpeed, 0.1f), (TalentStat.CritChance, 0.04f) }, CurrencyType.Coins, 4000, false, new Vector2(-7, 10)),
            ("horse_black", "Shadow Steed", "Swift as night — hard to hit at full gallop.",
                new[] { (TalentStat.MoveSpeed, 0.12f), (TalentStat.DodgeChance, 0.03f) }, CurrencyType.Gems, 60, false, new Vector2(-2, 10)),
            ("lion_red", "Blaze Lion", "Its roar sets the battlefield alight.",
                new[] { (TalentStat.MoveSpeed, 0.1f), (TalentStat.AttackDamage, 0.08f) }, CurrencyType.Gems, 150, false, new Vector2(-8, 10)),
            ("lion_frost", "Frost Lion", "A mountain king with a hide like ice.",
                new[] { (TalentStat.MoveSpeed, 0.1f), (TalentStat.MaxHealth, 0.12f) }, CurrencyType.Gems, 150, false, new Vector2(-8, 10)),
            ("golden_qilin", "Golden Qilin", "S-class. A sacred beast of gold and light.",
                new[] { (TalentStat.MoveSpeed, 0.15f), (TalentStat.AttackDamage, 0.1f), (TalentStat.MaxHealth, 0.1f) }, CurrencyType.Gems, 0, true, new Vector2(-8, 11)),
            ("nightmare", "Nightmare Steed", "S-class. Rides out of the void, faster than fear.",
                new[] { (TalentStat.MoveSpeed, 0.18f), (TalentStat.CritChance, 0.06f), (TalentStat.DodgeChance, 0.04f) }, CurrencyType.Gems, 0, true, new Vector2(-2, 11)),
        };

        [ContentGenerator("Mounts", 22)]
        public static void Generate()
        {
            var mounts = new List<MountDefinition>();
            for (int i = 0; i < Mounts.Length; i++)
            {
                var (id, name, desc, bonuses, currency, cost, special, rider) = Mounts[i];
                var frames = new List<object>();
                for (int f = 0; ; f++)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}Characters/Mounts/mount_{id}_{f}.png");
                    if (sprite == null) break;
                    frames.Add(sprite);
                }
                if (frames.Count == 0) { Debug.LogWarning($"[Mounts] Missing frames for {id}."); continue; }

                var mount = ContentGen.CreateOrLoad<MountDefinition>($"{Folder}/Mount_{id}.asset");
                ContentGen.Set(mount, ("id", id), ("displayName", name), ("description", desc),
                    ("icon", AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}UI/Icons/icon_mount_{id}.png")),
                    ("frames", frames), ("fps", 8f), ("riderOffset", rider * Px),
                    ("unlockCurrency", (int)currency), ("unlockCost", cost), ("sClass", special), ("sortOrder", i));

                var so = new SerializedObject(mount);
                var list = so.FindProperty("bonuses");
                list.arraySize = bonuses.Length;
                for (int b = 0; b < bonuses.Length; b++)
                {
                    var entry = list.GetArrayElementAtIndex(b);
                    entry.FindPropertyRelative("Stat").intValue = (int)bonuses[b].stat;
                    entry.FindPropertyRelative("Value").floatValue = bonuses[b].value;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mount);
                mounts.Add(mount);
            }
            ContentGen.CreateOrLoad<MountCatalog>($"{ContentGen.CatalogRoot}/MountCatalog.asset").EditorSetItems(mounts);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mounts] {mounts.Count} mounts.");
        }
    }
}
