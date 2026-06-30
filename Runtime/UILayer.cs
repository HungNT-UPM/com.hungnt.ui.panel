using UnityEngine;
using UnityEngine.UI;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Đại diện một layer trong Canvas. UIPanelManager tự tìm hoặc tạo khi cần.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public class UILayer : UIViewBase
    {
        [SerializeField]
        private LayerType _layerType;

        public LayerType LayerType
        {
            get { return _layerType; }
        }

        private void Reset()
        {
            gameObject.name = $"Layer {_layerType}";
        }

        private void Awake()
        {
            gameObject.name = $"Layer {_layerType}";
        }

        internal void Init(LayerType layerType)
        {
            _layerType = layerType;

            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)layerType;
        }
    }
}