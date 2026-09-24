using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Requests;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// Today's villager requests (EPIC 24 Phase 3): who asks, what they say, progress, the reward, and Deliver
    /// once it's done. A shortcut takes you where the work is (the farm for crops, the Kitchen for meals).
    /// Opened by tapping a villager with a "!" (that request comes first) or the HUD's Requests button.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class RequestsScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Requests;
        protected override string Title => "Villager Requests";

        private static int _focus = -1;

        /// <summary>Opens the requests with request <paramref name="index"/> (tapped on the map) at the top.</summary>
        public static void Open(int index)
        {
            _focus = index;
            Sfx.Play(AudioCueIds.UiClick);
            UIScreenNavigator.Instance.Show(ScreenIds.Requests);
        }

        protected override void Populate(RectTransform content)
        {
            var active = RequestService.Active;
            InfoText.text = $"Villagers ask for a hand every day. New requests in <color=#FFD24D>{GameClock.FormatCountdown(GameClock.UntilNextDailyReset)}</color>.";
            if (active.Count == 0)
            {
                UIBuilder.Text(content, "Nobody needs anything right now. Check back tomorrow!", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }

            var order = new List<int>();
            if (_focus >= 0 && _focus < active.Count) order.Add(_focus);
            for (int i = 0; i < active.Count; i++)
                if (i != _focus) order.Add(i);
            foreach (int i in order) Card(content, active[i], i == _focus);
        }

        protected override void OnHidden() => _focus = -1;

        private void Card(RectTransform content, VillagerRequestState state, bool focused)
        {
            var request = RequestService.Get(state.Id);
            if (request == null) return;
            bool ready = RequestService.CanDeliver(state);
            int progress = RequestService.Progress(state), target = RequestService.Target(state);

            string status = state.Delivered ? "<color=#9CFF8A>Delivered. Thank you!</color>"
                : request.Kind == RequestKind.Chapter ? (ready ? "<color=#9CFF8A>Cleared!</color>" : "Not cleared yet")
                : $"{(ready ? "<color=#9CFF8A>" : "")}{progress}/{target}{(ready ? "</color>" : "")}" +
                  (request.Kind == RequestKind.Deliver ? " in the storehouse" : "");
            string body = $"\"{request.Line}\"\n{status}\nReward: {RequestService.DescribeReward(request)}";

            var set = CharacterSpriteLibrary.Find(state.VillagerKey);
            var actions = UIBuilder.ActionCard(content, $"{RequestService.VillagerName(state)}: {RequestService.Title(state)}", body,
                out _, out _, state.Delivered ? UITheme.TextMuted : ready || focused ? UITheme.Gold : UITheme.Text,
                set != null ? set.DefaultSprite : request.Icon);

            if (!state.Delivered)
            {
                string shortcut = Shortcut(request);
                if (shortcut != null) UIBuilder.SmallButton(actions.transform, shortcut, () => GoDoIt(request), UITheme.ButtonSecondary, 220f);
                var deliver = UIBuilder.SmallButton(actions.transform, request.Kind == RequestKind.Deliver ? "Deliver" : "Claim",
                    () => Deliver(state), UITheme.Button, 240f);
                UIBuilder.SetEnabled(deliver, ready);
            }
        }

        private static string Shortcut(VillagerRequestDefinition request)
        {
            if (request.Kind != RequestKind.Deliver || request.Goods == null) return null;
            if (request.Goods.Category == GoodsCategory.Crop) return "Farm";
            if (request.Goods.Category == GoodsCategory.Meal && VillageService.GetLevel(BuildingIds.Kitchen) > 0) return "Kitchen";
            return null;
        }

        private static void GoDoIt(VillagerRequestDefinition request)
        {
            Sfx.Play(AudioCueIds.UiClick);
            var navigator = UIScreenNavigator.Instance;
            if (request.Goods.Category == GoodsCategory.Meal)
            {
                navigator.Show(ScreenIds.Kitchen);
                return;
            }
            navigator.Back();
            var cam = UnityEngine.Camera.main;
            var controller = cam != null ? cam.GetComponent<VillageCameraController>() : null;
            if (controller != null) controller.FocusOn(VillageLayout.FarmCenter);
        }

        private void Deliver(VillagerRequestState state)
        {
            if (RequestService.Deliver(state, out var reward))
            {
                Sfx.Play(AudioCueIds.RewardClaim);
                Toast(reward.Decoration != null
                    ? $"Thanks! +{reward}. Place your gift from the Decorate shop."
                    : $"Thanks! +{reward}");
            }
            else
            {
                Sfx.Play(AudioCueIds.UiError);
            }
            Refresh();
        }
    }
}
