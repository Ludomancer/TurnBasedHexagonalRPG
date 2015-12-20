using System;
using UnityEngine;
using UnityEngine.Events;

public class Destructable : MonoBehaviour
{
    #region Fields

    public const string ON_HEALTH_CHANGED = "OnHealthChanged";
    protected int _healthLeft;

    [SerializeField]
    protected UnityEvent _onNeedRemoval;

    [SerializeField]
    protected int _maxHealth;

    protected Transform _transform;

    #endregion

    #region Properties

    public virtual bool IsDead
    {
        get { return _healthLeft <= 0; }
    }

    public virtual int HealthLeft
    {
        get { return _healthLeft; }
        set
        {
            value = Mathf.Clamp(value, 0, MaxHealth);
            if (_healthLeft != value)
            {
                int delta = value - _healthLeft;
                _healthLeft = value;
                Messenger.Broadcast(ON_HEALTH_CHANGED, this, delta);
                if (_healthLeft == 0) _onNeedRemoval.Invoke();
            }
        }
    }

    public int MaxHealth
    {
        get { return _maxHealth; }
    }

    #endregion

    #region Other Members

    private void Awake()
    {
        _transform = transform;
    }

    private void OnEnable()
    {
        Reset();
    }

    public virtual void Reset()
    {
        //Don't trigger event.
        _healthLeft = MaxHealth;
    }

    #endregion
}