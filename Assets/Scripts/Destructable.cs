using UnityEngine;
using System.Collections;

public class Destructable : MonoBehaviour
{
    public const string ON_HEALTH_CHANGED = "OnHealthChanged";

    [SerializeField]
    protected int _maxHealth;

    protected int _healthLeft;

    protected Transform _transform;

    void Awake()
    {
        _transform = transform;
    }

    void OnEnable()
    {
        Reset();
    }

    public virtual bool IsDead
    {
        get { return _healthLeft <= 0; }
    }

    public virtual int HealthLeft
    {
        get { return _healthLeft; }
        set
        {
            if (value > MaxHealth) value = MaxHealth;
            if (_healthLeft != value)
            {
                Messenger.Broadcast(ON_HEALTH_CHANGED, this, value - _healthLeft);
                _healthLeft = value;
            }
        }
    }

    public int MaxHealth
    {
        get { return _maxHealth; }
    }

    public virtual void Reset()
    {
        //Don't trigger event.
        _healthLeft = MaxHealth;
    }
}
