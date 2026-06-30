using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Base cho panel dòng chuẩn (kế thừa UIViewBase). Quản lý layer, hành vi cache (chỉnh trong prefab),
    /// và disable/destroy khi hide. Subclass có animation override Hide để delay OnCompleteHide.
    /// </summary>
    public class UIPanelBase : UIViewBase, IUIPanel
    {
        /// <summary>
        /// True: cache để reuse | False: destroy sau khi hide complete
        /// </summary>
        [SerializeField] [GUIColor("cyan")]
        private bool _canCache = false;

        private PanelOptions _options;

        /// <summary>
        /// Phát khi panel hiện xong. Cho presenter/handler ngoài tự đăng ký lúc panel xuất hiện (vd MVP).
        /// </summary>
        public event Action OnShown;

        /// <summary>
        /// Phát khi panel ẩn xong. Manager dùng để dọn cache; presenter/handler ngoài dùng để hủy đăng ký.
        /// </summary>
        public event Action OnHidden;

        /// <summary>
        /// true thì disable (cache) sau khi hide; false thì destroy. Đặt trong prefab.
        /// </summary>
        public bool CanCache => _canCache;

        /// <summary>
        /// Layer Canvas mà panel nằm trong.
        /// </summary>
        public LayerType LayerType
        {
            get { return _options.Layer; }
        }

        /// <summary>
        /// Gọi một lần bởi UIPanelManager sau khi Instantiate, trước Show. Không gọi thủ công.
        /// </summary>
        public void Setup(PanelOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Hiển thị panel. Override để thêm animation; nhớ gọi OnBeginShow rồi OnCompleteShow.
        /// </summary>
        public virtual void Show()
        {
            OnBeginShow();
            OnCompleteShow();
        }

        /// <summary>
        /// Ẩn panel. Mặc định gọi OnBeginHide rồi OnCompleteHide ngay. Override để delay (vd chờ tween).
        /// </summary>
        public virtual void Hide()
        {
            OnBeginHide();
            OnCompleteHide();
        }

        /// <summary>
        /// Callback khi panel bắt đầu show. Override để thêm logic.
        /// </summary>
        protected virtual void OnBeginShow()
        {
        }

        /// <summary>
        /// Kết thúc show: phát OnShown.
        /// </summary>
        protected void OnCompleteShow()
        {
            OnShown?.Invoke();
        }

        /// <summary>
        /// Callback khi panel bắt đầu hide. Override để thêm logic.
        /// </summary>
        protected virtual void OnBeginHide()
        {
        }

        /// <summary>
        /// Kết thúc hide: disable nếu CanCache, ngược lại destroy; rồi phát OnHidden.
        /// </summary>
        protected void OnCompleteHide()
        {
            if (CanCache)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }

            OnHidden?.Invoke();
        }
    }
}