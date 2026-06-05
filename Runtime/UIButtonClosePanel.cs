using UnityEngine;
using UnityEngine.UI;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Nút đóng panel: tự tìm <see cref="UIPanelBase"/> cha gần nhất trong Awake,
    /// gọi <see cref="UIPanelManager.HidePanel(UIPanelBase)"/> khi click.
    /// </summary>
    /// <remarks>
    /// Không cần khai báo serialized field — gắn component này lên Button là dùng được.
    /// Hoạt động với mọi subclass của <see cref="UIPanelBase"/> (kể cả UIPanelTween).
    /// </remarks>
    [RequireComponent(typeof(Button))]
    public class UIButtonClosePanel : MonoBehaviour
    {
        private Button _button;
        private UIPanelBase _panel;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _panel = GetComponentInParent<UIPanelBase>();

            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (_panel != null)
                UIPanelManager.Instance.HidePanel(_panel);
        }
    }
}
