namespace HungNT.UI.Panel
{
    /// <summary>
    /// Layer hiển thị của panel trong Canvas.
    /// </summary>
    public enum LayerType
    {
        /// <summary>
        /// Layer thấp nhất trong UI nhưng cao hơn các layer khác ingame
        /// Thường dùng để hiển thị thông tin player như Health Bar, Bubble Chat, ...
        /// </summary>
        UnderUI = 0,

        /// <summary>
        /// Layer static, thường setup sẵn trên scene, không phải các popup sinh ra trong runtime
        /// Thường là UI Menu Home, UI Ingame, ...
        /// </summary>
        Static = 1,

        /// <summary>
        /// Layer gồm các popup sinh ra trong runtime
        /// Thường có nhiều thay đổi nên tách thành 1 Canvas để tránh việc canvas update
        /// </summary>
        Dynamic = 2,

        /// <summary>
        /// Layer cao hơn toàn bộ các UI khác với Canvas dạng Screen Space - Camera (vẫn thấp hơn các UI Loading dạng Screen Space - Overlay)
        /// Các thông báo, notification ingame, animation collect reward, ...
        /// </summary>
        OverUI = 3,
    }
}