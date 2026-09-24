using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Village;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The Shrine's blessings (EPIC 11 "Passive blessing"): +Health, +Critical, +Luck, +Coins... Each
    /// rank is permanent and applies to every run; the Shrine level caps how many ranks can be bought.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class ShrineScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Shrine;
        protected override string Title => "Shrine";

        protected override void Populate(RectTransform content)
        {
            InfoText.text = ShrineService.ShrineLevel > 0
                ? $"Shrine Lv {ShrineService.ShrineLevel} — blessings apply to every battle."
                : "Build the Shrine in the village to receive blessings.";

            var catalog = ShrineService.Catalog;
            if (catalog == null || catalog.All.Count == 0)
            {
                UIBuilder.Text(content, "No blessings found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }

            foreach (var blessing in catalog.All)
                if (blessing != null) AddBlessingCard(content, blessing);
        }

        private void AddBlessingCard(RectTransform content, BlessingDefinition blessing)
        {
            int rank = ShrineService.GetRank(blessing.Id);
            int cap = ShrineService.RankCap(blessing);
            string title = $"{blessing.NameOrId}   {rank}/{blessing.MaxRank}";
            string body = string.IsNullOrEmpty(blessing.Description) ? string.Empty : blessing.Description + "\n";
            body += rank > 0 ? $"Now: {blessing.FormatValue(blessing.ValueAt(rank))}" : "Not received yet";
            if (rank < blessing.MaxRank) body += $" · Next: {blessing.FormatValue(blessing.ValueAt(rank + 1))}";
            if (cap < blessing.MaxRank) body += $"\n<color=#AAAAB5>Shrine level allows rank {cap}</color>";

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, blessing.Color, UIIcons.Blessing(blessing.Id));
            var blocker = ShrineService.Check(blessing);
            if (blocker == BlessingBlocker.MaxRank)
            {
                UIBuilder.Text(actions.transform, "Fully blessed", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Gold);
                return;
            }

            var bless = UIBuilder.SmallButton(actions.transform, $"Bless {blessing.PriceForNextRank(rank)}", () =>
            {
                if (ShrineService.TryBless(blessing, out var b)) Sfx.Play(AudioCueIds.UiUpgrade);
                else
                {
                    Sfx.Play(AudioCueIds.UiError);
                    Toast(ShrineService.DescribeBlocker(b, blessing));
                }
                Refresh();
            }, null, 330f);

            if (blocker != BlessingBlocker.None)
            {
                UIBuilder.SetEnabled(bless, false);
                if (blocker != BlessingBlocker.NotEnoughCurrency)
                    UIBuilder.SetLabel(bless, ShrineService.DescribeBlocker(blocker, blessing));
            }
        }
    }
}
