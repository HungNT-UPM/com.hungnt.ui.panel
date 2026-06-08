using System;
using System.Collections.Generic;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Quản lý lifecycle của các UI Panel: spawn từ Resources, phân layer, cache, show/hide.
    /// </summary>
    /// <remarks>
    /// Gắn component này lên Canvas và assign 3 layer <see cref="RectTransform"/> trong Inspector:
    /// <code>
    /// Canvas
    /// └── UIPanelManager
    ///     ├── LowLayer   → _lowLayer
    ///     ├── MidLayer   → _midLayer
    ///     └── TopLayer   → _topLayer
    /// </code>
    /// Truy cập từ mọi nơi: <c>UIPanelManager.Instance.ShowPanel&lt;T&gt;(options)</c>.
    /// </remarks>
    /// <remarks>
    /// <b>Per-scene</b> (<see cref="MonoSingletonScene{T}"/>, KHÔNG DontDestroyOnLoad): layer là object
    /// trong scene, nên manager phải bị huỷ khi unload để mỗi scene tự bind lại layer của mình.
    /// </remarks>
    public class UIPanelManager : MonoSingletonScene<UIPanelManager>
    {
        [SerializeField] private RectTransform _lowLayer;
        [SerializeField] private RectTransform _midLayer;
        [SerializeField] private RectTransform _topLayer;

        private Dictionary<Type, UIPanelBase> _panels;

        protected override void OnAwake()
        {
            _panels = new Dictionary<Type, UIPanelBase>();
        }

        /// <summary>
        /// Hiển thị panel kiểu <typeparamref name="T"/>. Reuse instance từ cache nếu có;
        /// spawn mới từ Resources nếu chưa tồn tại.
        /// </summary>
        /// <param name="options">
        /// Config panel: đường dẫn Resources, layer, canCache.
        /// Dùng <c>new PanelOptions("Panels/HomePanel")</c> cho giá trị mặc định.
        /// </param>
        /// <returns>Instance của panel.</returns>
        public T ShowPanel<T>(PanelOptions options) where T : UIPanelBase
        {
            var type = typeof(T);

            if (_panels.TryGetValue(type, out var existing) && existing != null)
            {
                if (existing.gameObject.activeSelf)
                    return (T)existing;

                existing.gameObject.SetActive(true);
                existing.Show();
                return (T)existing;
            }

            var prefab = Resources.Load<GameObject>(options.Path);
            var instance = Instantiate(prefab, GetLayer(options.Layer));
            var panel = instance.GetComponent<T>();
            panel.Setup(options);
            panel.Show();

            _panels[type] = panel;
            return panel;
        }

        /// <summary>
        /// Ẩn panel kiểu <typeparamref name="T"/> (nếu đang shown hoặc cached).
        /// </summary>
        public void HidePanel<T>() where T : UIPanelBase
        {
            var type = typeof(T);
            if (_panels.TryGetValue(type, out var panel) && panel != null)
                panel.Hide();
        }

        /// <summary>
        /// Ẩn panel theo instance — dùng khi panel tự đóng (ví dụ: <see cref="UIButtonClosePanel"/>).
        /// </summary>
        public void HidePanel(UIPanelBase panel)
        {
            if (panel != null)
                panel.Hide();
        }

        /// <summary>
        /// Lấy panel kiểu <typeparamref name="T"/> đang shown hoặc cached.
        /// Trả về <c>null</c> nếu chưa tồn tại.
        /// </summary>
        public T GetPanel<T>() where T : UIPanelBase
        {
            var type = typeof(T);
            if (_panels.TryGetValue(type, out var panel) && panel != null)
                return (T)panel;

            return null;
        }

        /// <summary>
        /// Kiểm tra panel kiểu <typeparamref name="T"/> đang active (shown).
        /// </summary>
        public bool IsShowing<T>() where T : UIPanelBase
        {
            var type = typeof(T);
            return _panels.TryGetValue(type, out var panel)
                   && panel != null
                   && panel.gameObject.activeSelf;
        }

        private RectTransform GetLayer(PanelLayerType layerType) => layerType switch
        {
            PanelLayerType.Low => _lowLayer,
            PanelLayerType.Top => _topLayer,
            _ => _midLayer
        };
    }
}
