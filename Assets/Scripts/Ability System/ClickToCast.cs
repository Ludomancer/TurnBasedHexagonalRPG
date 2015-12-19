using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.iOS;

class ClickToCast : CastAbility
{
    void LateUpdate()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Cast();
        }
#else
        if (Input.touchCount > 0)
        {
            if (Input.touches[0].phase == TouchPhase.Began)
            {
                UnityEngine.Debug.Log("Cast");
                Cast();
            }
        }
#endif
    }
}
