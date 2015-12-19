using UnityEngine;

public class Destructable : MonoBehaviour
{
    #region Fields

    public const string ON_HEALTH_CHANGED = "OnHealthChanged";
    protected int _healthLeft;

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