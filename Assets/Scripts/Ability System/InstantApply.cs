using UnityEngine;

/// <summary>
/// Instantly applies all appliables.
/// </summary>
internal class InstantApply : MonoBehaviour, ICastable
{
    #region ICastable Members

    public void Cast()
    {
        //Apply all effects
        foreach (IAppliable appliable in GetComponents(typeof (IAppliable)))
        {
            appliable.Apply();
        }
    }

    public bool IsBusy()
    {
        return false;
    }

    #endregion
}