using UnityEngine;

namespace Cratesmith.Actors.Pool
{
    public class PoolInstance
    {
        GameObject instance;
        PoolTable table;
        public int despawnCount { get; private set; }

        public PoolInstance(GameObject instance, bool inUse, PoolTable table)
        {
            this.instance = instance;
            this.table = table;
            this.despawnCount = table.GetDespawnCount(instance);
        }
       
        public GameObject Instance { get { return this.instance; } }
        public PoolTable Table { get { return this.table; } }

        public void UpdateDespawnCount()
        {
            this.despawnCount = table.GetDespawnCount(instance);
        }
    }
}