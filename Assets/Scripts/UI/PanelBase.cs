using UnityEngine;

public abstract class PanelBase : MonoBehaviour
{
    #region Fields

    public const string ON_TRANSITION_BEGAN_EVENT = "OnTransitionBegan";
    public const string ON_TRANSITION_END_EVENT = "OnTransitionEnd";
    public static readonly int BRING_TRIGGER_HASH = Animator.StringToHash("Bring");
    public static readonly int DISMISS_TRIGGER_HASH = Animator.StringToHash("Dismiss");
    public static readonly int BRANG_TRIGGER_HASH = Animator.StringToHash("Brang");
    public static readonly int DISMISSED_TRIGGER_HASH = Animator.StringToHash("Dismissed");
    private Animator _animator;

    [SerializeField]
    protected bool _canTransitionToSelf;

    private bool _isTransitioning;
    protected bool _needThemeUpdate;
    protected PanelBase _returnAddress;

    #endregion

    #region Properties

    public bool CanTransitionToSelf
    {
        get { return _canTransitionToSelf; }
    }

    public PanelBase ReturnAddress
    {
        get { return _returnAddress; }
        set { _returnAddress = value; }
    }

    public bool IsTransitioning
    {
        get { return _isTransitioning; }

        protected set
        {
            _isTransitioning = value;
            if (value)
            {
                Messenger.Broadcast(ON_TRANSITION_BEGAN_EVENT, this);
            }
            else
            {
                Messenger.Broadcast(ON_TRANSITION_END_EVENT, this);
            }
        }
    }

    public Animator GetAnimator
    {
        get
        {
            if (!_animator) _animator = GetComponent<Animator>();
            return _animator;
        }
    }

    #endregion

    #region Other Members

    public abstract void BringPanel();
    public abstract void DismissPanel();

    /// <summary>
    /// Functions dependent on other Managers.
    /// Any functions that would be dependant by other Managers should be in Awake or Start instead.
    /// </summary>
    public virtual void Init()
    {
    }

    #endregion
}