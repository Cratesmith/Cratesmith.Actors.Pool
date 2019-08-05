using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR

namespace Cratesmith.Actors.Pool
{
    public static class PoolMenu 
    {
        [MenuItem("Tools/Pools/Clear Pools &#c",true)]
        public static bool _ClearPools()
        {
            return Application.isPlaying;
        }

        [MenuItem("Tools/Pools/Clear Pools &#c")]
        public static void ClearPools()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    var pool = Pool.Get(scene);
                    pool.Clear();
                }
            }
        }
    }
}
#endif