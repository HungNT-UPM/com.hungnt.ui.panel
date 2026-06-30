using System.Threading;
using Cysharp.Threading.Tasks;
using HungNT.UI.Tween;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Panel có hide animation: chờ hide tween của các tween con (TweenGroup) chạy xong rồi mới
    /// disable/destroy. Show: các tween con tự play qua OnEnable khi panel active.
    /// </summary>
    public class UIPanelTween : UIPanelBase
    {
        private TweenGroup _tweenGroup;

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
        /// Override: chờ hide tween xong rồi mới OnCompleteHide.
        /// </summary>
        public override void Hide()
        {
            OnBeginHide();
            HideTweenAsync(this.GetCancellationTokenOnDestroy()).Forget();
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
