using UnityEngine;

public abstract class Manager : MonoBehaviour
{
    #region Other Members

    /// <summary>
    /// Functions dependent on other Managers.
    /// Any functions that would be dependant by other Managers should be in Awake or Start instead.
    /// </summary>
    public abstract void Init();

    #endregion
}