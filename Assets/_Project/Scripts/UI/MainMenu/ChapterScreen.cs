using System.Linq;
using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Chapters;
using NinjaVillage.Systems.Chapters;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using UnityEngine;

namespace NinjaVillage.UI.MainMenu
{
    /// <summary>
    /// The chapter map as a list: every chapter with its final boss, enemies, bosses along the way, wave
    /// count, best wave and first-clear reward. Unlocked chapters can be selected (or played straight away);
    /// locked ones say which chapter to clear first.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class ChapterScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Chapters;
        protected override string Title => "Chapters";

        protected override void Populate(RectTransform content)
        {
            var chapters = ChapterService.GetChapters();
            InfoText.text = $"Cleared {ChapterService.HighestCleared}/{chapters.Count} chapters. Beat a chapter's final boss to unlock the next.";
            if (chapters.Count == 0)
            {
                UIBuilder.Text(content, "No chapters found. Run Ninja Village > Generate Default Content.", UITheme.BodySize);
                return;
            }
            foreach (var chapter in chapters)
                AddCard(content, chapter);
        }

        private void AddCard(RectTransform content, ChapterDefinition chapter)
        {
            bool unlocked = ChapterService.IsUnlocked(chapter);
            bool cleared = ChapterService.IsCleared(chapter);
            bool selected = ChapterService.Selected == chapter;
            var boss = chapter.FinalBoss;

            string status = cleared ? "  <color=#FFD24D>CLEARED</color>" : unlocked ? "" : "  (locked)";
            string title = $"Chapter {chapter.Number}: {chapter.DisplayName}{status}";

            var enemies = string.Join(", ", chapter.Enemies().Select(e => e.DisplayName));
            var bosses = chapter.Bosses();
            string bossLine = bosses.Count > 1
                ? $"Bosses: {string.Join(" > ", bosses.Take(bosses.Count - 1).Select(b => b.DisplayName))} > <b>{bosses[^1].DisplayName}</b>"
                : boss != null ? $"Final boss: <b>{boss.DisplayName}</b>" : "";
            int best = ChapterService.BestWave(chapter);
            string body = $"{chapter.Description}\nEnemies: {enemies}\n{bossLine}\n" +
                          $"{chapter.WaveCount} waves" + (best > 0 ? $"  ·  Best: wave {Mathf.Min(best, chapter.WaveCount)}/{chapter.WaveCount}" : "") +
                          (cleared ? "" : $"\nFirst clear: {chapter.ClearCoins} coins + {chapter.ClearGems} gems");
            if (!unlocked) body += $"\n<color=#F25A5A>Clear Chapter {chapter.Number - 1} to unlock.</color>";

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, unlocked ? chapter.ThemeColor : UITheme.TextMuted,
                UIIcons.Enemy(boss), unlocked ? Color.white : new Color(0.2f, 0.16f, 0.14f));
            if (!unlocked) return;

            if (selected)
            {
                var chosen = UIBuilder.SmallButton(actions.transform, "Selected", null, UITheme.ButtonSecondary, 220f);
                chosen.interactable = false;
            }
            else
            {
                UIBuilder.SmallButton(actions.transform, "Select", () =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    ChapterService.TrySelect(chapter);
                    Refresh();
                }, UITheme.ButtonSecondary, 220f);
            }
            UIBuilder.SmallButton(actions.transform, "Play", () =>
            {
                ChapterService.TrySelect(chapter);
                SceneLoader.LoadBattle();
            }, UITheme.Button, 220f);
        }
    }
}
