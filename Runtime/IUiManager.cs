using UnityEngine;
using Cysharp.Threading.Tasks;

namespace HungNT.UI.Panel
{
    public interface IUIManager
    {
        void SetLoader(IUIPrefabLoader loader);

        T ShowPanel<T>(PanelOptions options)
            where T : MonoBehaviour, IUIPanel;

        T ShowPanel<T, TData>(PanelOptions options, TData data)
            where T : MonoBehaviour, IUIPanel;

        UniTask<T> ShowPanelAsync<T>(PanelOptions options)
            where T : MonoBehaviour, IUIPanel;

        UniTask<T> ShowPanelAsync<T, TData>(PanelOptions options, TData data)
            where T : MonoBehaviour, IUIPanel;

        T SpawnUI<T>(PanelOptions options)
            where T : Component;

        void Attach(Component ui, LayerType layer = LayerType.Static);

        RectTransform GetLayerRoot(LayerType layer);

        void HidePanel<T>()
            where T : class, IUIPanel;

        void HidePanel(IUIPanel panel);

        T GetPanel<T>()
            where T : class, IUIPanel;

        bool IsShowing<T>()
            where T : class, IUIPanel;

        void ClearLayer(LayerType layerType);
    }
}