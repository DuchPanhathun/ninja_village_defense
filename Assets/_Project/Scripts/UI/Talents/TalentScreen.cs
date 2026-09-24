using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Talents;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Talents
{
    /// <summary>
    /// The talent tree (EPIC 14): one tab per category, nodes listed by tier with their rank, effect,
    /// prerequisites and cost, plus a reset button (full coin refund, gem cost after the first reset)
    /// that asks for confirmation.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu, SceneNames.Village)]
    public class TalentScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Talents;
        protected override string Title => "Talents";

        private TalentCategory _category = TalentCategory.Offense;
        private bool _confirmingReset;
        private Button _resetButton;
        private readonly Button[] _tabs = new Button[3];

        protected override void BuildTop(RectTransform body)
        {
            var tabs = UIBuilder.Horizontal(body, "Tabs", 12f);
            UIBuilder.SetPreferredSize(tabs, -1f, 100f);
            for (int i = 0; i < 3; i++)
            {
                var category = (TalentCategory)i;
                _tabs[i] = UIBuilder.Button(tabs.transform, category.ToString(), () =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    _category = category;
                    _confirmingReset = false;
                    Refresh();
                }, UITheme.ButtonSecondary, 100f);
            }
        }

        protected override void BuildBottom(RectTransform body)
        {
            _resetButton = UIBuilder.Button(body, "Reset talents", OnResetPressed, UITheme.ButtonSecondary, 100f);
        }

        protected override void OnHidden() => _confirmingReset = false;

        protected override void Populate(RectTransform content)
        {
            for (int i = 0; i < _tabs.Length; i++)
                UIBuilder.SetEnabled(_tabs[i], true, i == (int)_category ? UITheme.Button : UITheme.ButtonSecondary);

            InfoText.text = $"{TalentService.TotalRanks} ranks learned · {TalentService.CoinsSpent} coins invested. " +
                            "Talents apply to every run.";

            int gemCost = TalentService.ResetGemCost;
            string resetLabel = _confirmingReset
                ? $"Tap again to reset (refund {TalentService.CoinsSpent} coins{(gemCost > 0 ? $", costs {gemCost} gems" : ", free")})"
                : "Reset talents";
            UIBuilder.SetLabel(_resetButton, resetLabel);
            UIBuilder.SetEnabled(_resetButton, TalentService.TotalRanks > 0, _confirmingReset ? UITheme.Negative : UITheme.ButtonSecondary);

            var talents = TalentService.GetCategory(_category);
            if (talents.Count == 0)
            {
                UIBuilder.Text(content, "No talents found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }

            int currentTier = -1;
            foreach (var talent in talents)
            {
                if (talent.Tier != currentTier)
                {
                    currentTier = talent.Tier;
                    UIBuilder.SectionHeader(content, $"Tier {currentTier + 1}");
                }
                AddTalentCard(content, talent);
            }
        }

        private void AddTalentCard(RectTransform content, TalentDefinition talent)
        {
            int rank = TalentService.GetRank(talent);
            string title = $"{talent.NameOrId}   {rank}/{talent.MaxRank}";
            string body = $"{TalentService.FormatValue(talent.Stat, talent.ValuePerRank)} per rank" +
                          (rank > 0 ? $" (now {TalentService.FormatValue(talent.Stat, talent.ValueAt(rank))})" : string.Empty);
            if (!string.IsNullOrEmpty(talent.Description)) body = talent.Description + "\n" + body;

            var check = TalentService.CheckRankUp(talent);
            if (check == TalentResult.PrerequisitesMissing)
                body += $"\n<color=#F25A5A>{TalentService.Describe(check, talent)}</color>";

            Color accent = rank >= talent.MaxRank ? UITheme.Gold : rank > 0 ? UITheme.Positive : UITheme.Text;
            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, accent);

            if (check == TalentResult.MaxRank)
            {
                UIBuilder.Text(actions.transform, "Mastered", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Gold);
                return;
            }

            var learn = UIBuilder.SmallButton(actions.transform, $"Learn {talent.CostForNextRank(rank)} Coins", () =>
            {
                var result = TalentService.TryRankUp(talent);
                if (result != TalentResult.Success)
                {
                    Sfx.Play(AudioCueIds.UiError);
                    Toast(TalentService.Describe(result, talent));
                }
                Refresh();
            }, null, 340f);
            if (check != TalentResult.Success) UIBuilder.SetEnabled(learn, false);
        }

        private void OnResetPressed()
        {
            if (!_confirmingReset)
            {
                Sfx.Play(AudioCueIds.UiClick);
                _confirmingReset = true;
                Refresh();
                return;
            }

            _confirmingReset = false;
            var result = TalentService.TryReset();
            if (result == TalentResult.Success)
            {
                Sfx.Play(AudioCueIds.RewardClaim);
                Toast("Talents reset — coins refunded");
            }
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(TalentService.Describe(result));
            }
            Refresh();
        }
    }
}
