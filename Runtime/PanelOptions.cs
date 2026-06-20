namespace HungNT.UI.Panel
{
    /// <summary>
    /// Tham số khởi tạo và hiển thị panel, truyền vào <see cref="UIPanelManager.ShowPanel{T}"/>.
    /// </summary>
    public struct PanelOptions
    {
        /// <summary>
        /// Đường dẫn Resources tới prefab panel (không có extension).
        /// Ví dụ: <c>"Panels/HomePanel"</c>.
        /// </summary>
        public string Path;

        /// <summary>Layer Canvas mà panel được sinh ra. Mặc định: <see cref="LayerType.Dynamic"/>.</summary>
        public LayerType Layer;

        /// <summary>
        /// Có cache panel sau khi Hide không.
        /// <c>true</c> → disable GameObject, reuse khi <see cref="UIPanelManager.ShowPanel{T}"/> gọi lại.<br/>
        /// <c>false</c> → Destroy, spawn mới từ Resources mỗi lần show.
        /// </summary>
        public bool CanCache;

        /// <summary>
        /// Khởi tạo với đường dẫn và giá trị mặc định: Layer = Dynamic, CanCache = true.
        /// </summary>
        /// <param name="path">Đường dẫn Resources tới prefab (ví dụ: <c>"Panels/HomePanel"</c>).</param>
        public PanelOptions(string path)
        {
            Path = path;
            Layer = LayerType.Dynamic;
            CanCache = true;
        }
    }
}