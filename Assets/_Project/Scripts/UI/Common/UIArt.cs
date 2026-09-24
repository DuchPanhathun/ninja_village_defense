using System;
using System.Collections.Generic;
using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Every UI sprite by file name (<c>menu_shop</c>, <c>panel_wood_panel</c>, <c>portrait_hero_monk</c>…)
    /// so code-built screens can use the imported pixel art. Lives in <c>Resources/Catalogs/UIArt</c>;
    /// built by the art hookup generator from <c>Art/Sprites/UI</c> and <c>Art/Sprites/Pickups</c>.
    /// <see cref="Get"/> returns null when a sprite (or the whole catalog) is missing, so screens fall back
    /// to plain colours.
    /// </summary>
    public class UIArt : ScriptableObject
    {
        [SerializeField] private Sprite[] sprites = Array.Empty<Sprite>();

        private Dictionary<string, Sprite> _byName;
        private static UIArt _instance;
        private static bool _loaded;

        public static Sprite Get(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            if (!_loaded)
            {
                _loaded = true;
                _instance = Resources.Load<UIArt>($"{CatalogLoader.ResourcesFolder}/{nameof(UIArt)}");
            }
            if (_instance == null) return null;
            if (_instance._byName == null)
            {
                _instance._byName = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (var sprite in _instance.sprites)
                    if (sprite != null) _instance._byName[sprite.name] = sprite;
            }
            return _instance._byName.TryGetValue(spriteName, out var found) ? found : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _instance = null;
            _loaded = false;
        }

#if UNITY_EDITOR
        public void EditorSetSprites(Sprite[] value)
        {
            sprites = value;
            _byName = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
