using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using HungNT.UI.Tween;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Base panel cho view cần Odin serialize (vd dict lồng gán trong Inspector) — kế thừa
    /// SerializedMonoBehaviour thay vì UIViewBase. Gộp logic của UIPanelBase + UIPanelTween,
    /// phải đồng bộ thủ công với 2 file đó khi sửa vòng đời. Dùng cho Market/Auction/SecretManual.
    /// </summary>
    public class SerializedUIPanelTween : SerializedMonoBehaviour, IUIPanel
    {
        /// <summary>
        /// true: cache để reuse | false: destroy sau khi hide complete. Đặt trong prefab.
        /// </summary>
        [SerializeField] [GUIColor("cyan")]
        private bool _canCache = false;

        private PanelOptions _options;
        private TweenGroup _tweenGroup;

        /// <summary>
        /// Phát khi panel hiện xong. Cho presenter/handler ngoài tự đăng ký lúc panel xuất hiện.
        /// </summary>
        public event Action OnShown;

        /// <summary>
        /// Phát khi panel ẩn xong. Manager dùng để dọn cache; presenter/handler ngoài hủy đăng ký.
        /// </summary>
        public event Action OnHidden;

        /// <summary>
        /// true thì disable (cache) sau khi hide; false thì destroy. Đặt trong prefab.
        /// </summary>
        public bool CanCache => _canCache;

        /// <summary>
        /// Layer Canvas mà panel nằm trong.
        /// </summary>
        public LayerType LayerType => _options.Layer;

        /// <summary>
        /// TweenGroup trên cùng GameObject — lazy, tự add nếu chưa có.
        /// </summary>
        private TweenGroup TweenGroup
        {
            get
            {
                if (_tweenGroup == null && !TryGetComponent(out _tweenGroup))
                {
                    _tweenGroup = gameObject.AddComponent<TweenGroup>();
                }

                return _tweenGroup;
            }
        }

        /// <summary>
        /// true khi hide tween đang chạy — ngăn gọi lồng nhau.
        /// </summary>
        public bool IsHiding
        {
            get { return _tweenGroup != null && _tweenGroup.IsHiding; }
        }

        /// <summary>
        /// Gọi một lần bởi PanelManager sau khi Instantiate, trước Show. Không gọi thủ công.
        /// </summary>
        public void Setup(PanelOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Hiển thị panel. Show tween con tự play qua OnEnable khi panel active.
        /// </summary>
        public virtual void Show()
        {
            OnBeginShow();
            OnCompleteShow();
        }

        /// <summary>
        /// Ẩn panel: chờ hide tween xong rồi mới OnCompleteHide.
        /// </summary>
        public virtual void Hide()
        {
            OnBeginHide();
            HideTweenAsync(this.GetCancellationTokenOnDestroy()).Forget();
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

        private async UniTaskVoid HideTweenAsync(CancellationToken token)
        {
            if (IsHiding)
            {
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                OnCompleteHide();
                return;
            }

            await TweenGroup.PlayHideAsync(token);
            OnCompleteHide();
        }
    }
}