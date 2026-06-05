# com.hungnt.ui.panel

Hệ thống quản lý UI Panel: spawn từ Resources, phân layer, cache, show/hide lifecycle.

## Dependency

```
com.hungnt.ui.panel
├── com.hungnt.ui      (UIViewBase)
└── com.hungnt.core    (MonoSingleton)
```

Integration với tween animation: thêm `com.hungnt.ui.tween` → dùng `UIPanelTween` thay vì `UIPanelBase`.

---

## Canvas Setup

Tạo Canvas trong scene với cấu trúc sau:

```
Canvas (ScreenSpace - Camera)
└── UIPanelManager          ← gắn component UIPanelManager
    ├── LowLayer             ← RectTransform, assign vào _lowLayer
    ├── MidLayer             ← RectTransform, assign vào _midLayer
    └── TopLayer             ← RectTransform, assign vào _topLayer
```

Mỗi layer là một RectTransform với Anchors = stretch-stretch (full screen).

---

## PanelPaths

Khai báo đường dẫn prefab trong game project (không phải trong package):

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

Đặt prefab tương ứng tại `Assets/Resources/Panels/HomePanel.prefab`, v.v.

---

## UIPanelBase

Base class cho tất cả panels. Không abstract — dùng trực tiếp hoặc kế thừa.

```csharp
using HungNT.UI.Panel;

// Panel đơn giản — dùng trực tiếp UIPanelBase (không cần kế thừa)
// Gắn component UIPanelBase lên prefab là đủ.

// Panel có logic — kế thừa:
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
        ├── SetActive(false)    [nếu CanCache = true]
        └── Destroy(gameObject) [nếu CanCache = false]
```

Subclass có animation (xem `UIPanelTween`) override `Hide()` để delay `HideComplete()`.

---

## UIPanelManager

### ShowPanel

```csharp
// Đơn giản nhất — layer Mid, canCache = true
var panel = UIPanelManager.Instance.ShowPanel<HomePanel>(
    new PanelOptions(PanelPaths.HOME_PANEL));

// Custom layer (Top — popup)
var popup = UIPanelManager.Instance.ShowPanel<SettingsPanel>(
    new PanelOptions(PanelPaths.SETTINGS_PANEL) { Layer = PanelLayerType.Top });

// Không cache (spawn mới mỗi lần show)
var panel = UIPanelManager.Instance.ShowPanel<HomePanel>(
    new PanelOptions(PanelPaths.HOME_PANEL) { CanCache = false });
```

ShowPanel là idempotent — nếu panel đang shown, trả về instance hiện tại.
Nếu panel đã cached (disabled), re-enable và gọi Show().

### HidePanel

```csharp
// Từ bên ngoài — theo kiểu
UIPanelManager.Instance.HidePanel<HomePanel>();

// Từ bên ngoài — theo instance
UIPanelManager.Instance.HidePanel(panel);
```

### GetPanel / IsShowing

```csharp
// Lấy panel đang shown hoặc cached (null nếu chưa tồn tại)
var home = UIPanelManager.Instance.GetPanel<HomePanel>();

// Kiểm tra panel đang active
if (UIPanelManager.Instance.IsShowing<HomePanel>())
    Debug.Log("Home đang hiện");
```

---

## PanelOptions

Struct với constructor đặt default: Layer = Mid, CanCache = true.

```csharp
// Đơn giản:
new PanelOptions("Panels/HomePanel")

// Custom:
new PanelOptions("Panels/HomePanel") { Layer = PanelLayerType.Top, CanCache = false }
```

**Mở rộng về sau** — thêm field mới vào `PanelOptions` (với default trong constructor) sẽ không break code cũ.

---

## UIButtonClosePanel

Nút đóng panel. Không cần serialized field — tự tìm `UIPanelBase` cha trong `Awake`.

```
CloseButton (GameObject)
├── Button (component)
└── UIButtonClosePanel (component)   ← thêm vào là dùng được
```

Gọi `UIPanelManager.Instance.HidePanel(panel)` khi click. Hoạt động với mọi subclass `UIPanelBase`.

---

## Integration với UIPanelTween (com.hungnt.ui.tween)

Khi thêm package `com.hungnt.ui.tween`, dùng `UIPanelTween` thay vì `UIPanelBase` để có hide animation:

```csharp
// Gắn UIPanelTween thay vì UIPanelBase lên prefab.
// Thêm các UITweenFade / UITweenScale / ... lên các child GameObject.
// Khi HidePanel được gọi → child tweens play animation → sau đó panel disable/destroy.

var panel = UIPanelManager.Instance.ShowPanel<HomePanel>(
    new PanelOptions(PanelPaths.HOME_PANEL));
// HomePanel.cs : UIPanelTween  (không phải UIPanelBase)
```

Show animation: các `UITweenBase` con tự play qua `OnEnable` khi panel được SetActive(true).

---

## PanelLayerType

| Value | Mô tả |
|-------|-------|
| `Low` (0) | Layer thấp nhất — nền, ambient backgrounds |
| `Mid` (1) | Mặc định — màn hình chính (Home, InGame, Gameover) |
| `Top` (2) | Layer cao nhất — popup, loading screen, notification |
