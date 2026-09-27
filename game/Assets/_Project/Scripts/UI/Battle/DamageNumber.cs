using TMPro;
using NinjaVillage.Core.Utilities;
using UnityEngine;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// A floating world-space damage number: rises, fades, self-destructs.
    /// Prefab: empty GameObject + TextMeshPro (world) + this component.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DamageNumber : MonoBehaviour, IPoolable
    {
        [SerializeField] private float riseSpeed = 1.5f;
        [SerializeField] private float lifetime = 0.7f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = new(1f, 0.55f, 0.1f);
        [SerializeField] private float criticalScale = 1.4f;

        private TMP_Text _text;
        private float _elapsed;

        private Vector3 _baseScale;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _baseScale = transform.localScale;
        }

        public void OnSpawned()
        {
            _elapsed = 0f;
            transform.localScale = _baseScale; // crits enlarge it; undo for reuse
        }

        public void OnDespawned() { }

        public void Show(float amount, bool isCritical)
        {
            _text.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
            _text.color = isCritical ? criticalColor : normalColor;
            if (isCritical)
                transform.localScale *= criticalScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            var color = _text.color;
            color.a = 1f - _elapsed / lifetime;
            _text.color = color;

            if (_elapsed >= lifetime)
                PrefabPool.Release(gameObject);
        }
    }
}
