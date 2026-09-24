using System;
using System.Collections.Generic;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// "Something to claim here" indicators for menu buttons. Systems register a check per screen id
    /// (the Daily system: login reward ready / quest complete), and menus ask <see cref="Has"/> when
    /// drawing their buttons — so the Home screen shows badges without referencing those systems.
    /// </summary>
    public static class ScreenBadges
    {
        private static readonly Dictionary<string, Func<bool>> Checks = new();

        public static void Register(string screenId, Func<bool> hasBadge)
        {
            if (string.IsNullOrEmpty(screenId) || hasBadge == null) return;
            Checks[screenId] = hasBadge;
        }

        public static bool Has(string screenId)
        {
            if (string.IsNullOrEmpty(screenId) || !Checks.TryGetValue(screenId, out var check)) return false;
            try { return check(); }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
        }
    }
}
