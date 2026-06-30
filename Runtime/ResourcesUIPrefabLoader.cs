using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HungNT.UI.Panel
{
    /// <summary>
    /// Loader mặc định: nạp prefab từ Resources. Dùng khi dự án không inject loader riêng,
    /// để package chạy độc lập (demo, dự án Resources-based).
    /// </summary>
    public sealed class ResourcesUIPrefabLoader : IUIPrefabLoader
    {
        public GameObject Load(string path)
        {
            return Resources.Load<GameObject>(path);
        }

        public async UniTask<GameObject> LoadAsync(string path)
        {
            var request = Resources.LoadAsync<GameObject>(path);
            await request.ToUniTask();
            return request.asset as GameObject;
        }
    }
}
