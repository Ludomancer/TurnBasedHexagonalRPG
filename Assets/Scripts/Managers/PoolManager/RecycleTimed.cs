using System.Collections;
using UnityEngine;

class RecycleTimed : Recycle
{
    public bool startOnEnable = false;
    public float recycleDelay;

    void OnEnable()
    {
        if (startOnEnable) RecycleObject();
    }

    public override void RecycleObject()
    {
        StartCoroutine(RecycleRoutine());
    }

    IEnumerator RecycleRoutine()
    {
        yield return new WaitForSeconds(recycleDelay);
        RecycleInternal();
    }  
}
