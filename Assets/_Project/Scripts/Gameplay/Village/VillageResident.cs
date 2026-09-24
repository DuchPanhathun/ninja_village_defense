using System;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Animation;
using TMPro;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>
    /// Someone living on the village map — one of your heroes, a pet, a townsperson or a farm animal —
    /// drawn with its animated pack sprites. It wanders between random spots in its area (or trots after
    /// another resident: the active pet follows the selected hero), faces where it walks, sorts by depth,
    /// and on tap either runs its action (heroes/pets open their screen) and/or says something in a bubble.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class VillageResident : MonoBehaviour, IVillageTappable
    {
        private SpriteRenderer _renderer;
        private Rect _area;
        private Vector2 _target;
        private float _speed;
        private float _idleUntil;
        private Transform _follow;
        private Vector2 _followOffset;
        private Action _onTap;
        private Func<string> _speech;
        private TextMeshPro _tag;
        private GameObject _bubble;
        private TextMeshPro _bubbleText;
        private float _bubbleHideAt;

        public static VillageResident Spawn(Transform parent, string name, CharacterSpriteSet set, Vector2 position, Rect area, float speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var resident = go.AddComponent<VillageResident>();
            resident._area = area;
            resident._speed = speed;
            resident._renderer = go.AddComponent<SpriteRenderer>();
            if (set != null)
            {
                resident._renderer.sprite = set.DefaultSprite;
                go.AddComponent<SpriteFrameAnimator>().SetSpriteSet(set);
            }
            else
            {
                resident._renderer.sprite = GeneratedSprites.Circle; // no art: a dot beats nothing
            }
            var collider = go.GetComponent<CircleCollider2D>();
            collider.radius = 0.55f;
            resident._idleUntil = Time.time + UnityEngine.Random.Range(0f, 2f);
            resident.PickTarget();
            return resident;
        }

        public VillageResident Follow(Transform target, Vector2 offset)
        {
            _follow = target;
            _followOffset = offset;
            return this;
        }

        public VillageResident OnTap(Action action)
        {
            _onTap = action;
            return this;
        }

        /// <summary>What it says when tapped (a new line each time).</summary>
        public VillageResident Says(Func<string> speech)
        {
            _speech = speech;
            return this;
        }

        /// <summary>A small name tag above the head (e.g. the selected hero's star).</summary>
        public VillageResident WithTag(string text, Color color)
        {
            if (_tag == null)
            {
                _tag = new GameObject("Tag").AddComponent<TextMeshPro>();
                _tag.transform.SetParent(transform, false);
                _tag.alignment = TextAlignmentOptions.Center;
                _tag.fontSize = 2.2f;
                _tag.fontStyle = FontStyles.Bold;
                _tag.outlineWidth = 0.25f;
                _tag.outlineColor = new Color32(20, 27, 27, 255);
                _tag.rectTransform.sizeDelta = new Vector2(5f, 1f);
                _tag.sortingOrder = VillageSorting.Labels;
            }
            _tag.text = text;
            _tag.color = color;
            _tag.transform.localPosition = new Vector3(0f, (_renderer.sprite != null ? _renderer.sprite.bounds.extents.y : 0.6f) + 0.35f, 0f);
            return this;
        }

        private void Update()
        {
            Vector2 pos = transform.position;
            Vector2 goal = _follow != null ? (Vector2)_follow.position + _followOffset : _target;
            Vector2 toGoal = goal - pos;
            bool moving = false;

            if (_follow != null)
            {
                // Trot after the leader, stopping a little short.
                if (toGoal.sqrMagnitude > 0.25f)
                {
                    float speed = _speed * (toGoal.magnitude > 3f ? 2f : 1f);
                    transform.position = pos + toGoal.normalized * Mathf.Min(speed * Time.deltaTime, toGoal.magnitude);
                    moving = true;
                }
            }
            else if (Time.time >= _idleUntil)
            {
                if (toGoal.sqrMagnitude < 0.02f)
                {
                    _idleUntil = Time.time + UnityEngine.Random.Range(1.5f, 4.5f);
                    PickTarget();
                }
                else
                {
                    transform.position = pos + toGoal.normalized * Mathf.Min(_speed * Time.deltaTime, toGoal.magnitude);
                    moving = true;
                }
            }

            if (moving && Mathf.Abs(toGoal.x) > 0.01f) _renderer.flipX = toGoal.x < 0f; // sprites face right
            _renderer.sortingOrder = VillageSorting.Order(VillageSorting.Feet(transform.position, _renderer.sprite));
            if (_bubble != null && _bubble.activeSelf && Time.time >= _bubbleHideAt) _bubble.SetActive(false);
        }

        private void PickTarget() =>
            _target = new Vector2(UnityEngine.Random.Range(_area.xMin, _area.xMax), UnityEngine.Random.Range(_area.yMin, _area.yMax));

        public void OnTapped()
        {
            Sfx.Play(AudioCueIds.UiClick);
            string line = _speech?.Invoke();
            if (!string.IsNullOrEmpty(line)) ShowBubble(line);
            _idleUntil = Time.time + 3f; // stop to talk
            _onTap?.Invoke();
        }

        private void ShowBubble(string text)
        {
            if (_bubble == null)
            {
                _bubble = new GameObject("Bubble");
                _bubble.transform.SetParent(transform, false);
                var background = GeneratedSprites.CreateRenderer(_bubble.transform, "Background", GeneratedSprites.Square,
                    new Color(1f, 0.97f, 0.9f, 0.95f), VillageSorting.Labels + 10, Vector2.zero, new Vector2(5.4f, 1.5f));
                background.name = "Background";
                _bubbleText = new GameObject("Text").AddComponent<TextMeshPro>();
                _bubbleText.transform.SetParent(_bubble.transform, false);
                _bubbleText.rectTransform.sizeDelta = new Vector2(5.1f, 1.4f);
                _bubbleText.fontSize = 2.1f;
                _bubbleText.color = new Color(0.16f, 0.1f, 0.07f, 1f);
                _bubbleText.alignment = TextAlignmentOptions.Center;
                _bubbleText.textWrappingMode = TextWrappingModes.Normal;
                _bubbleText.sortingOrder = VillageSorting.Labels + 11;
            }
            float top = _renderer.sprite != null ? _renderer.sprite.bounds.extents.y : 0.6f;
            _bubble.transform.localPosition = new Vector3(0f, top + 1.2f, 0f);
            _bubbleText.text = text;
            _bubble.SetActive(true);
            _bubbleHideAt = Time.time + 3.5f;
        }
    }
}
