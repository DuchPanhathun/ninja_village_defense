using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Village;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>The farm's signpost: names the field and opens the Storehouse (your harvest) when tapped.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class FarmSignDisplay : MonoBehaviour, IVillageTappable
    {
        public void Build(VillageArt art)
        {
            var sprite = art != null ? art.FarmSign : null;
            var post = GeneratedSprites.CreateRenderer(transform, "Sign", sprite != null ? sprite : GeneratedSprites.Square,
                sprite != null ? Color.white : new Color(0.55f, 0.36f, 0.2f), VillageSorting.Order(transform.position.y));
            float height = sprite != null ? sprite.bounds.size.y : 1f;
            post.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            if (sprite == null) post.transform.localScale = new Vector3(0.8f, 1f, 1f);

            var label = new GameObject("Label").AddComponent<TextMeshPro>();
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, height + 0.55f, 0f);
            label.text = "Farm\n<size=70%><color=#E8D8B8>Storehouse</color></size>";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 2.6f;
            label.fontStyle = FontStyles.Bold;
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(20, 27, 27, 255);
            label.rectTransform.sizeDelta = new Vector2(5f, 1.4f);
            label.sortingOrder = VillageSorting.Labels;

            var box = GetComponent<BoxCollider2D>();
            box.size = new Vector2(1.6f, height + 1.2f);
            box.offset = new Vector2(0f, (height + 1.2f) * 0.5f);
        }

        public void OnTapped()
        {
            if (VillageVisit.IsVisiting) return;
            EventBus<VillageDisplayTappedEvent>.Raise(new VillageDisplayTappedEvent(VillageDisplayKind.Storehouse));
        }
    }
}
