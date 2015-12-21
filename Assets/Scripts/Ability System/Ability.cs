using UnityEngine;

/// <summary>
/// Basic class for all abilities. Does not do anything by itself.
/// </summary>
[RequireComponent(typeof (CastAbility))]
public class Ability : MonoBehaviour
{
    #region Fields

    public const string ON_ABILITY_ACTIVATED = "OnAbilityActivated";
    public const string ON_ABILITY_DEACTIVATED = "OnAbilityDeactivated";
    public const string ON_CAST_COMPLETED = "OnCastCompleted";

    [SerializeField]
    private bool _canCastSurrounded;

    [SerializeField]
    private bool _disableAfterInit = true;

    private bool _isInitialized;

    [SerializeField]
    private bool _isPassive = false;

    [SerializeField]
    private bool _isSubSkill;

    [SerializeField]
    private int _manaCost;

    [SerializeField]
    private HexUnit _owner;

    [SerializeField]
    private string _skillName;

    [SerializeField]
    private GameObject _target;

    #endregion

    #region Properties

    public GameObject Target
    {
        get { return _target; }
        set
        {
            if (value != _target)
            {
                _target = value;
                if (gameObject.activeInHierarchy)
                {
                    //Use interfaces instead of send message for type safety.
                    foreach (IOnTargetSelected onTargetSelected in GetComponents(typeof (IOnTargetSelected)))
                    {
                        onTargetSelected.OnTargetSelectionChanged(_target);
                    }
                }
            }
        }
    }

    public HexUnit Owner
    {
        get { return _owner; }
    }

    public bool CanCastSurrounded
    {
        get { return _canCastSurrounded; }
    }

    public bool IsInitialized
    {
        get { return _isInitialized; }
    }

    public string SkillName
    {
        get { return _skillName; }
    }

    public int ManaCost
    {
        get { return _manaCost; }
    }

    public bool IsBusy
    {
        get { return GetComponent<CastAbility>().IsBusy; }
    }

    public bool IsSubSkill
    {
        get { return _isSubSkill; }
    }

    public bool IsPassive
    {
        get { return _isPassive; }
    }

    #endregion

    #region Other Members

    private void Awake()
    {
        if (Owner == null)
            _owner = GetComponentInParent<HexUnit>();
    }

    private void Start()
    {
        Init();
    }

    protected virtual void Init()
    {
        if (_disableAfterInit) gameObject.SetActive(false);
        _isInitialized = true;
    }

    public void AbilityCastCompleted(bool isSuccess)
    {
        if (!_isSubSkill)
            Messenger.Broadcast(ON_CAST_COMPLETED, (Component)this, isSuccess);
        if (!_isPassive) DeactivatInternal();
    }

    public void Activate()
    {
        _target = null;
        gameObject.SetActive(true);
        Messenger.Broadcast(ON_ABILITY_ACTIVATED, this);
    }

    public void ActivateAutoCast(GameObject target)
    {
        gameObject.SetActive(true);
        _target = target;
        if (_target)
        {
            GetComponent<CastAbility>().Cast();
            if (!_isSubSkill)
                Messenger.Broadcast(ON_ABILITY_ACTIVATED, this);
        }
        else AbilityCastCompleted(false);
    }

    public void Deactivate()
    {
        if (!IsBusy)
        {
            DeactivatInternal();
            if (!_isSubSkill)
                Messenger.Broadcast(ON_ABILITY_DEACTIVATED, this);
        }
        else Debug.LogWarning("Can not deactivate a skill while it is busy!");
    }

    private void DeactivatInternal()
    {
        gameObject.SetActive(false);
    }

    #endregion
}