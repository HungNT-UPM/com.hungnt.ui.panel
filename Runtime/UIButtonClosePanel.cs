using UnityEngine;
using UnityEngine.UI;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Nút đóng panel: tìm UIPanelBase cha gần nhất trong Awake, gọi UIPanelManager.HidePanel khi click.
    /// Gắn lên Button là dùng được, không cần serialized field.
    /// </summary>
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
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (_panel != null)
            {
                PanelManager.Instance.HidePanel(_panel);
            }
        }
    }
}