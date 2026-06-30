# HungNT UI Panel

Hệ thống quản lý UI Panel: nạp prefab qua loader (Resources hoặc AssetBundle), phân layer theo `UILayer`, cache theo Type, show/hide lifecycle, inject data trước khi panel active.

Package: `com.hungnt.ui.panel` · Namespace: **`HungNT.UI.Panel`**.

---

## Cài đặt

`Packages/manifest.json`:

```json
"com.hungnt.ui.panel": "https://github.com/HungNT-UPM/com.hungnt.ui.panel.git#1.3.0"
```

### Mới ở 1.3.0
- **Pluggable loader** `IUIPrefabLoader` + default `ResourcesUIPrefabLoader`; inject qua `PanelManager.SetLoader(...)` (cho phép nạp từ AssetBundle thay Resources).
- **Inject data**: `ShowPanel<T,TData>` + `IPanelData<TData>` — `SetData` chạy ở trạng thái inactive, TRƯỚC Awake/OnEnable (instantiate-inactive → SetData → activate → Show).
- **Async**: `ShowPanelAsync<T>` / `ShowPanelAsync<T,TData>`.
- **Sự kiện vòng đời** trên panel: `IUIPanel.OnShown` / `OnHidden` — presenter/handler ngoài tự đăng ký lúc panel xuất hiện.
- **Contract interface** `IUIPanel` — Manager quản lý được panel ở mọi dòng base (`UIViewBase` / `SerializedMonoBehaviour` / `TTMonoBehaviour`); cache là `Dictionary<Type, IUIPanel>`, `ShowPanel<T>() where T : MonoBehaviour, IUIPanel`. Panel không cache tự evict khỏi cache khi destroy.

### Yêu cầu
- Unity 2022.3+
- [`com.hungnt.core`](https://github.com/HungNT-UPM/com.hungnt.core) ≥ 1.1.2 — `MonoSingletonScene`
- [`com.hungnt.ui`](https://github.com/HungNT-UPM/com.hungnt.ui) ≥ 1.0.2 — `UIViewBase`
- [`com.hungnt.ui.tween`](https://github.com/HungNT-UPM/com.hungnt.ui.tween) ≥ 2.3.0 — chỉ cần cho `UIPanelTween`
- Odin Inspector

---

## Setup

Kéo prefab **PanelManager** từ `Runtime/Prefabs/` vào scene.

```
PanelManager            ← MonoSingletonScene
└── Canvas (ScreenSpace - Camera)
    ├── Layer UnderUI   ← UILayer [UnderUI]
    ├── Layer Static    ← UILayer [Static]
    ├── Layer Dynamic   ← UILayer [Dynamic]
    └── Layer OverUI    ← UILayer [OverUI]
```

Mỗi layer con là sub-Canvas (`overrideSorting = true`) để tách batch riêng biệt. `PanelManager.OnAwake()` tự gom mọi `UILayer` trong children qua `GetComponentsInChildren<UILayer>(true)` (gồm cả inactive).

> Nút **Setup** cạnh field `Canvas Root` trong Inspector tự gán `Canvas` con + `worldCamera = Camera.main`.

> **Auto-create fallback**: nếu không tìm thấy `UILayer` cho một `LayerType`, `PanelManager` tự tạo GameObject + `UILayer` component, đặt `siblingIndex = (int)layerType` và `sortingOrder = (int)layerType`.

---

## UILayer

Component đại diện cho một layer trong Canvas (`[RequireComponent(Canvas, GraphicRaycaster)]`). Set **Layer Type** trong Inspector.

- `PanelManager` tự tìm tất cả `UILayer` trong children khi `OnAwake`.
- `sortingOrder` của Canvas = `(int)LayerType` → thứ tự render (thấp → dưới, cao → trên).

---

## UIPanelBase

Base class cho panel dòng chuẩn (kế thừa `UIViewBase`, implement `IUIPanel`). Gắn trực tiếp lên prefab hoặc kế thừa khi cần logic riêng.

```csharp
using HungNT.UI.Panel;

public class HomePanel : UIPanelBase
{
    [SerializeField] private TMP_Text _titleText;

    protected override void OnBeginShow()
    {
        _titleText.SetText("Chào mừng!");
    }

    protected override void OnBeginHide()
    {
        // cleanup nếu cần
    }
}
```

### Show / Hide lifecycle

```
Show()
  ├── OnBeginShow()        ← override để init khi hiện
  └── OnCompleteShow()
        └── OnShown?.Invoke()

Hide()
  ├── OnBeginHide()        ← override để cleanup khi ẩn
  └── OnCompleteHide()
        ├── SetActive(false)    [CanCache = true]
        ├── Destroy(gameObject) [CanCache = false]
        └── OnHidden?.Invoke()
```

`CanCache` đặt trong prefab (field `_canCache`). Subclass có animation override `Hide()` để delay `OnCompleteHide()` cho tới khi tween xong — xem `UIPanelTween`.

---

## PanelManager

### ShowPanel

```csharp
// Default — layer Dynamic
var panel = PanelManager.Instance.ShowPanel<HomePanel>(new PanelOptions(PanelPaths.HOME_PANEL));

// Custom layer
var popup = PanelManager.Instance.ShowPanel<SettingsPanel>(
    new PanelOptions(PanelPaths.SETTINGS_PANEL) { Layer = LayerType.OverUI });
```

Hành vi cache (ẩn-rồi-giữ hay destroy) đặt ở field `CanCache` trong **prefab** của panel, không truyền qua code.

Idempotent: nếu panel đang shown → trả về instance hiện tại; nếu cached (disabled) → re-enable và gọi `Show()`.

### ShowPanel kèm data

```csharp
// SetData chạy ở trạng thái inactive, TRƯỚC Awake/OnEnable
var panel = PanelManager.Instance.ShowPanel<ShopPanel, ShopData>(
    new PanelOptions(PanelPaths.SHOP_PANEL), shopData);
```

Panel cần implement `IPanelData<TData>`:

```csharp
public class ShopPanel : UIPanelBase, IPanelData<ShopData>
{
    private ShopData _data;
    public void SetData(ShopData data) { _data = data; }   // chạy trước khi GameObject active
}
```

### Async

```csharp
var panel = await PanelManager.Instance.ShowPanelAsync<HomePanel>(new PanelOptions(PanelPaths.HOME_PANEL));
var shop  = await PanelManager.Instance.ShowPanelAsync<ShopPanel, ShopData>(
    new PanelOptions(PanelPaths.SHOP_PANEL), shopData);
```

### HidePanel

```csharp
PanelManager.Instance.HidePanel<HomePanel>();   // theo kiểu
PanelManager.Instance.HidePanel(panel);          // theo instance (IUIPanel)
```

### GetPanel / IsShowing

```csharp
var home = PanelManager.Instance.GetPanel<HomePanel>();

if (PanelManager.Instance.IsShowing<HomePanel>())
    home.Log("Home đang hiện");
```

### SetLoader (inject loader tùy dự án)

```csharp
// Gọi lúc boot, trước panel đầu tiên — vd nạp từ AssetBundle thay Resources.
PanelManager.Instance.SetLoader(new BundleUIPrefabLoader());
```

---

## PanelOptions

Struct gồm `Path` (đường dẫn prefab loader phân giải) + `Layer` (mặc định `Dynamic`). Cache do panel tự quyết qua `CanCache` (serialize trong prefab), không nằm ở đây.

```csharp
new PanelOptions(PanelPaths.HOME_PANEL);                                          // Layer Dynamic (mặc định)
new PanelOptions(PanelPaths.HOME_PANEL) { Layer = LayerType.Static };
```

---

## LayerType

| Value | Tên | Mô tả |
|-------|-----|-------|
| `0` | `UnderUI` | Dưới tất cả UI — HUD, health bars, bubble chat |
| `1` | `Static`  | UI cố định trong scene — Home screen, InGame HUD |
| `2` | `Dynamic` | Popup sinh ra trong runtime (mặc định) |
| `3` | `OverUI`  | Trên tất cả — notification, collect-reward animation |

---

## PanelPaths (game project)

Khai báo đường dẫn prefab trong game project, không phải trong package:

```csharp
// Assets/.../UI/Panel/PanelPaths.cs
public static class PanelPaths
{
    public const string HOME_PANEL     = "Panels/HomePanel";
    public const string SETTINGS_PANEL = "Panels/SettingsPanel";
    public const string SHOP_PANEL     = "Panels/ShopPanel";
}
```

Đặt prefab tại `Assets/Resources/Panels/HomePanel.prefab` (default loader), hoặc trong bundle khi dùng loader tùy chỉnh.

---

## IUIPrefabLoader

Abstraction nạp prefab để `PanelManager` portable giữa Resources và AssetBundle. Trả về prefab (chưa Instantiate) để manager kiểm soát instantiate-inactive.

```csharp
public interface IUIPrefabLoader
{
    GameObject Load(string path);                 // đồng bộ, null nếu không có
    UniTask<GameObject> LoadAsync(string path);   // bất đồng bộ
}
```

Mặc định `ResourcesUIPrefabLoader`. Inject loader khác qua `PanelManager.SetLoader(...)`.

---

## UIButtonClosePanel

Nút đóng panel. Không cần serialized field — tự tìm `UIPanelBase` cha trong Awake, gọi `PanelManager.HidePanel` khi click.

```
CloseButton (GameObject)
├── Button (component)
└── UIButtonClosePanel (component)   ← thêm vào là dùng được
```

---

## Integration với UIPanelTween (com.hungnt.ui.tween)

Dùng `UIPanelTween` thay `UIPanelBase` để có hide animation. `UIPanelTween` ủy thác việc chạy hide tween cho `TweenGroup` (lazy get/add khi cần — không phải gắn sẵn):

```csharp
// Gắn UIPanelTween lên prefab thay vì UIPanelBase — TweenGroup tự thêm khi hide lần đầu.
// Thêm UITweenFade / UITweenScale / ... lên các child GameObject.
// HidePanel → TweenGroup.PlayHideAsync chờ child tweens xong → panel disable/destroy.
```

- **Show**: `UITweenBase` con tự play qua `OnEnable` khi panel được `SetActive(true)`.
- **Hide**: `Hide()` override để chờ `TweenGroup` hide xong rồi mới `OnCompleteHide()`. `IsHiding = true` trong lúc tween chạy.
