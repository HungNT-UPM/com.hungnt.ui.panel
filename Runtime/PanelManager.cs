using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Quản lý lifecycle UI Panel: nạp prefab qua IUIPrefabLoader (default Resources, có thể inject
    /// bundle loader), phân layer, cache theo Type, show/hide và inject data trước khi panel active.
    /// </summary>
    public class PanelManager : MonoSingletonScene<PanelManager>
    {
        [SerializeField] [InlineButton(nameof(SetupCanvas), "Setup")]
        private Canvas _canvasRoot;

        [ShowInInspector] [ReadOnly] [TableList]
        private Dictionary<LayerType, UILayer> _layers = new();

        [ShowInInspector] [ReadOnly]
        private Dictionary<Type, IUIPanel> _panels = new();

        private IUIPrefabLoader _loader;
        private RectTransform _staging;

        /// <summary>
        /// Loader nạp prefab; mặc định ResourcesUIPrefabLoader nếu chưa inject.
        /// </summary>
        private IUIPrefabLoader Loader
        {
            get
            {
                if (_loader == null)
                {
                    _loader = new ResourcesUIPrefabLoader();
                }

                return _loader;
            }
        }

        /// <summary>
        /// Parent ẩn (inactive) để Instantiate panel mà không chạy Awake/OnEnable trước khi SetData —
        /// đảm bảo data có sẵn trước khi panel active. Lazy tạo.
        /// </summary>
        private RectTransform Staging
        {
            get
            {
                if (_staging == null)
                {
                    var go = new GameObject("_staging", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    go.SetActive(false);
                    _staging = (RectTransform)go.transform;
                }

                return _staging;
            }
        }

        protected override void OnAwake()
        {
            foreach (var layer in GetComponentsInChildren<UILayer>(true))
            {
                _layers[layer.LayerType] = layer;
            }
        }

        /// <summary>
        /// Inject loader (vd BundleUIPrefabLoader của dự án). Gọi lúc boot, trước panel đầu tiên.
        /// </summary>
        public void SetLoader(IUIPrefabLoader loader)
        {
            _loader = loader;
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

        // ── Show ────────────────────────────────────────────────

        /// <summary>
        /// Hiển thị panel kiểu T. Reuse instance cache nếu có; spawn mới qua loader nếu chưa.
        /// </summary>
        public T ShowPanel<T>(PanelOptions options) where T : MonoBehaviour, IUIPanel
        {
            if (TryReuse(out T reused))
            {
                return reused;
            }

            return Spawn<T>(Loader.Load(options.Path), options, null);
        }

        /// <summary>
        /// Hiển thị panel kèm dữ liệu khởi tạo: SetData được gọi trước khi panel active
        /// (instantiate-inactive, SetData, active, Show).
        /// </summary>
        public T ShowPanel<T, TData>(PanelOptions options, TData data) where T : MonoBehaviour, IUIPanel
        {
            if (TryReuse(out T reused))
            {
                if (reused is IPanelData<TData> pd)
                {
                    pd.SetData(data);
                }

                return reused;
            }

            return Spawn<T>(Loader.Load(options.Path), options, panel =>
                {
                    if (panel is IPanelData<TData> pd)
                    {
                        pd.SetData(data);
                    }
                }
            );
        }

        /// <summary>
        /// Bản async của ShowPanel — nạp prefab qua IUIPrefabLoader.LoadAsync.
        /// </summary>
        public async UniTask<T> ShowPanelAsync<T>(PanelOptions options) where T : MonoBehaviour, IUIPanel
        {
            if (TryReuse(out T reused))
            {
                return reused;
            }

            var prefab = await Loader.LoadAsync(options.Path);
            return Spawn<T>(prefab, options, null);
        }

        /// <summary>
        /// Bản async của ShowPanel kèm dữ liệu khởi tạo.
        /// </summary>
        public async UniTask<T> ShowPanelAsync<T, TData>(PanelOptions options, TData data) where T : MonoBehaviour, IUIPanel
        {
            if (TryReuse(out T reused))
            {
                if (reused is IPanelData<TData> pd)
                {
                    pd.SetData(data);
                }

                return reused;
            }

            var prefab = await Loader.LoadAsync(options.Path);
            return Spawn<T>(prefab, options, panel =>
                {
                    if (panel is IPanelData<TData> pd)
                    {
                        pd.SetData(data);
                    }
                }
            );
        }

        private bool TryReuse<T>(out T panel) where T : MonoBehaviour, IUIPanel
        {
            panel = null;

            if (_panels.TryGetValue(typeof(T), out var existing) && Alive(existing))
            {
                var go = ((Component)existing).gameObject;
                if (!go.activeSelf)
                {
                    go.SetActive(true);
                    existing.Show();
                }

                panel = (T)existing;
                return true;
            }

            return false;
        }

        private T Spawn<T>(GameObject prefab, PanelOptions options, Action<T> setData) where T : MonoBehaviour, IUIPanel
        {
            if (prefab == null)
            {
                this.LogError($"Loader trả null cho path '{options.Path}'.");
                return null;
            }

            // Instantiate dưới Staging (inactive) để hoãn Awake/OnEnable cho tới khi SetData xong.
            var go = Instantiate(prefab, Staging);
            var panel = go.GetComponent<T>();
            if (panel == null)
            {
                this.LogError($"Prefab '{options.Path}' thiếu component {typeof(T).Name}.");
                Destroy(go);
                return null;
            }

            panel.Setup(options);
            if (setData != null)
            {
                setData.Invoke(panel);
            }

            var layer = GetOrCreateLayer(options.Layer);
            go.transform.SetParent(layer.RectTransform, false);
            go.SetActive(true); // active sau khi data đã set, để Awake/OnEnable đọc đúng dữ liệu

            var type = typeof(T);
            _panels[type] = panel;
            panel.OnHidden += () => { HandleHidden(type, panel); };

            panel.Show();
            return panel;
        }

        // ── Hide / Query ────────────────────────────────────────

        /// <summary>
        /// Ẩn panel kiểu T (nếu đang shown hoặc cached).
        /// </summary>
        public void HidePanel<T>() where T : class, IUIPanel
        {
            if (_panels.TryGetValue(typeof(T), out var panel) && Alive(panel))
            {
                panel.Hide();
            }
        }

        /// <summary>
        /// Ẩn panel theo instance — dùng khi panel tự đóng (vd UIButtonClosePanel).
        /// </summary>
        public void HidePanel(IUIPanel panel)
        {
            if (Alive(panel))
            {
                panel.Hide();
            }
        }

        /// <summary>
        /// Lấy panel kiểu T đang shown/cached; null nếu chưa có.
        /// </summary>
        public T GetPanel<T>() where T : class, IUIPanel
        {
            if (_panels.TryGetValue(typeof(T), out var panel) && Alive(panel))
            {
                return (T)panel;
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra panel kiểu T đang active (shown).
        /// </summary>
        public bool IsShowing<T>() where T : class, IUIPanel
        {
            if (_panels.TryGetValue(typeof(T), out var panel) && Alive(panel))
            {
                return ((Component)panel).gameObject.activeSelf;
            }

            return false;
        }

        private void HandleHidden(Type type, IUIPanel panel)
        {
            // Panel không cache đã bị Destroy ở OnCompleteHide, bỏ khỏi cache để GetPanel không trả fake-null.
            if (!panel.CanCache && _panels.TryGetValue(type, out var current) && ReferenceEquals(current, panel))
            {
                _panels.Remove(type);
            }
        }

        // Unity fake-null: panel đã Destroy vẫn khác C# null, dùng Component '==' override để check thật.
        private static bool Alive(IUIPanel panel)
        {
            return panel is Component component && component != null;
        }

        private UILayer GetOrCreateLayer(LayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out var layer) && layer != null)
            {
                return layer;
            }

            var go = new GameObject($"Layer {layerType}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex((int)layerType);

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