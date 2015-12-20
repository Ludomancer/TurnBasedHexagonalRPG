using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PoolManager : Manager
{
    public static PoolManager instance;

    private int _currentOperations = 0;

    /// <summary>
    /// Is Pool Manager available.
    /// </summary>
    public bool IsAvailable
    {
        get { return _currentOperations == 0; }
    }

    /// <summary>
    /// Total Buffer progress.
    /// </summary>
    [HideInInspector]
    public int totalProgress = -1;
    /// <summary>
    /// Current Buffer progress.
    /// </summary>
    [HideInInspector]
    public int currentProgress = -1;

    /// <summary>
    /// Array of Bufferable items.
    /// </summary>
    [Serializable]
    public class Buffer
    {
        public GameObject prefab;
        public int bufferAmount;
        internal int currentBufferAmount = 0;
        public int maximumBufferAmount;
        public Stack<GameObject> pooledObjects = new Stack<GameObject>();
    }


    /// <summary>
    /// The object prefabs which the pool can handle.
    /// </summary>
    public Buffer[] buffer;

    /// <summary>
    /// The pooled objects to be accessed by tags currently available.
    /// </summary>
    private Dictionary<string, List<int>> _objectIndexesByTag;

    /// <summary>
    /// To reduce heap allocations.
    /// </summary>
    private Dictionary<string, int> _objectIndexesByName;

    /// <summary>
    /// Its the default amount to buffer if object buffer has value -1.
    /// </summary>
    public int defaultBufferAmount = 3;

    /// <summary>
    /// Should we calculate the buffer loading percentage and report it.
    /// </summary>
    public bool reportPercentage = false;

    /// <summary>
    /// Should we index object by tags. Faster spawning and recycling but uses more memory. See Remark.
    /// </summary>
    /// <remarks>
    /// Setting this option to false will seriously slows down any functionality about tags. 
    /// Disable it only if you know what you are doing!
    /// </remarks>
    public bool indexObjectsByTag = false;

    /// <summary>
    /// Should we index object by tags. Faster spawning and recycling but uses more memory.
    /// </summary>
    /// <remarks>
    /// Setting this option to false will seriously slows down all basic fucntionality such as
    /// Recycle, GetObjectByname, Pool etc. and also any functionality about names.. 
    /// Disable it only if you know what you are doing!
    /// </remarks>
    public bool indexObjectsByName = false;

    /// <summary>
    /// How often we should yield while buffering.
    /// lessthen 0 for no yield.
    /// </summary>
    public float yieldEveryNItemWhenBuffering = -1;

    /// <summary>
    /// The container object that we will keep unused pooled objects so we dont clog up the editor with objects.
    /// </summary>
    protected Transform containerObject;
    /// <summary>
    /// The container object that we will keep used pooled objects so we dont clog up the editor with objects.
    /// </summary>
    protected Transform spawnedFromPool;

    /// <summary>
    /// To prevent recreation of a new list everytime we search indexes.
    /// </summary>
    List<int> _tagSearchResults;
    /// <summary>
    /// To prevent recreation of a new list everytime we search indexes.
    /// </summary>
    List<int> _availableSearchResults;

    /// <summary>
    /// Gets the total count of items to buffer.
    /// </summary>
    private int GetTotalItemsToBuffer()
    {
        int totalBuffer = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            totalBuffer += buffer[i].bufferAmount;
        }
        return totalBuffer;
    }

    private bool _isPoolInitialized;

    void Awake()
    {
        PoolManager[] pms = FindObjectsOfType<PoolManager>();
        if (pms.Length == 0) Debug.Log("Cannot find 'PoolManager' object");
        else if (pms.Length > 1) Debug.Log("There are more than 1 'PoolManager' object in Scene");
        instance = pms[0];
        //Creating the container objects.
        containerObject = new GameObject("[]ObjectPool").transform;
        containerObject.position = Vector3.one * -999;
        spawnedFromPool = new GameObject("[]SpawnedFromPool").transform;
        _availableSearchResults = new List<int>(10);
        _tagSearchResults = new List<int>(50);
        if (indexObjectsByTag) _objectIndexesByTag = new Dictionary<string, List<int>>();
        if (indexObjectsByName) _objectIndexesByName = new Dictionary<string, int>(buffer.Length);
    }
    public override void Init()
    {
        useGUILayout = false;
        if (!_isPoolInitialized) FillBufferBlocking();
    }

    /// <summary>
    /// Fills the buffer for only specified list of items and blocks the current thread until this operation finishes. Faster than coroutine version but freezes other stuff.
    /// </summary>
    /// <param name='bufferItems'>
    /// List of Buffer items to be buffered.
    /// </param>
    public void FillBufferBlocking(params Buffer[] bufferItems)
    {
        if (bufferItems != null && bufferItems.Length > 0)
        {
            if (reportPercentage) totalProgress = GetTotalItemsToBuffer() + buffer.Length;
            _currentOperations++;
            Vector3 spawnPos = new Vector3(500, 500, 500);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (indexObjectsByName)
                {
                    if (!_objectIndexesByName.ContainsKey(buffer[i].prefab.name))
                    {
                        _objectIndexesByName.Add(buffer[i].prefab.name, i);
                    }
                }
                for (int j = 0; j < bufferItems.Length; j++)
                {
                    if (bufferItems[j] != null && buffer[i].prefab.name.Equals(bufferItems[j].prefab.name))
                    {
                        GameObject prefabCache = bufferItems[j].prefab.gameObject;

                        int bufferAmount;
                        if (bufferItems[j].bufferAmount != -1) bufferAmount = bufferItems[j].bufferAmount;
                        else bufferAmount = defaultBufferAmount;

                        buffer[i].currentBufferAmount = bufferAmount;
                        if (buffer[i].maximumBufferAmount == 0) buffer[i].maximumBufferAmount = int.MaxValue;

                        if (indexObjectsByTag)
                        {
                            if (!_objectIndexesByTag.ContainsKey(prefabCache.tag))
                            {
                                _objectIndexesByTag.Add(prefabCache.tag, new List<int>());
                            }
                            _objectIndexesByTag[prefabCache.tag].Add(i);
                        }
                        bool isPreloaded = false;
                        for (int n = 0; n < bufferAmount; n++)
                        {
                            GameObject newObj = Instantiate(prefabCache, spawnPos, Quaternion.identity) as GameObject;
                            if (!isPreloaded)
                            {
                                isPreloaded = true;
                            }
                            newObj.name = prefabCache.name;
                            Recycle(newObj);
                        }
                        bufferItems[j] = null;
                    }
                }
            }
            Shader.WarmupAllShaders();
            _currentOperations--;
        }
        _isPoolInitialized = true;
    }

    /// <summary>
    /// Fills the buffer and blocks the current thread until this operation finishes. Faster than coroutine version but freezes other stuff.
    /// </summary>
    public void FillBufferBlocking()
    {
        _currentOperations++;
        Vector3 spawnPos = new Vector3(500, 500, 500);
        if (reportPercentage) totalProgress = GetTotalItemsToBuffer() + buffer.Length;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (indexObjectsByName)
            {
                if (!_objectIndexesByName.ContainsKey(buffer[i].prefab.name))
                {
                    _objectIndexesByName.Add(buffer[i].prefab.name, i);
                }
            }
            if (buffer[i].pooledObjects.Count == 0)
            {

                GameObject prefabCache = buffer[i].prefab;

                int bufferAmount;
                if (buffer[i].bufferAmount != -1) bufferAmount = buffer[i].bufferAmount;
                else bufferAmount = defaultBufferAmount;

                buffer[i].currentBufferAmount = bufferAmount;
                if (buffer[i].maximumBufferAmount == 0) buffer[i].maximumBufferAmount = int.MaxValue;

                if (indexObjectsByTag)
                {
                    if (!_objectIndexesByTag.ContainsKey(prefabCache.tag))
                    {
                        _objectIndexesByTag.Add(prefabCache.tag, new List<int>());
                    }
                    _objectIndexesByTag[prefabCache.tag].Add(i);
                }
                bool isPreloaded = false;
                for (int n = 0; n < bufferAmount; n++)
                {
                    GameObject newObj = Instantiate(prefabCache, spawnPos, Quaternion.identity) as GameObject;
                    if (!isPreloaded)
                    {
                        isPreloaded = true;
                    }
                    newObj.name = prefabCache.name;
                    Recycle(newObj);
                }
            }
        }
        Shader.WarmupAllShaders();
        _currentOperations--;
        _isPoolInitialized = true;
    }

    /// <summary>
    /// Starts FillBuffer routine.
    /// </summary>
    public void StartFillBufferRoutine()
    {
        if (reportPercentage) totalProgress = GetTotalItemsToBuffer() + buffer.Length;
        StartCoroutine("FillBuffer");
    }

    /// <summary>
    /// Fills the Buffer and returns yield every "yieldEveryNItemWhenBuffering" items.
    /// </summary>
    public IEnumerator FillBuffer()
    {
        _currentOperations++;
        int bufferAmount;
        bool isPreloaded;
        Vector3 spawnPos = new Vector3(500, 500, 500);
        for (int i = 0; i < buffer.Length; i++)
        {
            GameObject prefabCache;
            GameObject newObj;
            if (indexObjectsByName)
            {
                if (!_objectIndexesByName.ContainsKey(buffer[i].prefab.name))
                {
                    _objectIndexesByName.Add(buffer[i].prefab.name, i);
                }
            }
            if (buffer[i].pooledObjects.Count == 0)
            {
                prefabCache = buffer[i].prefab;

                if (buffer[i].bufferAmount != -1) bufferAmount = buffer[i].bufferAmount;
                else bufferAmount = defaultBufferAmount;

                buffer[i].currentBufferAmount = bufferAmount;
                if (buffer[i].maximumBufferAmount == 0) buffer[i].maximumBufferAmount = int.MaxValue;

                if (indexObjectsByTag)
                {
                    if (!_objectIndexesByTag.ContainsKey(prefabCache.tag))
                    {
                        _objectIndexesByTag.Add(prefabCache.tag, new List<int>());
                    }
                    _objectIndexesByTag[prefabCache.tag].Add(i);
                }
                isPreloaded = false;
                for (int n = 0; n < bufferAmount; n++)
                {
                    newObj = Instantiate(prefabCache, spawnPos, Quaternion.identity) as GameObject;
                    if (!isPreloaded)
                    {
                        isPreloaded = true;
                        yield return new WaitForEndOfFrame();
                    }
                    newObj.name = prefabCache.name;
                    Recycle(newObj);
                    if (reportPercentage) currentProgress++;
                    if (yieldEveryNItemWhenBuffering > 0 && (currentProgress % yieldEveryNItemWhenBuffering == 0 || currentProgress == totalProgress)) yield return new WaitForEndOfFrame();
                }
            }
        }
        Shader.WarmupAllShaders();
        _currentOperations--;
        _isPoolInitialized = true;
    }

    /// <summary>
    /// Adds specified set of Buffer items from the Buffer list. See Remark.
    /// </summary>
    /// <param name='objectsToBeAdded'>
    /// List of Buffer items to be added to the Buffer list.
    /// </param>
    /// <remarks> WARNING : This operation should not be used while the pool is active used. </remarks>
    public void AddToBuffer(params Buffer[] objectsToBeAdded)
    {
        if (objectsToBeAdded != null && objectsToBeAdded.Length > 0)
        {

            List<Buffer> tempBuffer = new List<Buffer>(buffer.Length + objectsToBeAdded.Length);
            tempBuffer.AddRange(buffer);
            tempBuffer.AddRange(objectsToBeAdded);
            buffer = tempBuffer.ToArray();
        }
    }

    /// <summary>
    /// Removes specified set of Buffer items from the Buffer list. See Remark.
    /// </summary>
    /// <param name='poolAll'>
    /// Wheter we should PoolEverything before destructive operation to make sure nothing is left out. If this option is false only Buffer items specified will be pooled. 
    /// </param>
    /// <param name='itemNames'>
    /// List of Buffer items names to be removed from Buffer list.
    /// </param>
    /// <remarks> WARNING : This operation should not be used while the pool is active used. </remarks>
    public void RemoveFromBuffer(bool poolAll, params string[] itemNames)
    {
        if (poolAll) RecycleAll();
        else
        {
            List<string> objectNames = new List<string>(itemNames.Length);
            for (int i = 0; i < itemNames.Length; i++)
            {
                objectNames.Add(itemNames[i]);
            }
            RecycleAllWithName(objectNames.ToArray());
        }

        for (int i = 0; i < buffer.Length; i++)
        {
            for (int k = 0; k < itemNames.Length; k++)
            {
                if (buffer[i].pooledObjects.Count > 0 && buffer[i].prefab.name.Equals(itemNames[k]))
                {
                    while (buffer[i].pooledObjects.Count > 0)
                    {
                        Destroy(buffer[i].pooledObjects.Pop());
                    }
                    buffer[i] = null;
                    break;
                }
            }
        }

        //Remove nulls from buffer
        List<Buffer> tempBuffer = new List<Buffer>();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != null) tempBuffer.Add(buffer[i]);
        }
        buffer = tempBuffer.ToArray();

        //Lets hope that they are still syncornized
        RecreateIndexes();
        Resources.UnloadUnusedAssets();
    }

    private void RecreateIndexes()
    {
        if (!indexObjectsByName && !indexObjectsByTag) return;
        _currentOperations++;
        //reindex tag and name cache
        if (indexObjectsByName) _objectIndexesByName.Clear();
        if (indexObjectsByTag) _objectIndexesByTag.Clear();
        for (int i = 0; i < buffer.Length; i++)
        {
            GameObject prefabCache = buffer[i].prefab;
            if (indexObjectsByName)
            {
                if (!_objectIndexesByName.ContainsKey(prefabCache.name))
                {
                    _objectIndexesByName.Add(prefabCache.name, i);
                }
            }
            if (indexObjectsByTag)
            {
                if (!_objectIndexesByTag.ContainsKey(prefabCache.tag))
                {
                    _objectIndexesByTag.Add(prefabCache.tag, new List<int>());
                }
                _objectIndexesByTag[prefabCache.tag].Add(i);
            }
        }
        if (indexObjectsByTag)
        {
            foreach (var item in _objectIndexesByTag)
            {
                foreach (int subitem in item.Value)
                {
                    if (buffer[subitem] == null) Debug.Log("Null in Index by Tag : " + buffer[subitem].prefab.name);
                }
            }
        }
        if (indexObjectsByName)
        {
            foreach (var item in _objectIndexesByName)
            {
                if (buffer[item.Value] == null) Debug.Log("Null in Index by Name : " + buffer[item.Value].prefab.name);
            }
        }
        _currentOperations--;
    }

    /// <summary>
    /// Removes specified set of Buffer items from the Buffer list. See Remark.
    /// </summary>
    /// <param name='poolAll'>
    /// Wheter we should PoolEverything before destructive operation to make sure nothing is left out. If this option is false only Buffer items specified will be pooled. 
    /// </param>
    /// <param name='items'>
    /// List of Buffer items to be removed from Buffer list.
    /// </param>
    /// <remarks> WARNING : This operation should not be used while the pool is active used. </remarks>
    public void RemoveFromBuffer(bool poolAll, params Buffer[] items)
    {
        if (items != null && items.Length > 0)
        {
            string[] itemNames = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                itemNames[i] = items[i].prefab.name;
            }
            RemoveFromBuffer(poolAll, itemNames);
        }
    }

    private void Enable(GameObject go)
    {
        go.SetActive(true);
    }


    private void Disable(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(containerObject, false);
    }


    /// <summary>
    /// Finds and returns a GameObject from the pool by it's tag if available with the given parameters.
    /// </summary>
    /// <param name='objectName'>
    /// Name of the object to be searched.
    /// </param>
    /// <param name='onlyPooled'>
    /// Wheter we shuoul create a new object in case we have available free buffer space according to maximumBufferAmount.
    /// </param>
    public GameObject GetObjectForTag(string objectName, bool onlyPooled)
    {
        return GetObjectForTag(objectName, onlyPooled, Vector3.zero, Quaternion.identity, null);
    }


    /// <summary>
    /// Finds and returns a GameObject from the pool by it's tag if available with the given parameters.
    /// </summary>
    /// <param name='objectTag'>
    /// Tag of the object to be searched.
    /// </param>
    /// <param name='onlyPooled'>
    /// Wheter we shuoul create a new object in case we have available free buffer space according to maximumBufferAmount.
    /// </param>
    /// <param name='position'>
    /// Position to be set to the GameObject before returning.
    /// </param>
    /// <param name='angle'>
    /// Rotation to be set to the GameObject before returning.
    /// </param>
    /// <param name='parent'>
    /// Parent to be set to the GameObject before returning.
    /// </param>
    public GameObject GetObjectForTag(string objectTag, bool onlyPooled, Vector3 position, Quaternion angle, Transform parent)
    {
        Init();
        if (indexObjectsByTag)
        {
            _objectIndexesByTag.TryGetValue(objectTag, out _tagSearchResults);
        }
        else
        {
            _tagSearchResults.Clear();
            for (int index = 0; index < buffer.Length; index++)
            {
                if (buffer[index].prefab.tag.Equals(objectTag))
                {
                    _tagSearchResults.Add(index);
                    break;
                }
            }
            if (_tagSearchResults.Count == 0) return null;
        }

        _availableSearchResults.Clear();
        for (int i = 0; i < _tagSearchResults.Count; i++)
        {
            if (buffer[_tagSearchResults[i]].pooledObjects.Count != 0)
            {
                _availableSearchResults.Add(_tagSearchResults[i]);
            }
            else if (!onlyPooled)
            {
                if (buffer[_tagSearchResults[i]].currentBufferAmount < buffer[_tagSearchResults[i]].maximumBufferAmount)
                {
                    i--;
                    _tagSearchResults.Remove(_tagSearchResults[i]);
                }
            }
        }

        int selectedItem;
        if (_availableSearchResults.Count > 0)
        {
            selectedItem = _availableSearchResults[Random.Range(0, _availableSearchResults.Count)];
            GameObject tempGameObject = buffer[selectedItem].pooledObjects.Pop();
            Transform tempTransform = tempGameObject.transform;

            if (tempTransform.parent == null || (tempTransform.parent && !tempTransform.parent.Equals(parent)))
            {
                if (parent) tempTransform.parent = parent;
                else tempTransform.parent = spawnedFromPool.transform;
            }

            tempTransform.position = position;
            tempTransform.rotation = angle;
            Enable(tempGameObject);
            return tempGameObject;
        }
        else if (!onlyPooled && _tagSearchResults.Count > 0)
        {
            selectedItem = _tagSearchResults[Random.Range(0, _tagSearchResults.Count)];
            buffer[selectedItem].currentBufferAmount++;

            GameObject tempGameObject = Instantiate(buffer[selectedItem].prefab);
            Transform tempTransform = tempGameObject.transform;

            if (parent) tempTransform.SetParent(parent, true);
            else tempTransform.SetParent(spawnedFromPool.transform, true);

            tempTransform.position = position;
            tempTransform.rotation = angle;
            tempTransform.tag = objectTag;
            tempTransform.name = buffer[selectedItem].prefab.name;

            Enable(tempGameObject);

            return tempGameObject;
        }

        //If we have gotten here either there was no object of the specified type or non were left in the pool with onlyPooled set to true
        return null;
    }

    /// <summary>
    /// Finds and returns a GameObject from the pool by it's name if available with the given parameters.
    /// </summary>
    /// <param name='objectName'>
    /// Name of the object to be searched.
    /// </param>
    /// <param name='onlyPooled'>
    /// Wheter we shuoul create a new object in case we have available free buffer space according to maximumBufferAmount.
    /// </param>
    public GameObject GetObjectForName(string objectName, bool onlyPooled)
    {
        return GetObjectForName(objectName, onlyPooled, Vector3.zero, Quaternion.identity, null);
    }

    /// <summary>
    /// Finds and returns a GameObject from the pool by it's name if available with the given parameters.
    /// </summary>
    /// <param name='objectName'>
    /// Name of the object to be searched.
    /// </param>
    /// <param name='onlyPooled'>
    /// Wheter we shuoul create a new object in case we have available free buffer space according to maximumBufferAmount.
    /// </param>
    /// <param name='position'>
    /// Position to be set to the GameObject before returning.
    /// </param>
    /// <param name='angle'>
    /// Rotation to be set to the GameObject before returning.
    /// </param>
    /// <param name='parent'>
    /// Parent to be set to the GameObject before returning.
    /// </param>
    public GameObject GetObjectForName(string objectName, bool onlyPooled, Vector3 position, Quaternion angle, Transform parent)
    {
        Init();
        int index = -1;
        if (indexObjectsByName)
        {
            index = _objectIndexesByName[objectName];
        }
        else
        {
            bool isFound = false;
            for (index = 0; index < buffer.Length; index++)
            {
                if (buffer[index].prefab.name.Equals(objectName))
                {
                    isFound = true;
                    break;
                }
            }
            if (!isFound) return null;
        }

        if (buffer[index].pooledObjects.Count > 0)
        {
            GameObject tempGameObject = buffer[index].pooledObjects.Pop();
            Transform tempTransform = tempGameObject.transform;
            if (tempTransform.parent == null || (tempTransform.parent && !tempTransform.parent.Equals(parent)))
            {
                if (parent) tempTransform.SetParent(parent, false);
                else tempTransform.SetParent(spawnedFromPool.transform, false);
            }
            tempTransform.position = position;
            tempTransform.rotation = angle;
            Enable(tempGameObject);
            return tempGameObject;
        }

        if (!onlyPooled && buffer[index].currentBufferAmount < buffer[index].maximumBufferAmount)
        {

            buffer[index].currentBufferAmount++;
            GameObject tempGameObject = Instantiate(buffer[index].prefab);
            Transform tempTransform = tempGameObject.transform;

            if (parent) tempTransform.SetParent(parent, true);
            else tempTransform.SetParent(spawnedFromPool.transform, true);

            tempTransform.position = position;
            tempTransform.rotation = angle;
            tempTransform.name = objectName;

            Enable(tempGameObject);

            return tempGameObject;
        }

        //If we have gotten here either there was no object of the specified type or non were left in the pool with onlyPooled set to true
        return null;
    }


    /// <summary>
    /// Returns true if any object is available with the given tag.
    /// </summary>
    /// <param name='objectTag'>
    /// Tag of the object to be checked.
    /// </param>
    public bool IsAnyObjectWithTagAvailable(string objectTag)
    {
        if (indexObjectsByTag)
        {
            if (_objectIndexesByTag.ContainsKey(objectTag))
            {
                for (int i = 0; i < _objectIndexesByTag[objectTag].Count; i++)
                {
                    if (buffer[_objectIndexesByTag[objectTag][i]].pooledObjects.Count > 0) return true;
                }
            }
        }
        else
        {
            for (int index = 0; index < buffer.Length; index++)
            {
                if (buffer[index].prefab.tag.Equals(objectTag))
                {
                    if (buffer[index].pooledObjects.Count > 0) return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if any object is available with the given name.
    /// </summary>
    /// <param name='objectName'>
    /// Name of the object to be checked.
    /// </param>
    public bool IsAnyObjectOfTypeAvailable(string objectName)
    {
        if (indexObjectsByName)
        {
            if (_objectIndexesByName.ContainsKey(objectName))
            {
                return buffer[_objectIndexesByName[objectName]].pooledObjects.Count > 0;
            }
        }
        else
        {
            for (int index = 0; index < buffer.Length; index++)
            {
                if (buffer[index].prefab.name.Equals(objectName))
                {
                    if (buffer[index].pooledObjects.Count > 0)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Pools the object specified and marks it as Spawnable. Only Adds active objects to pool.
    /// Objects that are already in pool will not be added to pool again. Instead they will be disabled.
    /// </summary>
    /// <param name='obj'>
    /// Object to be pooled.
    /// </param>
    public void Recycle(GameObject go)
    {
        if (indexObjectsByName)
        {
            if (!buffer[_objectIndexesByName[go.name]].pooledObjects.Contains(go))
            {
                Disable(go);
                buffer[_objectIndexesByName[go.name]].pooledObjects.Push(go);
            }
            else
            {
                Disable(go);
            }
        }
        else
        {
            for (int index = 0; index < buffer.Length; index++)
            {
                if (buffer[index].prefab.name.Equals(go.name))
                {
                    if (!buffer[index].pooledObjects.Contains(go))
                    {
                        Disable(go);
                        buffer[index].pooledObjects.Push(go);
                    }
                    else
                    {
                        Disable(go);
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Pools the objects specified and marks them as Spawnable. Only Adds active objects to pool.
    /// Objects that are already in pool will not be added to pool again.
    /// </summary>
    /// <param name='objectNames'>
    /// Names of objects to be pooled
    /// </param>
    public void RecycleAllWithName(params string[] objectNames)
    {
        _currentOperations++;
        for (int i = 0; i < objectNames.Length; i++)
        {
            string objectName = objectNames[i];
            while (spawnedFromPool.childCount > 0)
            {
                GameObject go = spawnedFromPool.GetChild(0).gameObject;
                if (go.name == objectName)
                {
                    Recycle(go);
                }
            }
        }
        _currentOperations--;
    }

    /// <summary>
    /// Pools all objects and marks them as Spawnable. Only Adds active objects to pool.
    /// Objects that are already in pool will not be added to pool again.
    /// </summary>
    public void RecycleAll()
    {
        _currentOperations++;
        while (spawnedFromPool.childCount > 0)
        {
            Recycle(spawnedFromPool.GetChild(0).gameObject);
        }
        _currentOperations--;
    }


    /// <summary>
    /// Truncates the excessive items from the Buffer item with the specified Buffer index.
    /// </summary>
    /// <param name='index'>
    /// Buffer index of the Buffer item to be truncated.
    /// </param>
    public void Truncate(int index)
    {
        _currentOperations++;
        while (buffer[index].pooledObjects.Count > buffer[index].bufferAmount)
        {
            buffer[index].pooledObjects.Peek().transform.parent = null;
            Destroy(buffer[index].pooledObjects.Pop());
        }
        buffer[index].currentBufferAmount = buffer[index].bufferAmount;
        _currentOperations--;
    }

    /// <summary>
    /// Truncates the excessive items from all Buffer items in Buffer list.
    /// </summary>
    public void TruncateAll()
    {
        _currentOperations++;
        for (int i = 0; i < buffer.Length; i++)
        {
            Truncate(i);
        }
        RecreateIndexes();
        Resources.UnloadUnusedAssets();
        _currentOperations--;
    }
}