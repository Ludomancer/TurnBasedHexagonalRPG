using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Settworks.Hexagons;

public class HexUnit : Destructable
{
    public const string ON_DODGED = "OnDodged";

    public delegate void OnActionFinishedCallback();

    [SerializeField]
    private string _uniqueName;

    [SerializeField]
    private Attack _meleeAttack;

    [SerializeField]
    private Attack _rangedAttack;

    [SerializeField]
    private short _movementRange;

    [SerializeField]
    private int _armor;

    [SerializeField]
    private float _dodgeChange;

    [SerializeField]
    private float _movementSpeed;

    private int _ownerId;

    private bool _isBusy;

    public float DodgeChange
    {
        get { return _dodgeChange; }
    }

    public int Armor
    {
        get { return _armor; }
    }

    public short MovementRange
    {
        get { return _movementRange; }
    }

    public Attack RangedAttack
    {
        get { return _rangedAttack; }
    }

    public Attack MeleeAttack
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

    public bool HasAttack()
    {
        return RangedAttack || MeleeAttack;
    }

    public void Move(HexTile selfHexTile, List<HexCoord> path, HexGrid hexGrid, OnActionFinishedCallback callback)
    {
        StartCoroutine(MoveRoutine(selfHexTile, path, hexGrid, callback));
    }

    IEnumerator MoveRoutine(HexTile selfHexTile, List<HexCoord> path, HexGrid hexGrid, OnActionFinishedCallback callback)
    {
        _isBusy = true;
        foreach (HexCoord node in path)
        {
            float progress = Time.deltaTime * _movementSpeed;
            Vector3 startPos = _transform.position;
            Vector3 endPos = hexGrid.GetHexWorldPosition(node);
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
        if (callback != null) callback();
    }

    public void Attack(HexCoord attackerCoord, HexCoord targetCoord, HexGrid hexGrid, HexUnit targetUnit, OnActionFinishedCallback callback)
    {
        int hexDist = HexCoord.Distance(attackerCoord, targetCoord);
        if (!IsInRange(RangedAttack, hexDist))
        {
            if (IsInRange(MeleeAttack, hexDist)) StartCoroutine(AttackRoutine(attackerCoord, targetCoord, hexGrid, targetUnit, MeleeAttack, callback));
        }
        else
        {
            StartCoroutine(AttackRoutine(attackerCoord, targetCoord, hexGrid, targetUnit, RangedAttack, callback));
        }
    }

    public bool IsInRange(Attack attackMode, float range)
    {
        if (attackMode == null) return false;
        return attackMode.MaxRange >= range && range >= attackMode.MinRange;
    }

    IEnumerator AttackRoutine(HexCoord attackerCoord, HexCoord targetCoord, HexGrid hexGrid, HexUnit targetUnit, Attack attackMode, OnActionFinishedCallback callback)
    {
        _isBusy = true;
        Vector3 startPos = hexGrid.GetHexWorldPosition(attackerCoord);
        Vector3 endPos = hexGrid.GetHexWorldPosition(targetCoord);
        float progress = Time.deltaTime * _movementSpeed;
        bool isAttacked = false;
        while (progress < 1)
        {
            _transform.position = Vector3.Lerp(startPos, endPos, progress < 0.5f ? progress : 0.5f - (progress - 0.5f));
            yield return null;
            progress += Time.deltaTime * _movementSpeed;
            if (progress >= 0.5f && !isAttacked)
            {
                isAttacked = true;
                if (Random.value < DodgeChange)
                {
                    Debug.Log("Dodged");
                    Messenger.Broadcast(ON_DODGED, this);
                }
                else
                {
                    Debug.Log("Attacked");
                    if (attackMode.IgnoreArmor) targetUnit.HealthLeft -= attackMode.Strength;
                    else targetUnit.HealthLeft -= Mathf.Max(1, targetUnit.Armor - attackMode.Strength);
                }
            }
        }
        _transform.position = startPos;
        _isBusy = false;
        if (callback != null) callback();
    }
}
