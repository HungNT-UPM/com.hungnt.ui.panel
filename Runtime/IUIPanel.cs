using System;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Contract vòng đời để UIPanelManager quản lý mọi panel, bất kể base
    /// (UIViewBase / SerializedMonoBehaviour / TTMonoBehaviour). Dùng interface vì C# đơn kế thừa.
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// true thì disable (cache) sau khi hide; false thì destroy. Đặt trong prefab.
        /// </summary>
        bool CanCache { get; }

        /// <summary>
        /// Layer Canvas mà panel nằm trong.
        /// </summary>
        LayerType LayerType { get; }

        /// <summary>
        /// Phát khi panel hiện xong. Cho presenter/handler ngoài tự đăng ký lúc panel xuất hiện.
        /// </summary>
        event Action OnShown;

        /// <summary>
        /// Phát khi panel ẩn xong. Manager dùng để dọn cache; presenter/handler ngoài hủy đăng ký.
        /// </summary>
        event Action OnHidden;

        /// <summary>
        /// Gọi một lần bởi UIPanelManager sau khi Instantiate, trước Show.
        /// </summary>
        void Setup(PanelOptions options);

        /// <summary>
        /// Hiển thị panel.
        /// </summary>
        void Show();

        /// <summary>
        /// Ẩn panel (có thể async nếu có tween).
        /// </summary>
        void Hide();
    }
}
