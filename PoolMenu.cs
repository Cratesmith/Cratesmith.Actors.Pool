using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;

namespace Cratesmith
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