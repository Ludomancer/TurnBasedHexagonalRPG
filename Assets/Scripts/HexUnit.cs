using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Settworks.Hexagons;
using UnityEngine;

public class HexUnit : Destructable
{
    #region Fields

    public const string ON_DODGED = "OnDodged";
    public const string ON_MOVE_COMPLETED = "OnMoveCompleted";
    public const string ON_MANA_CHANGED = "OnManaChanged";
    private int _activeSkillIndex = -1;
    private int _manaLeft;

    [SerializeField]
    private string _unitName;

    [SerializeField]
    private int _armor;

    [SerializeField]
    private float _dodgeChance;

    private bool _isBusy;

    [SerializeField]
    private Ability _meleeAttack;

    [SerializeField]
    private short _movementRange;

    [SerializeField]
    private float _movementSpeed;

    [SerializeField]
    private int _maxMana;

    [SerializeField]
    private int _ownerId;

    [SerializeField]
    private Ability _rangedAttack;

    [SerializeField]
    private Ability[] _skills;

    [SerializeField]
    private string _uniqueName;

    #endregion

    #region Properties

    public float DodgeChance
    {
        get { return _dodgeChance; }
    }

    public int Armor
    {
        get { return _armor; }
    }

    public short MovementRange
    {
        get { return _movementRange; }
    }

    public Ability MeleeAttack
    {
        get { return _meleeAttack; }
    }

    public int OwnerId
    {
        get { return _ownerId; }
        set { _ownerId = value; }
    }

    public string UniqueName
    {
        get { return _uniqueName; }
    }

    public bool IsBusy
    {
        get { return _isBusy; }
    }

    public bool IsAnySkillBusy
    {
        get
        {
            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i].IsBusy) return true;
            }
            return ((_meleeAttack && _meleeAttack.IsBusy) || (_rangedAttack && _rangedAttack.IsBusy));
        }
    }

    public Ability RangedAttack
    {
        get { return _rangedAttack; }
    }

    public Ability[] Skills
    {
        get { return _skills; }
    }

    public int MaxMana
    {
        get { return _maxMana; }
    }

    public int ManaLeft
    {
        get { return _manaLeft; }
        set
        {
            value = Mathf.Clamp(value, 0, MaxMana);
            if (_manaLeft != value)
            {
                _manaLeft = value;
                Messenger.Broadcast(ON_MANA_CHANGED, this, value - _manaLeft);
            }
        }
    }

    #endregion

    #region Other Members

    private void Awake()
    {
        if (Skills.Length > 8) throw new Exception("A unit can not have more than 8 skills.");
        if (_meleeAttack)
        {
#if UNITY_EDITOR
            AbilityAimUnit meleeAim = _meleeAttack.GetComponent<AbilityAimUnit>();
            if (!meleeAim) throw new Exception("MeleeAttack should have an AbilityAimUnit component.");
            if (meleeAim.RangeMin != 0)
            {
                throw new Exception("MeleeAttack RangeMin must be 0.");
            }
            if (meleeAim.RangeMax != 1)
            {
                throw new Exception("MeleeAttack RangeMax must be 1.");
            }
#endif
        }
        _transform = transform;
    }

    private void Start()
    {
        ToggleWeapons(false);
    }

    public void ToggleWeapons(bool state)
    {
        if (_meleeAttack)
            _meleeAttack.gameObject.SetActive(state);
        if (_rangedAttack)
            _rangedAttack.gameObject.SetActive(state);
    }

    public void BeginTurn()
    {
        if (gameObject.activeInHierarchy)
        {
            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i].IsPassive)
                {
                    CastOnBeginTurn cobt = _skills[i].GetComponent<CastOnBeginTurn>();
                    if (cobt)
                    {
                        _skills[i].Activate();
                        _skills[i].Target = gameObject;
                        cobt.Cast();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Selects skill at given index.
    /// </summary>
    /// <param name="i">Skill index. -1 to deselect.</param>
    public void SelectSkill(int i)
    {
        if (i == _activeSkillIndex) return;
        if (IsBusy || IsAnySkillBusy) return;

        Ability activeAbility = ActiveSkill();
        if (i == -1)
        {
            _activeSkillIndex = i;
        }
        else if (i >= 0 && i < _skills.Length)
        {
            _activeSkillIndex = i;
            _skills[_activeSkillIndex].Activate();
        }
        if (activeAbility) activeAbility.Deactivate();
    }

    public Ability ActiveSkill()
    {
        return Skills.Length == 0 || _activeSkillIndex == -1 ? null : Skills[_activeSkillIndex];
    }

    /// <summary>
    /// Calculates HexTile of unit by given HexGrid.
    /// </summary>
    /// <param name="hexGrid"></param>
    /// <returns>HexTile of unit.</returns>
    public HexTile GetHexTile(HexGrid hexGrid)
    {
        return hexGrid.GetHexTile(_transform.position);
    }

    public void FlipFacing()
    {
        _transform.localScale = new Vector3(_transform.localScale.x, _transform.localScale.y, -_transform.localScale.z);
    }

    public void FaceRight()
    {
        _transform.localScale = new Vector3(_transform.localScale.x, _transform.localScale.y,
            -Mathf.Abs(_transform.localScale.z));
    }

    public void FaceLeft()
    {
        _transform.localScale = new Vector3(_transform.localScale.x, _transform.localScale.y,
            Mathf.Abs(-_transform.localScale.z));
    }

    public void Move(HexTile selfHexTile, List<HexCoord> path, HexGrid hexGrid)
    {
        StartCoroutine(MoveRoutine(selfHexTile, path, hexGrid));
    }

    public bool IsSurrounded(HexGrid hexGrid)
    {
        HexTile hexTile = hexGrid.GetHexTile(_transform.position);
        return hexTile.Coord.Neighbors()
            .Any(hex =>
            {
                if (hexGrid.IsCordinateValid(hex))
                {
                    HexTile neighborHexTile = hexGrid.GetHexTile(hex);
                    if (neighborHexTile.OccupyingObject)
                    {
                        HexUnit hexUnit = neighborHexTile.OccupyingObject.GetComponent<HexUnit>();
                        return hexUnit != null && hexUnit.OwnerId != OwnerId && !hexUnit.IsDead;
                    }
                }
                return false;
            });
    }

    private IEnumerator MoveRoutine(HexTile selfHexTile, List<HexCoord> path, HexGrid hexGrid)
    {
        _isBusy = true;
        foreach (HexCoord node in path)
        {
            float progress = Time.deltaTime * _movementSpeed;
            Vector3 startPos = _transform.position;
            Vector3 endPos = hexGrid.GetWorldPositionOfHex(node);
            while (progress < 1)
            {
                _transform.position = Vector3.Lerp(startPos, endPos, progress);
                yield return null;
                progress += Time.deltaTime * _movementSpeed;
            }
            _transform.position = endPos;
        }
        hexGrid.GetHexTile(path[path.Count - 1]).OccupyingObject = gameObject;
        selfHexTile.OccupyingObject = null;
        _isBusy = false;
        Messenger.Broadcast(ON_MOVE_COMPLETED, (Component)this, true);
    }

    public override void Reset()
    {
        _healthLeft = MaxHealth;
        _manaLeft = MaxMana;
    }

    public override int HealthLeft
    {
        get { return _healthLeft; }
        set
        {
            value = Mathf.Clamp(value, 0, MaxHealth);
            if (_healthLeft != value)
            {
                int delta = value - _healthLeft;
                _healthLeft = value;
                Messenger.Broadcast(ON_HEALTH_CHANGED, (Destructable)this, delta);
                if (_healthLeft == 0) _onNeedRemoval.Invoke();
            }
        }
    }

    public string UnitName
    {
        get { return _unitName; }
    }

    #endregion
}