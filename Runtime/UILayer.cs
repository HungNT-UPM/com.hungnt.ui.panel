using UnityEngine;
using UnityEngine.UI;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Đại diện một layer trong Canvas. Được <see cref="UIPanelManager"/> tự động tìm hoặc tạo khi cần.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public class UILayer : UIViewBase
    {
        [SerializeField]
        private LayerType _layerType;

        public LayerType LayerType => _layerType;

        private void OnValidate()
        {
            gameObject.name = $"Layer {_layerType}";
        }

        internal void Init(LayerType layerType)
        {
            _layerType = layerType;
            
            // override sorting order
            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)layerType;
        }
    }
}