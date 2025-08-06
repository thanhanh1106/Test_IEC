using System.Collections.Generic;
using UnityEngine;
#if UNITY_2021_1_OR_NEWER
using UnityEngine.Pool;
#endif

// định dùng pool của unity nhưng cái bản cùi bắp này chưa có =))
public class ObjectPool : MonoBehaviour
{
    private static ObjectPool s_instance;
    public static ObjectPool Instance
    {
        get
        {
            if (s_instance == null)
            {
                GameObject go = new GameObject("ObjectPool");
                s_instance = go.AddComponent<ObjectPool>();
                DontDestroyOnLoad(go);
            }
            return s_instance;
        }
    }

#if UNITY_2021_1_OR_NEWER
    // Using Unity's ObjectPool for newer versions
    private Dictionary<string, IObjectPool<GameObject>> m_pools = new Dictionary<string, IObjectPool<GameObject>>();
    private Dictionary<string, GameObject> m_prefabs = new Dictionary<string, GameObject>();
#else
    // bản này chưa có 
    private Dictionary<string, Queue<GameObject>> m_pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> m_prefabs = new Dictionary<string, GameObject>();
#endif

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (s_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public GameObject SpawnFromPool(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab is null!");
            return null;
        }

        string prefabName = prefab.name;
        
#if UNITY_2021_1_OR_NEWER
        // Using Unity's ObjectPool
        if (!m_pools.ContainsKey(prefabName))
        {
            m_prefabs[prefabName] = prefab;
            m_pools[prefabName] = new ObjectPool<GameObject>(
                createFunc: () => CreatePooledObject(prefabName),
                actionOnGet: null, // Will handle positioning separately
                actionOnRelease: (obj) => OnReturnToPool(obj),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 100
            );
        }
        
        GameObject obj = m_pools[prefabName].Get();
        OnGetFromPool(obj, position, rotation, parent);
        return obj;
#else
        // Fallback for older Unity versions
        if (!m_pools.ContainsKey(prefabName))
        {
            m_prefabs[prefabName] = prefab;
            m_pools[prefabName] = new Queue<GameObject>();
        }

        GameObject objectToSpawn;
        if (m_pools[prefabName].Count > 0)
        {
            objectToSpawn = m_pools[prefabName].Dequeue();
        }
        else
        {
            objectToSpawn = CreatePooledObject(prefabName);
        }

        OnGetFromPool(objectToSpawn, position, rotation, parent);
        return objectToSpawn;
#endif
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null) return;

        string prefabName = GetPrefabName(objectToReturn);
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogWarning("Cannot determine prefab name for object: " + objectToReturn.name);
            Destroy(objectToReturn);
            return;
        }

#if UNITY_2021_1_OR_NEWER
        if (m_pools.ContainsKey(prefabName))
        {
            m_pools[prefabName].Release(objectToReturn);
        }
        else
        {
            Destroy(objectToReturn);
        }
#else
        if (m_pools.ContainsKey(prefabName))
        {
            OnReturnToPool(objectToReturn);
            m_pools[prefabName].Enqueue(objectToReturn);
        }
        else
        {
            Destroy(objectToReturn);
        }
#endif
    }

    private GameObject CreatePooledObject(string prefabName)
    {
        if (!m_prefabs.ContainsKey(prefabName))
        {
            Debug.LogError("Prefab not found: " + prefabName);
            return null;
        }

        GameObject obj = Instantiate(m_prefabs[prefabName]);
        obj.name = prefabName; // Keep original name for identification
        return obj;
    }

    private void OnGetFromPool(GameObject obj, Vector3 position, Quaternion rotation, Transform parent)
    {
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        if (parent != null)
        {
            obj.transform.SetParent(parent);
        }
    }

    private void OnReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(this.transform);
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
    }

    private string GetPrefabName(GameObject obj)
    {
        // Try to get the original prefab name
        string name = obj.name.Replace("(Clone)", "").Trim();
        return name;
    }

    // Legacy method for backward compatibility
    public GameObject SpawnFromPool(string prefabName, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        // Try to find prefab by name
        if (m_prefabs.ContainsKey(prefabName))
        {
            return SpawnFromPool(m_prefabs[prefabName], position, rotation, parent);
        }

        Debug.LogWarning("Prefab with name '" + prefabName + "' not found in pool. Use SpawnFromPool(GameObject prefab, ...) instead.");
        return null;
    }

    // Clear all pools
    public void ClearAllPools()
    {
#if UNITY_2021_1_OR_NEWER
        foreach (var pool in m_pools.Values)
        {
            pool.Clear();
        }
#else
        foreach (var pool in m_pools.Values)
        {
            while (pool.Count > 0)
            {
                Destroy(pool.Dequeue());
            }
        }
#endif
        m_pools.Clear();
        m_prefabs.Clear();
    }
}
