using System.Threading;
using Cysharp.Threading.Tasks;
using HungNT.UI.Tween;
using UnityEngine;

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
    [RequireComponent(typeof(TweenGroup))]
    public class UIPanelTween : UIPanelBase
    {
        /// <summary>
        /// <see cref="Tween.TweenGroup"/> điều phối hide tween con (lazy, tự lấy nếu thiếu).
        /// </summary>
        private TweenGroup _tweenGroup;

        /// <summary>
        /// <c>true</c> khi hide tween đang chạy — ngăn gọi lồng nhau.
        /// </summary>
        public bool IsHiding => _tweenGroup.IsHiding;

        protected virtual void Awake()
        {
            _tweenGroup = GetComponent<TweenGroup>();
        }

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

            await _tweenGroup.PlayHideAsync(token);
            HideComplete();
        }
    }
}