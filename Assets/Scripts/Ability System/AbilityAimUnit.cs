using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Selects clicked unit as target.
/// </summary>
internal class AbilityAimUnit : AbilityAim
{
    #region Fields

    [SerializeField]
    private bool _canTargetSelf;

    [SerializeField]
    private int _rangeMax;

    [SerializeField]
    private int _rangeMin;

    [SerializeField]
    private bool _targetEnemy;

    public LayerMask layerMask;

    #endregion

    #region Properties

    public int RangeMin
    {
        get { return _rangeMin; }
    }

    public int RangeMax
    {
        get { return _rangeMax; }
    }

    #endregion

    #region Other Members

    protected override void Init()
    {
        Messenger.AddListener<HexTile>(HexTile.ON_HEX_CLICKED_EVENT_NAME, OnHexClicked);
    }

    private void OnHexClicked(HexTile hexTile)
    {
        if (gameObject.activeInHierarchy && IsTargetValid(hexTile))
        {
            _ability.Target = hexTile.OccupyingObject;
        }
    }

    public override List<HexTile> GetAvailableHexes(HexGrid hexGrid)
    {
        List<HexTile> targetList = new List<HexTile>();
        if (_ability.CanCastSurrounded || !_ability.Owner.IsSurrounded(hexGrid))
        {
            HexTile casterHex = _ability.Owner.GetHexTile(hexGrid);
            if (_canTargetSelf) targetList.Add(casterHex);
            foreach (HexTile targetHex in hexGrid.HexesInRange(casterHex.Coord, RangeMin, RangeMax))
            {
                if (IsTargetValid(targetHex))
                    if (!targetList.Contains(targetHex)) targetList.Add(targetHex);
            }
        }
        return targetList;
    }

    private bool IsTargetValid(HexTile targetHex)
    {
        if (targetHex.OccupyingObject)
        {
            HexUnit targetUnit = targetHex.OccupyingObject.GetComponent<HexUnit>();
            if (targetUnit.IsDead) return false;
            if (_canTargetSelf && targetUnit == _ability.Owner) return true;
            if (targetUnit.OwnerId != _ability.Owner.OwnerId)
            {
                return _targetEnemy;
            }
            return !_targetEnemy;
        }
        return false;
    }

    #endregion
}