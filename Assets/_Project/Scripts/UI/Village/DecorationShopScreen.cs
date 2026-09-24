using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The Decorate shop: every decoration by category with its picture and price. "Buy" closes the shop and
    /// starts placing it at the middle of the view — it's only paid for once it's placed.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class DecorationShopScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Decorations;
        protected override string Title => "Decorate";

        private static readonly (DecorationCategory category, string title)[] Sections =
        {
            (DecorationCategory.Nature, "Flowers & Trees"), (DecorationCategory.Village, "Village Life"),
            (DecorationCategory.Statues, "Statues"), (DecorationCategory.Banners, "Banners & Flags"),
            (DecorationCategory.Special, "Rare Crystals"),
        };

        protected override void Populate(RectTransform content)
        {
            InfoText.text = "Buy a decoration, then drag it where it looks best. Tap a placed decoration later to move or sell it (half price back).";
            var items = DecorationService.GetShopItems();
            if (items.Count == 0)
            {
                UIBuilder.Text(content, "No decorations found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }
            foreach (var (category, title) in Sections)
            {
                bool header = false;
                foreach (var item in items)
                {
                    if (item.Category != category) continue;
                    if (!header)
                    {
                        UIBuilder.SectionHeader(content, title);
                        header = true;
                    }
                    AddCard(content, item);
                }
            }
        }

        private void AddCard(RectTransform content, DecorationDefinition item)
        {
            int placed = DecorationService.CountPlaced(item);
            string body = item.Description + (placed > 0 ? $"\n<color=#9CFF8A>In your village: {placed}</color>" : "");
            var actions = UIBuilder.ActionCard(content, item.NameOrId, body, out _, out _,
                item.Price.Currency == CurrencyType.Gems ? UITheme.Gem : UITheme.Text, item.Icon);
            var buy = UIBuilder.SmallButton(actions.transform, $"Buy {item.Price}", () => Buy(item),
                item.Price.Currency == CurrencyType.Gems ? UITheme.Gem : UITheme.Button, 320f);
            if (!CurrencyService.CanAfford(item.Price)) UIBuilder.SetEnabled(buy, false);
        }

        private static void Buy(DecorationDefinition item)
        {
            var placer = DecorationPlacer.Instance;
            if (placer == null) return;
            Sfx.Play(AudioCueIds.UiClick);
            UIScreenNavigator.Instance.Back(); // back to the map
            var cam = UnityEngine.Camera.main;
            Vector2 start = cam != null ? (Vector2)cam.transform.position : Vector2.zero;
            placer.Begin(item, FreeSpotNear(item, start));
        }

        /// <summary>The nearest spot to <paramref name="start"/> where <paramref name="item"/> fits (spiralling out), else the start.</summary>
        private static Vector2 FreeSpotNear(DecorationDefinition item, Vector2 start)
        {
            start = DecorationRules.Snap(start);
            if (DecorationService.CheckPlacement(item, start) == PlacementBlocker.None) return start;
            for (float r = DecorationRules.Grid; r <= 8f; r += DecorationRules.Grid)
                for (int i = 0; i < 16; i++)
                {
                    float a = i / 16f * Mathf.PI * 2f;
                    var p = DecorationRules.Snap(start + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                    if (DecorationService.CheckPlacement(item, p) == PlacementBlocker.None) return p;
                }
            return start;
        }
    }
}
