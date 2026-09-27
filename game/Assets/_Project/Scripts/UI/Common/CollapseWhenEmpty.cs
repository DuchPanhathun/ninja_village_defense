using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Takes no layout space while this object has no active children — e.g. a card's action row
    /// when the card has no buttons — and its normal height as soon as something is added.
    /// </summary>
    [RequireComponent(typeof(LayoutElement))]
    public class CollapseWhenEmpty : MonoBehaviour
    {
        [SerializeField] private float height = 96f;

        public void SetHeight(float value)
        {
            height = value;
            Apply();
        }

        private void Start() => Apply();
        private void OnTransformChildrenChanged() => Apply();

        private void Apply()
        {
            bool empty = true;
            foreach (Transform child in transform)
                if (child.gameObject.activeSelf) { empty = false; break; }
            var element = GetComponent<LayoutElement>();
            element.minHeight = empty ? 0f : height;
            element.preferredHeight = empty ? 0f : height;
        }
    }
}
