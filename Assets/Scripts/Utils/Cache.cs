using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets
{
    public class Cache<T> : IEnumerable<KeyValuePair<int, List<T>>>
    {
        #region Fields

        private readonly Dictionary<int, List<T>> _cache = new Dictionary<int, List<T>>();

        #endregion

        #region Other Members

        /// <summary>
        /// Adds given item to given cache id.
        /// </summary>
        /// <param name="cacheId">Unique SubCache Id.</param>
        /// <param name="objectToCache">Object to be Cached.</param>
        /// <param name="allowDuplicate">If set to false, Caches can not contain duplicate items.</param>
        public void Add(int cacheId, T objectToCache, bool allowDuplicate = false)
        {
            if (!_cache.ContainsKey(cacheId))
            {
                _cache.Add(cacheId, new List<T> {objectToCache});
            }
            else
            {
                if (allowDuplicate || !_cache[cacheId].Contains(objectToCache)) _cache[cacheId].Add(objectToCache);
            }
        }

        /// <summary>
        /// Remove subcache from Cache.
        /// </summary>
        /// <param name="cacheId">SubCache id.</param>
        public void RemoveCache(int cacheId)
        {
            _cache.Remove(cacheId);
        }

        /// <summary>
        /// Removes given item from given SubCache if SubCache and Item are found.
        /// </summary>
        /// <param name="cacheId">SubCache id.</param>
        /// <param name="objectToRemove">Object to be removed.</param>
        public void Remove(int cacheId, T objectToRemove)
        {
            if (_cache.ContainsKey(cacheId)) _cache[cacheId].Remove(objectToRemove);
        }

        /// <summary>
        /// Remove given item from alll SubCaches.
        /// </summary>
        /// <param name="objectToRemove">Object to be removed.</param>
        public void Remove(T objectToRemove)
        {
            foreach (KeyValuePair<int, List<T>> subCache in _cache)
            {
                subCache.Value.Remove(objectToRemove);
            }
        }

        /// <summary>
        /// Checks if object exists in given cache id.
        /// </summary>
        /// <param name="cacheId">SubCache Id</param>
        /// <param name="objectToCheck">Object to Check.</param>
        /// <returns></returns>
        public bool Contains(int cacheId, T objectToCheck)
        {
            return _cache.ContainsKey(cacheId) && _cache[cacheId].Contains(objectToCheck);
        }

        /// <summary>
        /// Checks if the object exists in any of the caches.
        /// </summary>
        /// <param name="objectToCheck">Object to Check.</param>
        /// <returns></returns>
        public bool Contains(T objectToCheck)
        {
            return _cache.Any(c => c.Value.Contains(objectToCheck));
        }

        /// <summary>
        /// Returns the whole cache.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, List<T>> GetCache()
        {
            return _cache;
        }

        /// <summary>
        /// Return given sub cache. Returns null if not found.
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        public List<T> GetSubCache(int cacheId)
        {
            return _cache.ContainsKey(cacheId) ? _cache[cacheId] : null;
        }

        /// <summary>
        /// Clear all items in cache.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Clears sub cache.
        /// Throws exception if given id is not found.
        /// </summary>
        /// <param name="cacheId"></param>
        public void ClearSubCache(int cacheId)
        {
            _cache[cacheId].Clear();
        }

        #endregion

        #region IEnumerable<KeyValuePair<int,List<T>>> Members

        public IEnumerator<KeyValuePair<int, List<T>>> GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}