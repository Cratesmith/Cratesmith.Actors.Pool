using System.Collections.Generic;
using Cratesmith.Actors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cratesmith
{
    public class PoolRoot : SceneRoot<PoolRoot>
    {
        private readonly Dictionary<GameObject, PoolTableRoot> m_tables = new Dictionary<GameObject, PoolTableRoot>();

        public void AddTable(PoolTableRoot poolTableRoot)
        {
            if (poolTableRoot.prefab != null)
            {
                m_tables[poolTableRoot.prefab] = poolTableRoot;
            }
        }

        public PoolTableRoot GetTable(GameObject prefab)
        {   
            PoolTableRoot tableRoot = null;
            m_tables.TryGetValue(prefab, out tableRoot);
            return tableRoot;
        }

        public IEnumerable<PoolTableRoot> tables { get { return m_tables.Values; } }
    }
}