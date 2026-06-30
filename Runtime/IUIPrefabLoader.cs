using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Abstraction nạp prefab panel — cho phép UIPanelManager portable: dự án dùng Resources hay
    /// AssetBundle đều inject loader phù hợp qua UIPanelManager.SetLoader. Trả về prefab (chưa Instantiate)
    /// để manager kiểm soát instantiate-inactive.
    /// </summary>
    public interface IUIPrefabLoader
    {
        /// <summary>
        /// Nạp đồng bộ prefab theo path; trả null nếu không có.
        /// </summary>
        GameObject Load(string path);

        /// <summary>
        /// Nạp bất đồng bộ prefab theo path; trả null nếu không có.
        /// </summary>
        UniTask<GameObject> LoadAsync(string path);
    }
}
