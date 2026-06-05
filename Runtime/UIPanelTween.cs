using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HungNT.UI.Tween;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Panel có hide animation: điều phối hide tween trên các <see cref="UITweenBase"/> con
    /// trước khi deactivate/destroy. Kế thừa <see cref="UIPanelBase"/> — dùng với <see cref="UIPanelManager"/>.
    /// </summary>
    /// <remarks>
    /// Show animation: các <see cref="UITweenBase"/> con tự play qua <c>OnEnable</c> khi panel SetActive(true).
    /// Hide animation: override <see cref="UIPanelBase.Hide"/> — chạy hide tween rồi gọi <c>HideComplete()</c>.
    /// </remarks>
    public class UIPanelTween : UIPanelBase
    {
        /// <summary>
        /// <c>true</c> khi hide tween đang chạy — ngăn gọi lồng nhau.
        /// </summary>
        public bool IsHideTweening { get; private set; }

        protected virtual void OnEnable()
        {
            IsHideTweening = false;
        }

        /// <summary>
        /// Override: chạy hide tween trên các <see cref="UITweenBase"/> con,
        /// sau đó gọi <see cref="UIPanelBase.HideComplete"/>.
        /// </summary>
        public override void Hide()
        {
            OnHide();
            HideTweenAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid HideTweenAsync(CancellationToken token)
        {
            if (IsHideTweening)
                return;

            if (!gameObject.activeInHierarchy)
            {
                HideComplete();
                return;
            }

            IsHideTweening = true;

            try
            {
                await HideChildTweensAsync(token);
                HideComplete();
            }
            finally
            {
                IsHideTweening = false;
            }
        }

        private async UniTask HideChildTweensAsync(CancellationToken token)
        {
            IReadOnlyList<UITweenBase> tweens = CollectActiveTweens();
            if (tweens.Count == 0)
                return;

            var hideTasks = new List<UniTask>();

            for (int i = 0; i < tweens.Count; i++)
            {
                UITweenBase tween = tweens[i];
                if (tween != null && tween.HasHideTween)
                    hideTasks.Add(tween.Hide(token));
            }

            if (hideTasks.Count > 0)
                await UniTask.WhenAll(hideTasks);
        }

        private IReadOnlyList<UITweenBase> CollectActiveTweens()
        {
            UITweenBase[] tweens = GetComponentsInChildren<UITweenBase>(includeInactive: false);
            var activeTweens = new List<UITweenBase>(tweens.Length);

            for (int i = 0; i < tweens.Length; i++)
            {
                UITweenBase tween = tweens[i];
                if (tween != null && tween.gameObject.activeInHierarchy && tween.enabled)
                    activeTweens.Add(tween);
            }

            return activeTweens;
        }
    }
}
