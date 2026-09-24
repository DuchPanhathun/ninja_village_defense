using System.IO;
using NinjaVillage.EditorTools.Generators;
using UnityEditor;

namespace NinjaVillage.EditorTools.Build
{
    /// <summary>
    /// First-import settings for world sprites (characters, projectiles, pickups, VFX, environment),
    /// alongside <see cref="TextureImportRules"/>: one pixel density for everything drawn in the world
    /// (16 source px = 1.2 world units, see <see cref="ArtHookupGenerator"/>), and Full Rect meshes for the
    /// tiling ground textures. UI sprites keep the default. Later manual changes in the Inspector stick.
    /// </summary>
    public class WorldSpriteImportRules : AssetPostprocessor
    {
        private static readonly string[] WorldFolders = { "Characters/", "Projectiles/", "Pickups/", "VFX/", "Environment/" };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtHookupGenerator.SpriteRoot + "/")) return;
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            string relative = assetPath.Substring(ArtHookupGenerator.SpriteRoot.Length + 1);
            bool world = false;
            foreach (var folder in WorldFolders)
                if (relative.StartsWith(folder)) world = true;
            if (!world) return;

            importer.spritePixelsPerUnit = ArtHookupGenerator.WorldPixelsPerUnit;
            if (Path.GetFileName(assetPath).StartsWith("bg_ground_"))
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
        }
    }
}
