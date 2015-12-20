using UnityEngine;

class Recycle : MonoBehaviour, IRecycle
{
    public delegate void OnObjectRecycled(GameObject go);
    public OnObjectRecycled onObjectRecycledCallback;

    public delegate void OnRecycling(GameObject go);
    public OnRecycling onRecyclingCallback;

    public virtual void RecycleObject()
    {
        RecycleInternal();
    }

    protected virtual void RecycleInternal()
    {
        if (onRecyclingCallback != null) onRecyclingCallback.Invoke(gameObject);
        PoolManager.instance.Recycle(gameObject);
        if (onObjectRecycledCallback != null) onObjectRecycledCallback.Invoke(gameObject);
    }
}
