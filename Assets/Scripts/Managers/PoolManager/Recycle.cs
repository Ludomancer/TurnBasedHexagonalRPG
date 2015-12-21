using UnityEngine;

internal class Recycle : MonoBehaviour, IRecycle
{
    #region Delegates

    public delegate void OnObjectRecycled(GameObject go);

    public delegate void OnRecycling(GameObject go);

    #endregion

    #region Fields

    public OnObjectRecycled onObjectRecycledCallback;
    public OnRecycling onRecyclingCallback;

    #endregion

    #region Other Members

    protected virtual void RecycleInternal()
    {
        if (onRecyclingCallback != null) onRecyclingCallback.Invoke(gameObject);
        PoolManager.instance.Recycle(gameObject);
        if (onObjectRecycledCallback != null) onObjectRecycledCallback.Invoke(gameObject);
    }

    #endregion

    #region IRecycle Members

    public virtual void RecycleObject()
    {
        RecycleInternal();
    }

    #endregion
}