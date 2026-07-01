namespace HungNT.UI.Panel
{
    /// <summary>
    /// Tham số hiển thị panel: đường dẫn prefab + layer. Hành vi cache do panel tự quyết qua
    /// CanCache (serialize trong prefab), không nằm ở đây.
    /// </summary>
    public struct PanelOptions
    {
        /// <summary>
        /// Đường dẫn tới prefab panel (loader phân giải). Khai báo tập trung trong PanelPaths.
        /// </summary>
        public string Path;

        /// <summary>
        /// Layer Canvas mà panel được sinh ra. Mặc định Dynamic.
        /// </summary>
        public LayerType Layer;

        /// <summary>
        /// Khởi tạo với đường dẫn; Layer mặc định Dynamic.
        /// </summary>
        public PanelOptions(string path)
        {
            Path = path;
            Layer = LayerType.Dynamic;
        }

        public PanelOptions(string path, LayerType layer)
        {
            Path = path;
            Layer = layer;
        }
    }
}
