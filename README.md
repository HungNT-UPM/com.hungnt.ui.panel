# com.hungnt.ui.panel

Hệ thống quản lý UI Panel: spawn từ Resources, phân layer theo `UILayer`, cache, show/hide lifecycle.

---

## Setup

Kéo prefab **UIPanelManager** từ `Runtime/Prefabs/` vào scene.

```
UIPanelManager          ← MonoSingletonScene — tự tìm UILayer trong children khi Awake
└── Canvas (ScreenSpace - Camera)
    ├── Layer Under UI  ← UILayer [UnderUI]
    ├── Layer Static    ← UILayer [Static]
    ├── Layer Dynamic   ← UILayer [Dynamic]
    └── Layer Over UI   ← UILayer [OverUI]
```

Mỗi layer con là sub-Canvas với `Override Sorting = true` để tách batch riêng biệt.

> **Auto-create fallback**: nếu không tìm thấy `UILayer` cho một `LayerType`, `UIPanelManager` tự tạo GameObject với `UILayer` component và đặt `siblingIndex = (int)layerType`.

---

## UILayer

Component đại diện cho một layer trong Canvas. Set **Layer Type** trong Inspector.

- `UIPanelManager.OnAwake()` tự tìm tất cả `UILayer` trong children qua `GetComponentsInChildren<UILayer>`.
- Thứ tự sibling trong hierarchy = thứ tự render (thấp → dưới, cao → trên).

---

## UIPanelBase

Base class cho tất cả panels. Gắn trực tiếp lên prefab hoặc kế thừa khi cần logic riêng.

```csharp
using HungNT.UI.Panel;

public class HomePanel : UIPanelBase
{
    [SerializeField] private TMP_Text _titleText;

    protected override void OnShow()
    {
        _titleText.SetText("Chào mừng!");
    }

    protected override void OnHide()
    {
        // cleanup nếu cần
    }
}
```

### Hide lifecycle

```
Hide()
  ├── OnHide()         ← override để cleanup
  └── HideComplete()
        ├── SetActive(false)    [CanCache = true]
        └── Destroy(gameObject) [CanCache = false]
```

Subclass có animation override `Hide()` để delay `HideComplete()` cho đến khi tween xong — xem `UIPanelTween`.

---

## UIPanelManager

### ShowPanel

```csharp
// Default — layer Dynamic, CanCache = true
var panel = UIPanelManager.Instance.ShowPanel<HomePanel>(
    new PanelOptions(PanelPaths.HOME_PANEL));

// Custom layer
var popup = UIPanelManager.Instance.ShowPanel<SettingsPanel>(
    new PanelOptions(PanelPaths.SETTINGS_PANEL) { Layer = LayerType.OverUI });

// Không cache — spawn mới mỗi lần show
var panel = UIPanelManager.Instance.ShowPanel<HomePanel>(
    new PanelOptions(PanelPaths.HOME_PANEL) { CanCache = false });
```

Idempotent: nếu panel đang shown → trả về instance hiện tại; nếu cached (disabled) → re-enable và gọi `Show()`.

### HidePanel

```csharp
UIPanelManager.Instance.HidePanel<HomePanel>();   // theo kiểu
UIPanelManager.Instance.HidePanel(panel);          // theo instance
```

### GetPanel / IsShowing

```csharp
var home = UIPanelManager.Instance.GetPanel<HomePanel>();

if (UIPanelManager.Instance.IsShowing<HomePanel>())
    Debug.Log("Home đang hiện");
```

---

## PanelOptions

Struct với default: `Layer = Dynamic`, `CanCache = true`.

```csharp
new PanelOptions("Panels/HomePanel")
new PanelOptions("Panels/HomePanel") { Layer = LayerType.Static, CanCache = false }
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
// Assets/Game/Scripts/UI/PanelPaths.cs
public static class PanelPaths
{
    public const string HOME_PANEL     = "Panels/HomePanel";
    public const string INGAME_PANEL   = "Panels/IngamePanel";
    public const string GAMEOVER_PANEL = "Panels/GameoverPanel";
    public const string SETTINGS_PANEL = "Panels/SettingsPanel";
}
```

Đặt prefab tại `Assets/Resources/Panels/HomePanel.prefab`, v.v.

---

## UIButtonClosePanel

Nút đóng panel. Không cần serialized field — tự tìm `UIPanelBase` cha trong Awake.

```
CloseButton (GameObject)
├── Button (component)
└── UIButtonClosePanel (component)   ← thêm vào là dùng được
```

---

## Integration với UIPanelTween (com.hungnt.ui.tween)

Thêm package `com.hungnt.ui.tween`, dùng `UIPanelTween` thay `UIPanelBase` để có hide animation:

```csharp
// Gắn UIPanelTween lên prefab thay vì UIPanelBase.
// Thêm UITweenFade / UITweenScale / ... lên các child GameObject.
// Khi HidePanel được gọi → child tweens play animation → panel disable/destroy.
```

Show animation: `UITweenBase` con tự play qua `OnEnable` khi panel được SetActive(true).
