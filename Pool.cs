using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Cratesmith.Actors;
using Cratesmith.ScriptExecutionOrder;
using Cratesmith.Utils;
using UnityEngine.SceneManagement;

namespace Cratesmith
{
    [ScriptExecutionOrder(-900)]
    public class Pool : SceneSingleton<Pool>
    {
        [System.Serializable]
        public class Preallocation
        {
            public GameObject prefab;
            public int count;
        }

        static List<Pool> s_allPools = new List<Pool>();

        [SerializeField] Preallocation[] preallocations = new Preallocation[0];
        [SerializeField] List<PoolTable> items = new List<PoolTable>();

        Dictionary<GameObject, PoolTable> poolTables = new Dictionary<GameObject, PoolTable>();
        Dictionary<GameObject, PoolTable> overriddenPoolTables = new Dictionary<GameObject, PoolTable>();
        Dictionary<GameObject, PoolInstance> poolInstances = new Dictionary<GameObject, PoolInstance>();

        private PoolRoot m_poolRoot;

        public List<PoolTable> Items { get { return items; } }

        private PoolTable GetOrCreateTable(GameObject prefab)
        {
            PoolTable table;
            if (!poolTables.TryGetValue(prefab, out table))
            {
                table = new PoolTable(prefab, this);
                poolTables[prefab] = table;
                items.Add(table);
            }

            return table;
        }

        private void DoPreallocate(GameObject prefab, int count)
        {
            GetOrCreateTable(prefab).Preallocate(count, scene);
        }

        private GameObject DoSpawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            enabled = true;
            PoolTable table = GetOrCreateTable(prefab);
            GameObject obj = table.Spawn(position, rotation, scene, parent);

            // note the null check!
            // objects can actually kill themselves during Awake. 
            // Despawn will delete them because they have no PoolInstance yet
            if (obj != null && !poolInstances.ContainsKey(obj)) 
            {
                poolInstances[obj] = new PoolInstance(obj, true, table);                
            }
            return obj;
        }

        private void DoDespawn_FindChildren(GameObject obj)
        {
            var objTransform = obj.transform;
            for (int i = 0; i < objTransform.childCount; ++i)
            {
                var childObj = objTransform.GetChild(i).gameObject;
                DoDespawn_FindChildren(childObj);

                PoolInstance inst;
                if (poolInstances.TryGetValue(childObj, out inst))
                {
                    //Debug.Log("Despawning child: " + childObj.name);
                    DoDespawn_DespawnInstance(inst);
                }
            }
        }

        bool DoDespawn_DespawnInstance(PoolInstance inst)
        {
            PoolTable table = inst.Table;
            if (table != null)
            {
                table.Despawn(inst.Instance);
                enabled = true;
                return true;
            }
            return false;
        }

        private bool DoDespawn(GameObject obj)
        {
            PoolInstance inst;
            if (obj == null)
            {
                return false;
            }

            DoDespawn_FindChildren(obj);

            if (poolInstances.TryGetValue(obj, out inst))
            {
                if (DoDespawn_DespawnInstance(inst))
                {
                    return true;
                }
            }

            return false;
        }

        private void DoReplace(GameObject prefab, GameObject otherPrefab)
        {
            Debug.Log("Replacing " + prefab.name + " with " + otherPrefab.name);

            PoolTable table;
            if (!poolTables.TryGetValue(prefab, out table))
            {
                Debug.LogError("Prefab does not exist to replace: " + prefab.name + " with: " + otherPrefab.name);
                return;
            }

            if (table.Prefab == otherPrefab)
            {
                Debug.Log("Prefab to replace already matches the new prefab, ignoring");
                return;
            }

            // Despawn current instances
            foreach (var pair in poolInstances)
            {
                if (pair.Value.Table == table)
                {
                    table.Despawn(pair.Key);
                }
            }

            // Process despawns next update
            enabled = true;

            // Check overriden pool tables so see if other prefab already has a table
            PoolTable otherTable;
            if (overriddenPoolTables.TryGetValue(otherPrefab, out otherTable))
            {
                Debug.Log("Using existing overridden pool table");
                overriddenPoolTables.Remove(otherPrefab);
            }
            else
            {
                Debug.Log("Creating new pool table");
                otherTable = new PoolTable(otherPrefab, this);
                items.Add(otherTable);

                // Preallocate the same number of instances
                otherTable.Preallocate(table.ActiveCount + table.FreeCount, scene);
            }

            // Move the old table to the overriden tables
            overriddenPoolTables[table.Prefab] = table;

            // Replace the pool table reference
            poolTables[prefab] = otherTable;
        }

        protected override void OnAwake()
        {
            s_allPools.Add(this);
            m_poolRoot = PoolRoot.Get(scene);
            if (m_poolRoot)
            {
                foreach (var tableRoot in m_poolRoot.tables)
                {
                    GetOrCreateTable(tableRoot.prefab);
                }
            }

            foreach (Preallocation preallocation in preallocations)
            {
                DoPreallocate(preallocation.prefab, preallocation.count);
            }
        }

        void OnDestroy()
        {
            if(ApplicationState.isQuitting) return;
            s_allPools.Remove(this);
        }

        private void DespawnAll()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].DespawnAll();
                }
            }
        }

	    void Update()
	    {
		    for (int i = 0; i < items.Count; i++)
		    {
			    if (items[i] != null)
			    {
				    items[i].Update();
			    }
		    }
	    }

	    void LateUpdate()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].Update();
                }
            }
            enabled = false;
        }

        public PoolTable GetTable(GameObject prefab)
        {
            return GetOrCreateTable(prefab);
        }

        public Handle Spawn(GameObject prefab, Transform parent = null)
        {
            var pos = Vector3.zero;
            var rot = Quaternion.identity;
            if (parent != null)
            {
                pos = parent.position;
                rot = parent.rotation;
            }
            return new Handle(DoSpawn(prefab, pos, rot, parent), this);
        }

        public Handle Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return new Handle(DoSpawn(prefab, position, rotation, parent), this);
        }

        public Handle<T> Spawn<T>(T prefab, Transform parent = null) where T : Component
        {
            if (!prefab)
            {
                Debug.LogError("The thing you're trying to instantiate is null", parent);
            }
            else
            {
                var pos = Vector3.zero;
                var rot = Quaternion.identity;
                if (parent != null)
                {
                    pos = parent.position;
                    rot = parent.rotation;
                }
                GameObject obj = DoSpawn(prefab.gameObject, pos, rot, parent);
                if (obj)
                {
                    return new Handle<T>(obj.GetComponent<T>(), this);
                }                               
            }
            return null;
        }

        public Handle<T> Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            GameObject obj = DoSpawn(prefab.gameObject, position, rotation, parent);
            if (obj)
            {
                if (parent == null && scene.IsDontDestroy())
                {
                    SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
                }
                return new Handle<T>(obj.GetComponent<T>(), this);
            }

            return null;
        }

        private static void Despawn_Common(GameObject obj, Pool firstPool)
        {
            if (firstPool != null && firstPool.DoDespawn(obj))
            {
                return;
            }

            var e = s_allPools.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current == null || e.Current == firstPool)
                {
                    continue;
                }

                if (e.Current.DoDespawn(obj))
                {
                    return;
                }
            }

            //Debug.LogWarning("Couldn't find pool responsible for "+obj.name+" destroying instead");
            Destroy(obj);
        }

        public static void Despawn(Handle obj)
        {
            if (!obj) return;
            var pool = obj.pool;
            Despawn_Common(obj, pool);
        }

        public static void Despawn(GameObject obj)
        {
            if (!obj) return;            
            var pool = Get(obj.scene);
            Despawn_Common(obj, pool);
        }

        public static void Despawn<T>(Handle<T> obj) where T : Component
        {
            if (!obj) return;
            var pool = obj.pool;
            Despawn_Common(obj.value.gameObject, pool);
        }

        public static void Despawn<T>(T obj) where T : Component
        {
            if (!obj) return;
            var pool = Get(obj.gameObject.scene);
            Despawn_Common(obj.gameObject, pool);
        }

        public void Replace(GameObject prefab, GameObject otherPrefab)
        {
            DoReplace(prefab, otherPrefab);
        }

        public void Revert(GameObject prefab)
        {
            DoReplace(prefab, prefab);
        }

        public int GetActiveCount(GameObject prefab)
        {
            PoolTable table;
            if (poolTables.TryGetValue(prefab, out table))
            {
                return table.ActiveCount;
            }

            return 0;
        }

        public PoolInstance GetPoolInstance(GameObject obj)
        {
            PoolInstance inst = null;
            if (poolInstances.TryGetValue(obj, out inst))
            {
                return inst;
            }            
            return null;
        }

        public Handle FindRef(Transform parent, bool returnNonPoolHandleOnFailure = true)
        {
            var current = parent;
            while (current != null)
            {
                var inst = GetPoolInstance(current.gameObject);
                if (inst != null)
                {
                    return new Handle(current.gameObject, this);
                }
                current = current.parent;
            }

            return returnNonPoolHandleOnFailure
                ? new Handle(parent.gameObject)
                : null;
        }
        
        public void Clear()
        {
            foreach (var poolTable in poolTables)
            {
                poolTable.Value.Clear();
            }

            foreach (var overriddenPoolTable in overriddenPoolTables)
            {
                overriddenPoolTable.Value.Clear();
            }
        }
    }
}