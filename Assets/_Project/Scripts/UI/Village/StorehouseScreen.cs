using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The village storehouse: everything harvested and cooked (later fished, mined) with its count, and
    /// Sell 1 / Sell all for coins. Opened from the farm sign or the farm bar.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class StorehouseScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Storehouse;
        protected override string Title => "Storehouse";

        protected override void Populate(RectTransform content)
        {
            InfoText.text = "Your harvest and meals. Sell them for coins, or cook crops into meals in the Kitchen — meals power up your battles.";
            var stock = GoodsService.InStock();
            if (stock.Count == 0)
            {
                UIBuilder.Text(content, "Nothing stored yet. Plant seeds on the farm and harvest them when they're ripe!",
                    UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }
            foreach (var (goods, count) in stock)
            {
                string meal = goods.IsBattleMeal ? $"\n<color=#9CFF8A>Battle meal: {goods.MealEffect}</color>" : "";
                var actions = UIBuilder.ActionCard(content, $"{goods.NameOrId}   ×{count}",
                    $"{goods.Description}{meal}\nSells for <color=#FFD24D>{goods.SellPrice}</color> coins each", out _, out _, UITheme.Text, goods.Icon);
                var g = goods;
                UIBuilder.SmallButton(actions.transform, "Sell 1", () => Sell(g, 1), UITheme.ButtonSecondary, 200f);
                UIBuilder.SmallButton(actions.transform, $"Sell all +{count * goods.SellPrice}", () => Sell(g, int.MaxValue), UITheme.Button, 320f);
            }
        }

        private void Sell(GoodsDefinition goods, int amount)
        {
            int coins = GoodsService.Sell(goods, amount);
            if (coins > 0)
            {
                Sfx.Play(AudioCueIds.UiPurchase);
                Toast($"+{coins} coins");
            }
            Refresh();
        }
    }
}
