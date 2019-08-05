using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Cratesmith
{
// A pool reference for game objects, works just fine for non-pooled objects too!
    public struct Handle : System.IEquatable<GameObject>, System.IEquatable<Handle>
    {
        readonly int        m_hashCode;
        readonly GameObject m_cachedObj;
        readonly int        m_despawnId;
        readonly Pool       m_cachedPool;
        PoolInstance        m_ptr;

        public int        hashCode {get { return m_hashCode; }}
        public GameObject rawValue { get { return m_cachedObj; } }
        public GameObject value { get { return (GameObject) this; } }
        public Pool pool { get { return m_cachedPool; } }
        public bool valid { get { return (bool) this; } }

        public Handle(Handle other)
        {
            m_despawnId = other.m_despawnId;
            m_ptr = other.m_ptr;
            m_cachedPool = other.m_cachedPool;
            m_cachedObj = other.m_cachedObj;
            m_hashCode = other.m_hashCode;
        }
        
        public Handle(GameObject src)
        {
            m_cachedObj = src;
            if (src != null)
            {
                m_cachedPool = Pool.Get(src.scene);
                m_ptr = m_cachedPool!=null 
                    ? m_cachedPool.GetPoolInstance(src)
                    : null;
                m_despawnId = m_ptr != null ? m_ptr.despawnCount : 0; // no ptr means either not a pooled object or it's still being spawned
            }
            else
            {
                m_despawnId = -1;
                m_cachedPool = null;
                m_ptr = null;
            }
            m_hashCode = GenerateHash(src,m_despawnId);
        }

        internal static int GenerateHash(UnityEngine.Object src, int despawnCount)
        {
            int hash = !ReferenceEquals(src,null) ? src.GetInstanceID() : 0;
            int despawn = (int)((uint)despawnCount >> 16);
            return hash + despawn;
        }

        public Handle(GameObject src, Pool pool)
        {
            m_cachedObj = src;
            m_cachedPool = pool;
            m_ptr = pool != null ? pool.GetPoolInstance(src) : null;
            m_despawnId = m_ptr != null
                ? m_ptr.despawnCount
                : -1;
            m_hashCode = GenerateHash(src,m_despawnId);
        }

        public static implicit operator Handle(GameObject other)
        {
            return new Handle(other);
        }

        public static implicit operator bool(Handle me)
        {
            return ((GameObject) me) != null;
        }

        public static implicit operator GameObject(Handle me)
        {
            // no pointer, this is a non-pool object (or hasn't been fixed up)
            if (me.m_ptr == null)
            {
                if (me.m_cachedObj != null && me.m_cachedPool != null)
                {
                    me.m_ptr = me.m_cachedPool.GetPoolInstance(me.m_cachedObj);
                }

                if(me.m_ptr==null)
                {
                    return me.m_cachedObj;                    
                }
            }

            // check that the object hasn't been despawned
            if (me.m_ptr.Instance == null || me.m_despawnId != me.m_ptr.despawnCount)
            {
                me.m_ptr = null;
                return null;
            }
            // return the component from the object
            return me.m_ptr.Instance;
        }

        public bool Equals(GameObject other)
        {
            return value == other;
        }

        public bool Equals(Handle other)
        {
            return value == other.value && m_despawnId==other.m_despawnId;
        }

        public void InvokeIfValid(System.Action<GameObject> func)
        {
            if (valid)
            {
                func.Invoke(value);
            }
        }
		
	    public override int GetHashCode()
	    {
	        int hash = ReferenceEquals(m_cachedObj,null) ? m_cachedObj.GetInstanceID() : 0;
	        int despawn = (int)((uint)m_despawnId >> 16);
	        return hash + despawn;
	    }
	}

// A pool reference for components, works just fine for non-pooled objects too!
    public struct Handle<T> : System.IEquatable<T>, System.IEquatable<Handle<T>> where T : Component
    {
        readonly int m_hashCode;
        readonly T m_cachedObj;
        readonly Pool m_cachedPool;
        readonly int m_despawnId;
        PoolInstance m_ptr;

        public int        hashCode {get { return m_hashCode; }}
        public T rawValue { get { return m_cachedObj; } }
        public T value { get { return (T) this; } }
        public Pool pool { get { return m_cachedPool; } }
        public bool valid { get { return (bool) this; } }

        public Handle(Handle<T> other)
        {
            m_ptr = other.m_ptr;
            m_cachedPool = other.m_cachedPool;
            m_cachedObj = other.m_cachedObj;
            m_despawnId = other.m_despawnId;
            m_hashCode = other.m_hashCode;
        }

        public Handle(T src)
        {
            m_cachedObj = src;
            if (src != null)
            {
                m_cachedPool = Pool.Get(src.gameObject.scene);
                m_ptr = m_cachedPool != null ? m_cachedPool.GetPoolInstance(src.gameObject) : null;
                m_despawnId = m_ptr != null ? m_ptr.despawnCount : 0;
            }
            else
            {
                m_cachedPool = null;
                m_ptr = null;
                m_despawnId = -1;
            }
            m_hashCode = Handle.GenerateHash(m_cachedObj, m_despawnId);
        }

        public Handle(T src, Pool pool)
        {
            m_cachedObj = src;
            m_cachedPool = pool;
            m_ptr = pool != null && src != null ? pool.GetPoolInstance(src.gameObject) : null;
            m_despawnId = m_ptr != null ? m_ptr.despawnCount : 0;
            m_hashCode = Handle.GenerateHash(m_cachedObj, m_despawnId);
        }

        public static implicit operator Handle<T>(T other)
        {
            return new Handle<T>(other);
        }

        public static implicit operator bool(Handle<T> me)
        {
            return ((T) me) != null;
        }

        public static implicit operator T(Handle<T> me)
        {
            // no pointer, this is a non-pool object (or hasn't been fixed up)
            if (me.m_ptr == null)
            {
                if (me.m_cachedObj != null && me.m_cachedPool != null)
                {
                    me.m_ptr = me.m_cachedPool.GetPoolInstance(me.m_cachedObj.gameObject);
                }

                if(me.m_ptr==null)
                {
                    return me.m_cachedObj;                    
                }
            }

            // check that the object hasn't been despawned
            if (me.m_ptr.Instance == null || me.m_despawnId != me.m_ptr.despawnCount)
            {
                return null;
            }

            // return the component from the object
            return me.m_cachedObj;
        }

        public bool Equals(T other)
        {
            return value == other;
        }

        public bool Equals(Handle<T> other)
        {            
            return value == other.value && m_despawnId==other.m_despawnId;
        }

        public void InvokeIfValid(System.Action<T> func)
        {
            if (valid)
            {
                func.Invoke(value);
            }
        }

	    public override int GetHashCode()
	    {
	        return m_hashCode;
	    }
    }
}