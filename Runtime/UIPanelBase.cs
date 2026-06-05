using HungNT.UI;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Base class cho mọi UI Panel (Home, InGame, Gameover, v.v.).
    /// Kế thừa <see cref="UIViewBase"/> — có sẵn RectTransform, CanvasGroup, Interactable.
    /// </summary>
    /// <remarks>
    /// Không abstract — dùng trực tiếp được cho panel đơn giản, hoặc kế thừa để custom.
    /// <para>
    /// Hide lifecycle:
    /// <c>Hide()</c> → <c>OnHide()</c> → <c>HideComplete()</c> (disable hoặc destroy).<br/>
    /// Subclass có animation có thể override <c>Hide()</c> để delay <c>HideComplete()</c>
    /// cho đến khi animation xong.
    /// </para>
    /// </remarks>
    public class UIPanelBase : UIViewBase
    {
        private bool _canCache;
        private PanelLayerType _layerType;

        /// <summary>
        /// <c>true</c> nếu panel được disable (cache) sau Hide thay vì destroy.
        /// </summary>
        public bool CanCache => _canCache;

        /// <summary>Layer mà panel đang nằm trong Canvas.</summary>
        public PanelLayerType LayerType => _layerType;

        /// <summary>
        /// Gọi một lần bởi <see cref="UIPanelManager"/> sau khi Instantiate.
        /// Không gọi thủ công.
        /// </summary>
        internal void Setup(PanelOptions options)
        {
            _canCache = options.CanCache;
            _layerType = options.Layer;
        }

        /// <summary>
        /// Hiển thị panel. Override để thêm animation; nhớ gọi <see cref="OnShow"/> trong override.
        /// </summary>
        public virtual void Show()
        {
            OnShow();
        }

        /// <summary>
        /// Ẩn panel. Default: gọi <see cref="OnHide"/> rồi <see cref="HideComplete"/> ngay lập tức.<br/>
        /// Override để delay <see cref="HideComplete"/> — ví dụ: chờ tween animation xong.
        /// </summary>
        public virtual void Hide()
        {
            OnHide();
            HideComplete();
        }

        /// <summary>
        /// Kết thúc quá trình hide: disable (CanCache) hoặc destroy (!CanCache).<br/>
        /// Gọi sau khi animation kết thúc — hoặc ngay lập tức nếu không có animation.
        /// </summary>
        protected void HideComplete()
        {
            if (_canCache)
                gameObject.SetActive(false);
            else
                Destroy(gameObject);
        }

        /// <summary>Callback khi panel bắt đầu show. Override để thêm logic tùy chỉnh.</summary>
        protected virtual void OnShow() { }

        /// <summary>Callback khi panel bắt đầu hide. Override để thêm logic tùy chỉnh.</summary>
        protected virtual void OnHide() { }
    }
}
