//#define DEBUG_POOL

using System.Collections.Generic;
using Cratesmith.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
#if PLAYMAKER
using HutongGames.PlayMaker.Actions;
#endif

#pragma warning disable 618

namespace Cratesmith.Actors.Pool
{

    [System.Serializable]
    public class PoolTable
    {
        [SerializeField] string name;
        [SerializeField] GameObject prefab;
        [SerializeField] List<GameObject> inUse = new List<GameObject>();
        [SerializeField] List<GameObject> free = new List<GameObject>();
        [SerializeField] List<GameObject> newObjects = new List<GameObject>();
        List<GameObject> despawnQueue = new List<GameObject>();
        Dictionary<GameObject, int> despawnCounts = new Dictionary<GameObject, int>();

        private Pool pool;
        private PoolTableRoot root;
        private static readonly List<Rigidbody> s_rigidbodyList = new List<Rigidbody>();

        public PoolTable(GameObject prefab, Pool pool)
        {
            this.name = prefab.name;
            this.prefab = prefab;
            this.pool = pool;
            this.root = pool.scene.IsDontDestroy()
                ? PoolTableRoot.GetOrCreateForDontDestroy(prefab)
                : PoolTableRoot.GetOrCreateForScene(pool.scene, prefab);

            for (int i = 0; i < root.transform.childCount; i++)
            {
                var obj = root.transform.GetChild(i).gameObject;
                free.Add(obj);
                despawnCounts[obj] = 0;
            }
        }

        public GameObject Prefab { get { return prefab; } }
        public int ActiveCount { get { return inUse.Count; } }
        public int FreeCount { get { return free.Count; } }
        public List<GameObject> ActiveObjects { get { return inUse; } }
        public List<GameObject> FreeObjects { get { return free; } }

        private GameObject CreateNew(Vector3 position, Quaternion rotation, Scene scene, Transform parent)
        {
            GameObject obj = (GameObject) GameObject.Instantiate(prefab, position, rotation, parent);
            despawnCounts[obj] = 0;
#if DEBUG_POOL
        Debug.Log("PoolTable: CreateNew - Attempting to move object to " + scene.name + " scene");
#endif
            if (obj.scene != scene && parent == null)
            {
                if (!scene.IsDontDestroy())
                {
                    SceneManager.MoveGameObjectToScene(obj, scene);
                }
                else
                {
#if DEBUG_POOL
	            Debug.LogWarning("PoolTable: CreateNew - Could not to move new object to " + scene.name + " scene");
#endif
                    GameObject.DontDestroyOnLoad(obj);
                }
            }

            return obj;
        }

        public void Preallocate(int count, Scene scene)
        {
            count -= inUse.Count;
            count -= free.Count;

            while (count > 0)
            {
                GameObject obj = CreateNew(Vector3.zero, Quaternion.identity, scene, root.transform);
                obj.SetActive(false);
                free.Add(obj);

                --count;
            }
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Scene scene, Transform parent)
        {
            GameObject obj = null;
            free.RemoveAll(m => m == null);
            if (free.Count == 0)
            {
//#if DEBUG
//            Debug.LogWarning("Spawning new: " + prefab.name);
//#endif
                obj = CreateNew(position, rotation, scene, parent);
            }
            else
            {
                obj = free[0];
                free.RemoveAt(0);

                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.transform.localScale = prefab.transform.localScale;


                obj.GetComponentsInChildren<Rigidbody>(s_rigidbodyList);
                foreach (var rigidbody in s_rigidbodyList)
                {
                    rigidbody.ClearVelocityAndIgnoredCollisions();
                }

               
                obj.transform.SetParent(parent, true);
                obj.SetActive(true);
                obj.hideFlags = 0;

                int despawnCount = -1;
                if (despawnCounts.TryGetValue(obj, out despawnCount) && despawnCount > 0)
                {
                    newObjects.Add(obj);
                }

                //Debug.Log("Spawning existing: " + obj.name);
            }

            inUse.Add(obj);
            return obj;
        }

        public int GetDespawnCount(GameObject obj)
        {
            int value = -1;
            despawnCounts.TryGetValue(obj, out value);
            return value;
        }

        public void Update()
        {
            while (despawnQueue.Count > 0)
            {
                GameObject obj = despawnQueue[0];

                despawnQueue.RemoveAt(0);
                if (obj == null)
                {
                    continue;
                }

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                inUse.Remove(obj);
                free.Add(obj);
            }

            foreach (var i in newObjects)
            {
                if (i == null)
                {
                    continue;
                }

                i.BroadcastMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
            newObjects.Clear();
        }

        public void Despawn(GameObject obj)
        {
            if (!despawnQueue.Contains(obj) && obj != null)
            {
                obj.BroadcastMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
                obj.SetActive(false);
                if (root != null)
                {
                    obj.transform.SetParent(root.transform, false);                    
                }
                

                ++despawnCounts[obj];
                var instance = pool.GetPoolInstance(obj);
                if (instance != null)
                {
                    instance.UpdateDespawnCount();
                }

                despawnQueue.Add(obj);
            }
        }

        public void SetPrefab(GameObject newPrefab)
        {
            if (prefab == newPrefab)
            {
                return;
            }

            prefab = newPrefab;
        }

        public override string ToString()
        {
            return name;
        }

        public void DespawnAll()
        {
            while (inUse.Count > 0)
            {
                if (inUse[0] == null)
                {
                    inUse.RemoveAll(x => x == null);
                }
                else
                {
                    Despawn(inUse[0]);
                    inUse.RemoveAt(0);
                }
            }
        }

        public void Rebuild()
        {
            var prevCount = FreeCount;
            while (free.Count > 0)
            {
                var item = free[0];
                if (item != null) Object.DestroyImmediate(item);
                free.RemoveAt(0);
            }
            Preallocate(prevCount, pool.scene);
        }

        public void Clear()
        {
            foreach (var gameObject in free)
            {
                Object.Destroy(gameObject);
                despawnCounts.Remove(gameObject);
            }
            free.Clear();
        }
    }
}