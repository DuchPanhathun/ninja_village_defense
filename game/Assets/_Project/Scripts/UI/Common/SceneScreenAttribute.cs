using System;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Declares which scenes a <see cref="UIScreen"/> belongs in (<c>SceneNames</c> values). The
    /// Editor scene builder (Ninja Village → Build Scenes) adds every tagged screen to those scenes,
    /// so adding a new screen never requires hand-editing a scene.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SceneScreenAttribute : Attribute
    {
        public string[] Scenes { get; }
        public SceneScreenAttribute(params string[] scenes) => Scenes = scenes ?? Array.Empty<string>();
    }
}
