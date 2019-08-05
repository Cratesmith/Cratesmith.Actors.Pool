using Cratesmith.Actors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cratesmith
{
    public class PoolTableRoot : Actor
    {
        public GameObject prefab;
        public PoolTable table { get; set; }

        static PoolTableRoot GetOrCreate(PoolRoot root, GameObject prefab)
        {
            var table = root.GetTable(prefab);
            if (table == null)
            {
                var newGo = new GameObject("PoolTable:" + prefab.name);
                newGo.transform.SetParent(root.transform, false);
                table = newGo.AddComponent<PoolTableRoot>();
                table.prefab = prefab;
                root.AddTable(table);
            }
            return table;
        }

        public static PoolTableRoot GetOrCreateForDontDestroy(GameObject prefab)
        {
            return GetOrCreate(PoolRoot.GetDontDestroy(), prefab);
        }

        public static PoolTableRoot GetOrCreateForScene(Scene scene, GameObject prefab)
        {
            return GetOrCreate(PoolRoot.Get(scene), prefab);
        }

        protected virtual void Awake()
        {
            var poolRoot = GetComponentInParent<PoolRoot>();
            if (prefab != null)
            {
                poolRoot.AddTable(this);
            }
        }
    }
}