using UnityEngine;

/// <summary>
/// Called when a target is selected.
/// </summary>
internal interface IOnTargetSelected
{
    #region Other Members

    void OnTargetSelectionChanged(GameObject target);

    #endregion
}