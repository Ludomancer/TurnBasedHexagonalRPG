using UnityEngine;

/// <summary>
/// Automatically casts ability when a certain Messenger event is triggered. Requires Messenger class.
/// </summary>
internal class AutoCastOnEvent : CastAbility
{
    #region Fields

    [SerializeField]
    private string _eventName;

    #endregion

    #region Other Members

    protected override void Init()
    {
        Messenger.AddListener(_eventName, EventCallback);
    }

    private void EventCallback()
    {
        if (gameObject.activeInHierarchy) Cast();
    }

    #endregion
}