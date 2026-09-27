using UnityEngine;

namespace NinjaVillage.Core.ScriptableObjects
{
    /// <summary>
    /// Base class for all data-definition ScriptableObjects in the game
    /// (weapons, skills, enemies, heroes, pets, buildings, ...).
    ///
    /// The design is data-driven: designers create asset instances instead of
    /// touching code, and every definition carries a stable <see cref="Id"/>
    /// plus presentation fields used by the UI and save system.
    /// </summary>
    public abstract class DescriptiveScriptableObject : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id used by save data and lookups. Never rename once shipped.")]
        [SerializeField] private string id;

        [Header("Presentation")]
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        /// <summary>
        /// Auto-fills a missing id from the asset name in the Editor so no definition
        /// ever ships without one.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = name;
        }
#endif
    }
}
