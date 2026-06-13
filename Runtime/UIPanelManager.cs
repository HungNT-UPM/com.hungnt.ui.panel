using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Quản lý lifecycle của các UI Panel: spawn từ Resources, phân layer, cache, show/hide.
    /// </summary>
    public class UIPanelManager : MonoSingletonScene<UIPanelManager>
    {
        [InlineButton(nameof(SetupCanvas), "Setup")]
        [SerializeField]
        private Canvas _canvasRoot;

        [ShowInInspector, ReadOnly, TableList]
        private Dictionary<LayerType, UILayer> _layers = new();

        [ShowInInspector, ReadOnly, TableList]
        private Dictionary<Type, UIPanelBase> _panels = new();

        protected override void OnAwake()
        {
            foreach (var layer in GetComponentsInChildren<UILayer>(true))
                _layers[layer.LayerType] = layer;
        }

        private void SetupCanvas()
        {
            if (_canvasRoot == null)
            {
                _canvasRoot = GetComponentInChildren<Canvas>();
            }

            if (_canvasRoot != null && _canvasRoot.worldCamera == null)
            {
                _canvasRoot.worldCamera = Camera.main;
            }
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

            var layer = GetOrCreateLayer(options.Layer);
            var prefab = Resources.Load<GameObject>(options.Path);
            var instance = Instantiate(prefab, layer.RectTransform);
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

        private UILayer GetOrCreateLayer(LayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out var layer) && layer != null)
                return layer;

            // create layer & set sibling by LayerType
            var go = new GameObject($"Layer {layerType}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex((int)layerType);

            // set full screen
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            layer = go.AddComponent<UILayer>();
            layer.Init(layerType);

            _layers[layerType] = layer;
            return layer;
        }
    }
}