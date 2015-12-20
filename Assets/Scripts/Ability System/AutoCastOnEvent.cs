using UnityEngine;

internal class AutoCastOnEvent : CastAbility
{
    [SerializeField]
    private string _eventName;

    protected override void Init()
    {
        Messenger.AddListener(_eventName, EventCallback);
    }

    private void EventCallback()
    {
        if (gameObject.activeInHierarchy) Cast();
    }
}
