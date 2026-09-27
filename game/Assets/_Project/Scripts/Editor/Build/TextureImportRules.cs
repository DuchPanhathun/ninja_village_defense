using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace NinjaVillage.EditorTools.Build
{
    /// <summary>
    /// Asset compression (EPIC 23): every texture imported under <c>Assets/_Project/Art/</c> gets
    /// mobile-friendly settings automatically — Sprite mode for Art/Sprites, no mipmaps (2D never
    /// needs them), a 2048 cap, and ASTC 6x6 on Android and iOS (good quality at ~3.6 bpp, supported
    /// by every device the Android-first plan targets). Artists just drop files in; nothing to set by hand.
    /// Settings are only applied on first import, so later manual tweaks in the Inspector stick.
    /// </summary>
    public class TextureImportRules : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/_Project/Art/";
        public const string SpriteRoot = "Assets/_Project/Art/Sprites/";
        public const int MaxSize = 2048;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return; // respect manual changes after the first import

            if (assetPath.StartsWith(SpriteRoot))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
            }
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = MaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;

            foreach (var platform in new[] { "Android", "iPhone" })
            {
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    maxTextureSize = MaxSize,
                    format = TextureImporterFormat.ASTC_6x6,
                    compressionQuality = 50,
                });
            }
        }

        /// <summary>Folder-based sprite atlas for all game sprites: new sprites join automatically.</summary>
        [MenuItem("Ninja Village/Build/Create Sprite Atlas", priority = 20)]
        public static void CreateSpriteAtlas()
        {
            const string atlasPath = "Assets/_Project/Art/GameSprites.spriteatlas";
            ContentGen.EnsureFolder(SpriteRoot.TrimEnd('/'));

            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false, // tight packing breaks sprites that use full rects in UI
                padding = 4,
            });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                generateMipMaps = false,
                filterMode = FilterMode.Bilinear,
                sRGB = true,
            });
            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var settings = atlas.GetPlatformSettings(platform);
                settings.overridden = true;
                settings.maxTextureSize = MaxSize;
                settings.format = TextureImporterFormat.ASTC_6x6;
                atlas.SetPlatformSettings(settings);
            }

            var folder = AssetDatabase.LoadAssetAtPath<Object>(SpriteRoot.TrimEnd('/'));
            if (folder != null && atlas.GetPackables().Length == 0)
                atlas.Add(new[] { folder });

            // Pack atlases into builds (and Play Mode) so sprites batch into few draw calls on mobile.
            EditorSettings.spritePackerMode = SpritePackerMode.AlwaysOnAtlas;
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TextureImportRules] Sprite atlas ready at {atlasPath}");
        }
    }
}
