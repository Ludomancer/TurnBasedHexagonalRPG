using System.Collections.Generic;

/// <summary>
/// Automatically selects Active player as target.
/// </summary>
internal class AbilityAimPlayer : AbilityAim
{
    #region Other Members

    private void OnEnable()
    {
        if (_ability.IsInitialized)
        {
            _ability.Target = TurnManager.Instance.GetActivePlayer().gameObject;
        }
    }

    public override List<HexTile> GetAvailableHexes(HexGrid hexGrid)
    {
        return null;
    }

    #endregion
}