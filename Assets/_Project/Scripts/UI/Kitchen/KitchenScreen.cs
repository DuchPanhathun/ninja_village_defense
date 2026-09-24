using System.Collections.Generic;
using System.Text;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Kitchen;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Kitchen
{
    /// <summary>
    /// The Kitchen (EPIC 24 Phase 2): the stoves (what's cooking, a live countdown, Collect when done), the
    /// meals picked for the next battle (<see cref="MealCards"/>), and every recipe with its boost, the
    /// ingredients you have against what it needs, and Cook. Opened from the Kitchen building or the village HUD.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class KitchenScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Kitchen;
        protected override string Title => "Kitchen";

        private readonly List<(int slot, TextMeshProUGUI text, Image fill, bool ready)> _stoves = new();
        private float _nextTick;

        protected override void Populate(RectTransform content)
        {
            _stoves.Clear();
            int level = KitchenService.KitchenLevel;
            InfoText.text = "Cook your harvest into meals. Take up to 2 meals into a battle: one of each is eaten as it starts and powers you up for the whole run.";
            if (level <= 0)
            {
                UIBuilder.Text(content, "The Kitchen isn't built yet. Tap it on the map (Castle Lv 2) to build it.",
                    UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }

            UIBuilder.SectionHeader(content, "On the stove");
            int slots = KitchenService.SlotCount;
            for (int slot = 0; slot < slots; slot++) Stove(content, slot);
            int nextLevel = NextLevelWithMoreSlots(level, slots);
            if (nextLevel > 0)
                UIBuilder.Text(content, $"Another stove at Kitchen Lv {nextLevel}.", UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);

            MealCards.Populate(content, Refresh);

            UIBuilder.SectionHeader(content, "Recipes");
            foreach (var recipe in KitchenService.GetRecipes()) Recipe(content, recipe);
        }

        private static int NextLevelWithMoreSlots(int level, int slots)
        {
            var kitchen = VillageService.Get(BuildingIds.Kitchen);
            if (kitchen == null) return 0;
            for (int l = level + 1; l <= kitchen.MaxLevel; l++)
                if (KitchenRules.Slots(l, kitchen.EffectAt(l)) > slots) return l;
            return 0;
        }

        // ------------------------------------------------------------------ stoves

        private void Stove(RectTransform content, int slot)
        {
            var job = KitchenService.GetJob(slot);
            var recipe = job != null ? KitchenService.GetRecipe(job.RecipeId) : null;
            if (job == null || recipe == null)
            {
                UIBuilder.ActionCard(content, $"Stove {slot + 1}  ·  free", "Pick a recipe below to start cooking.", out _, out _, UITheme.TextMuted,
                    UIArt.Get("item_rice"), new Color(1f, 1f, 1f, 0.35f));
                return;
            }

            bool ready = KitchenService.IsReady(slot);
            var actions = UIBuilder.ActionCard(content, $"Stove {slot + 1}  ·  {recipe.NameOrId}", StoveText(slot, ready),
                out _, out var body, ready ? UITheme.Gold : UITheme.Text, recipe.Output != null ? recipe.Output.Icon : recipe.Icon);
            var bar = UIBuilder.ProgressBar(actions.transform.parent, "Cooking", out var fill, UITheme.Gold, 26f);
            bar.transform.SetSiblingIndex(actions.transform.GetSiblingIndex());
            fill.fillAmount = KitchenRules.Progress(GameClock.UtcNow, job.StartTicks, job.ReadyTicks);
            if (ready)
            {
                int s = slot;
                UIBuilder.SmallButton(actions.transform, "Collect", () => Collect(s), UITheme.Button, 260f);
            }
            _stoves.Add((slot, body, fill, ready));
        }

        private static string StoveText(int slot, bool ready) => ready
            ? "<color=#FFD24D>Ready!</color> Collect it into the storehouse."
            : $"Cooking... {FarmRules.Format(KitchenService.TimeLeft(slot))} left";

        private void Collect(int slot)
        {
            if (KitchenService.Collect(slot, out var meal, out int amount) == KitchenResult.Success && meal != null)
            {
                Sfx.Play(AudioCueIds.RewardClaim);
                Toast($"+{amount} {meal.NameOrId}");
            }
            Refresh();
        }

        private void Update()
        {
            if (_stoves.Count == 0 || Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 0.5f;
            foreach (var (slot, text, fill, ready) in _stoves)
            {
                var job = KitchenService.GetJob(slot);
                if (job == null) continue;
                bool nowReady = KitchenService.IsReady(slot);
                if (nowReady != ready)
                {
                    Refresh(); // show Collect
                    return;
                }
                if (text != null) text.text = StoveText(slot, nowReady);
                if (fill != null) fill.fillAmount = KitchenRules.Progress(GameClock.UtcNow, job.StartTicks, job.ReadyTicks);
            }
        }

        // ------------------------------------------------------------------ recipes

        private void Recipe(RectTransform content, RecipeDefinition recipe)
        {
            var meal = recipe.Output;
            bool unlocked = KitchenService.IsUnlocked(recipe);
            var text = new StringBuilder();
            if (meal != null && meal.IsBattleMeal) text.Append($"<color=#9CFF8A>{meal.MealEffect}</color> for one battle\n");
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var ingredient = recipe.Ingredients[i];
                if (ingredient.Goods == null) continue;
                int have = GoodsService.Count(ingredient.Goods);
                string color = have >= ingredient.Amount ? "#E8D8B8" : "#FF8A7A";
                if (i > 0) text.Append("  ·  ");
                text.Append($"{ingredient.Amount} {ingredient.Goods.NameOrId} <color={color}>({have})</color>");
            }
            text.Append($"\nCooks in {FarmRules.Format(System.TimeSpan.FromSeconds(recipe.CookSeconds))}");

            var actions = UIBuilder.ActionCard(content, recipe.NameOrId, text.ToString(), out _, out _,
                unlocked ? UITheme.Text : UITheme.TextMuted, meal != null ? meal.Icon : recipe.Icon,
                unlocked ? (Color?)null : new Color(1f, 1f, 1f, 0.45f));
            var check = KitchenService.CheckCook(recipe);
            string label = unlocked ? "Cook" : $"Kitchen Lv {recipe.RequiredKitchenLevel}";
            var r = recipe;
            UIBuilder.SmallButton(actions.transform, label, () => Cook(r),
                check == KitchenResult.Success ? UITheme.Button : UITheme.ButtonSecondary, 280f);
        }

        private void Cook(RecipeDefinition recipe)
        {
            var result = KitchenService.Cook(recipe);
            if (result == KitchenResult.Success)
            {
                Sfx.Play(AudioCueIds.UiUpgrade);
                Toast($"Cooking {recipe.NameOrId}...");
            }
            else
            {
                Sfx.Play(AudioCueIds.UiError);
                Toast(KitchenService.Describe(result, recipe));
            }
            Refresh();
        }
    }
}
