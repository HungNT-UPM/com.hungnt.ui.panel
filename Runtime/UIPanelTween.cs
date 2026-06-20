using System.Threading;
using Cysharp.Threading.Tasks;
using HungNT.UI.Tween;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Panel có hide animation: ủy thác hide tween của các <see cref="UITweenBase"/> con
    /// cho <see cref="Tween.TweenGroup"/> trước khi disable/destroy. Kế thừa <see cref="UIPanelBase"/>.
    /// </summary>
    /// <remarks>
    /// Show: các <see cref="UITweenBase"/> con tự play qua <c>OnEnable</c> khi SetActive(true).
    /// Hide: override <see cref="UIPanelBase.Hide"/>, chờ tween xong rồi <c>HideComplete()</c>.
    /// </remarks>
    public class UIPanelTween : UIPanelBase
    {
        private TweenGroup _tweenGroup;

        /// <summary>
        /// <see cref="Tween.TweenGroup"/> điều phối hide tween con — lazy, tự add nếu chưa có.
        /// </summary>
        private TweenGroup TweenGroup
        {
            get
            {
                if (_tweenGroup == null && !TryGetComponent(out _tweenGroup))
                    _tweenGroup = gameObject.AddComponent<TweenGroup>();

                return _tweenGroup;
            }
        }

        /// <summary>
        /// <c>true</c> khi hide tween đang chạy — ngăn gọi lồng nhau.
        /// </summary>
        public bool IsHiding => _tweenGroup != null && _tweenGroup.IsHiding;

        /// <summary>
        /// Override: ủy thác hide tween cho <see cref="Tween.TweenGroup"/>,
        /// sau đó gọi <see cref="UIPanelBase.HideComplete"/>.
        /// </summary>
        public override void Hide()
        {
            OnHide();
            HideTweenAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid HideTweenAsync(CancellationToken token)
        {
            if (IsHiding)
                return;

            if (!gameObject.activeInHierarchy)
            {
                HideComplete();
                return;
            }

            await TweenGroup.PlayHideAsync(token);
            HideComplete();
        }
    }
}