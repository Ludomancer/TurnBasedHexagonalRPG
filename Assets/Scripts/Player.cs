using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region Fields
    public const string ON_UNIT_ADDED = "OnUnitAdded";
    public const string ON_UNIT_REMOVED = "OnUnitRemoved";

    private int _id = -1;
    private readonly ListQueue<HexUnit> _units = new ListQueue<HexUnit>();

    #endregion

    #region Properties

    public int Id
    {
        get { return _id; }
    }

    public int UnitCount
    {
        get { return _units.Count; }
    }

    #endregion

    #region Other Members

    // Use this for initialization
    public void Initialize(int id)
    {
        _id = id;
    }

    public void AddUnit(HexUnit newUnit)
    {
        if (newUnit == null) throw new NullReferenceException("newUnit is null!");
        newUnit.OwnerId = Id;
        newUnit.GetComponent<Recycle>().onRecyclingCallback += RemoveUnit;
        _units.Enqueue(newUnit);
        Messenger.Broadcast(ON_UNIT_ADDED, newUnit);
    }

    private void RemoveUnit(GameObject go)
    {
        HexUnit hexUnit = go.GetComponent<HexUnit>();
        hexUnit.OwnerId = -1;
        _units.Remove(hexUnit);
        hexUnit.GetComponent<Recycle>().onRecyclingCallback -= RemoveUnit;
        Messenger.Broadcast(ON_UNIT_REMOVED, go);
    }

    public HexUnit PeekUnit(int i)
    {
        return _units[i];
    }

    public HexUnit GetNextUnit()
    {
        if (_units.Count == 0) return null;
        HexUnit hexUnit = _units.Dequeue();

        //Unit might be dead waiting to be pooled.
        if (!hexUnit.IsDead)
        {
            _units.Enqueue(hexUnit);

            //Process after enqueue to prevent false positive gameover in armies as small as 1 unit.
            hexUnit.SelectSkill(-1);
            hexUnit.ToggleWeapons(true);
            hexUnit.BeginTurn();
        }
        return hexUnit;
    }

    public void RemoveAllUnits()
    {
        for (int i = 0; i < _units.Count; i++)
        {
            RemoveUnit(_units[i].gameObject);
        }
    }

    public bool IsAnyUnitAlive()
    {
        for (int u = 0; u < UnitCount; u++)
        {
            HexUnit tempUnit = PeekUnit(u);
            if (!tempUnit || tempUnit.IsDead)
            {
                //Player is out of units if unit is dead or not found and UnitCount is less than 2
                if (UnitCount < 2)
                {
                    //Check if other player has any units left, it might be draw
                    return false;
                }
            }
        }
        return true;
    }

    public override string ToString()
    {
        return "Player " + (Id + 1);
    }

    #endregion
}