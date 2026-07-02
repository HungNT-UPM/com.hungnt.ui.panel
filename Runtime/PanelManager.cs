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
            if (TryGetCached(out T cached))
            {
                Reactivate(cached);
                return cached;
            }

            return Spawn<T>(Loader.Load(options.Path), options, null);
        }

        /// <summary>
        /// Hiển thị panel kèm dữ liệu khởi tạo: SetData được gọi trước khi panel active
        /// (instantiate-inactive, SetData, active, Show) — kể cả nhánh reuse instance cache.
        /// </summary>
        public T ShowPanel<T, TData>(PanelOptions options, TData data) where T : MonoBehaviour, IUIPanel
        {
            if (TryGetCached(out T cached))
            {
                // SetData TRƯỚC khi activate để OnEnable của lần mở lại đọc đúng dữ liệu mới
                // (cùng contract với nhánh spawn instantiate-inactive).
                if (cached is IPanelData<TData> pd)
                {
                    pd.SetData(data);
                }

                Reactivate(cached);
                return cached;
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
            if (TryGetCached(out T cached))
            {
                Reactivate(cached);
                return cached;
            }

            var prefab = await Loader.LoadAsync(options.Path);
            return Spawn<T>(prefab, options, null);
        }

        /// <summary>
        /// Bản async của ShowPanel kèm dữ liệu khởi tạo.
        /// </summary>
        public async UniTask<T> ShowPanelAsync<T, TData>(PanelOptions options, TData data) where T : MonoBehaviour, IUIPanel
        {
            if (TryGetCached(out T cached))
            {
                if (cached is IPanelData<TData> pd)
                {
                    pd.SetData(data);
                }

                Reactivate(cached);
                return cached;
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

        /// <summary>
        /// Lấy panel cache còn dùng được, KHÔNG activate (để caller SetData trước khi active).
        /// Panel đang chạy hide-tween dở bị hủy hẳn và báo không có cache — reuse instance sắp bị
        /// disable/destroy sẽ làm panel "vừa mở" biến mất khi tween kết thúc.
        /// </summary>
        private bool TryGetCached<T>(out T panel) where T : MonoBehaviour, IUIPanel
        {
            panel = null;

            if (!_panels.TryGetValue(typeof(T), out var existing) || !Alive(existing))
            {
                return false;
            }

            var go = ((Component)existing).gameObject;
            if (existing.IsHidden && go.activeSelf)
            {
                // Đã Hide nhưng GO còn active = hide-tween đang chạy. Destroy cancel luôn tween
                // (token GetCancellationTokenOnDestroy) nên OnCompleteHide không chạy nữa;
                // entry gỡ tay ở đây vì OnHidden sẽ không fire.
                _panels.Remove(typeof(T));
                Destroy(go);
                return false;
            }

            panel = (T)existing;
            return true;
        }

        /// <summary>
        /// Bring-to-front + kích hoạt lại panel cache nếu đang ẩn. Tách khỏi TryGetCached để
        /// SetData kịp chạy trước khi panel active (contract instantiate-inactive).
        /// </summary>
        private void Reactivate(IUIPanel panel)
        {
            var go = ((Component)panel).gameObject;
            // Đưa panel lên trên cùng trong layer khi show lại — giữ ngữ nghĩa "mở sau nằm trên"
            // (tương đương AddUIAtLast của hệ cũ).
            go.transform.SetAsLastSibling();
            if (!go.activeSelf)
            {
                go.SetActive(true);
                panel.Show();
            }
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

        // ── Spawn không quản lý (tooltip / dialog xếp chồng / multi-instance) ──

        /// <summary>
        /// Sinh một UI KHÔNG quản lý lifecycle: nạp prefab qua loader, Instantiate thẳng dưới layer
        /// chỉ định rồi trả component. Không cache theo Type, không Setup/Show/Hide — caller tự Destroy.
        /// Dùng cho tooltip, dialog xếp chồng (nhiều instance cùng lúc), UI vòng đời đặc thù.
        /// </summary>
        public T SpawnUI<T>(PanelOptions options) where T : Component
        {
            var prefab = Loader.Load(options.Path);
            if (prefab == null)
            {
                this.LogError($"Loader trả null cho path '{options.Path}'.");
                return null;
            }

            var layer = GetOrCreateLayer(options.Layer);
            var go = Instantiate(prefab, layer.RectTransform);
            go.transform.SetAsLastSibling();

            var ui = go.GetComponent<T>();
            if (ui == null)
            {
                this.LogError($"Prefab '{options.Path}' thiếu component {typeof(T).Name}.");
                Destroy(go);
                return null;
            }

            return ui;
        }

        // ── Attach (HUD tĩnh, không lifecycle) ──────────────────

        /// <summary>
        /// Gắn 1 UI thường (không phải IUIPanel) làm con cố định của một layer — dùng cho HUD nền
        /// game luôn hiển thị (top bar, joystick, radar...) trước đây nằm trong Main UI Root cũ.
        /// Không cache/show/hide theo Type; caller tự quản lý vòng đời như trước.
        /// </summary>
        public void Attach(Component ui, LayerType layer = LayerType.Static)
        {
            if (ui == null)
            {
                this.LogWarning("Attach nhận ui null.");
                return;
            }

            var target = GetOrCreateLayer(layer);
            ui.transform.SetParent(target.RectTransform, false);
        }

        /// <summary>
        /// RectTransform gốc của một layer (tạo nếu chưa có) — cho code legacy cần reparent/duyệt con.
        /// </summary>
        public RectTransform GetLayerRoot(LayerType layer)
        {
            return GetOrCreateLayer(layer).RectTransform;
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

        /// <summary>
        /// Hủy toàn bộ UI con của một layer (panel lẫn UI spawn không quản lý) và dọn các entry cache
        /// thuộc layer đó. Dùng khi đổi scene / relogin để reset UI runtime về trạng thái sạch.
        /// </summary>
        public void ClearLayer(LayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out var layer) && layer != null)
            {
                foreach (Transform child in layer.RectTransform)
                {
                    Destroy(child.gameObject);
                }
            }

            // Destroy chỉ có hiệu lực cuối frame → prune cache theo LayerType đã lưu trong panel,
            // không dựa vào check alive (panel vừa Destroy vẫn khác null trong frame này).
            var dead = new List<Type>();
            foreach (var entry in _panels)
            {
                if (!Alive(entry.Value) || entry.Value.LayerType == layerType)
                {
                    dead.Add(entry.Key);
                }
            }

            foreach (var type in dead)
            {
                _panels.Remove(type);
            }
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