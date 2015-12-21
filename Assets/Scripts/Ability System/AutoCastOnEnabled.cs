using UnityEngine;

/// <summary>
/// Automatically casts the abilit when the obbject is activated.
/// </summary>
internal class AutoCastOnEnabled : CastAbility
{
    #region Other Members

    private void OnEnable()
    {
        Debug.Log("Cast");
        Cast();
    }

    #endregion
}