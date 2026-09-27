using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// The village notice board by the plaza: whose village this is and their proudest numbers — best wave,
    /// chapters cleared, trophies, demons defeated — what a visitor reads first. Tap (own village) to open
    /// the Profile screen.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ProfileBoardDisplay : MonoBehaviour, IVillageTappable
    {
        private SpriteRenderer _board;
        private TextMeshPro _title, _lines;

        public void Show(VillageArt art, VillageSnapshot snapshot)
        {
            if (_board == null) Build(art);
            string owner = string.IsNullOrEmpty(snapshot.DisplayName) ? "Ninja" : snapshot.DisplayName;
            _title.text = $"{owner}'s Village";
            _lines.text = $"Best wave <color=#FFD24D>{snapshot.HighestWave}</color>   Chapters <color=#9CFF8A>{snapshot.ChaptersCleared}</color>\n" +
                          $"Trophies <color=#FFB0DA>{snapshot.AchievementTiers}</color>   Demons defeated <color=#FF8A7A>{snapshot.TotalKills}</color>";
        }

        private void Build(VillageArt art)
        {
            var sprite = art != null ? art.NoticeBoard : null;
            float boardY = 1.05f;
            int order = VillageSorting.Order(transform.position.y);
            for (int i = -1; i <= 1; i += 2)
                GeneratedSprites.CreateRenderer(transform, "Post", GeneratedSprites.Square, new Color(0.36f, 0.22f, 0.13f), order,
                    new Vector2(i * 1.3f, 0.55f), new Vector2(0.18f, 1.1f));
            _board = GeneratedSprites.CreateRenderer(transform, "Board", sprite != null ? sprite : GeneratedSprites.Square,
                sprite != null ? Color.white : new Color(0.62f, 0.42f, 0.25f), order + 1, new Vector2(0f, boardY));
            if (sprite == null) _board.transform.localScale = new Vector3(3.4f, 1.1f, 1f);
            else _board.transform.localScale = Vector3.one * 1.2f;

            _title = Label("Title", new Vector2(0f, boardY + 1.55f), 2.8f, Color.white);
            _lines = Label("Lines", new Vector2(0f, boardY + 0.95f), 1.9f, new Color(1f, 0.95f, 0.85f));

            var box = GetComponent<BoxCollider2D>();
            box.size = new Vector2(3.6f, 2.4f);
            box.offset = new Vector2(0f, 1.2f);
        }

        private TextMeshPro Label(string name, Vector2 position, float size, Color color)
        {
            var label = new GameObject(name).AddComponent<TextMeshPro>();
            label.transform.SetParent(transform, false);
            label.transform.localPosition = position;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(20, 27, 27, 255);
            label.rectTransform.sizeDelta = new Vector2(8f, 1.2f);
            label.sortingOrder = VillageSorting.Labels;
            return label;
        }

        public void OnTapped()
        {
            if (VillageVisit.IsVisiting) return;
            EventBus<VillageDisplayTappedEvent>.Raise(new VillageDisplayTappedEvent(VillageDisplayKind.Profile));
        }
    }
}
