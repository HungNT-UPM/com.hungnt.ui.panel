namespace HungNT.UI.Panel
{
    /// <summary>
    /// Layer hiển thị của panel trong Canvas.
    /// Thứ tự thấp → cao: Low → Mid → Top.
    /// </summary>
    public enum PanelLayerType
    {
        /// <summary>Layer thấp nhất — nền, ambient panels.</summary>
        Low = 0,

        /// <summary>Layer mặc định — màn hình chính (Home, InGame, Gameover).</summary>
        Mid = 1,

        /// <summary>Layer cao nhất — popup, loading, notification.</summary>
        Top = 2
    }
}
