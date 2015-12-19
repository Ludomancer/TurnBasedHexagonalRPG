using UnityEngine;

internal class ClickToCast : CastAbility
{
    #region Other Members

    private void LateUpdate()
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

    #endregion
}