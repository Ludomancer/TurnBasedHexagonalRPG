using System.Collections;
using UnityEngine;

internal class RecycleTimed : Recycle
{
    #region Fields

    public float recycleDelay;
    public bool startOnEnable = false;

    #endregion

    #region Other Members

    private void OnEnable()
    {
        if (startOnEnable) RecycleObject();
    }

    public override void RecycleObject()
    {
        StartCoroutine(RecycleRoutine());
    }

    private IEnumerator RecycleRoutine()
    {
        yield return new WaitForSeconds(recycleDelay);
        RecycleInternal();
    }

    #endregion
}