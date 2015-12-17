using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniJSON;
using Settworks.Hexagons;

public class GameManager : Manager
{
    public const string UNIT_SELECTION_CHANGED_EVENT_NAME = "OnUnitSelectionChanged";
    public const string GAME_STATE_CHANGED_EVENT_NAME = "OnGameStateChanged";

    //TODO: make this calculate from grids instead of fixed value.
    [SerializeField, Range(0.15f, 0.40f)]
    private float _maxSpawnArea = 0.15f;

    private GameState _currentGameState;

    private HexGrid _hexGrid;
    private readonly List<HexTile> _cachedOtherHexes = new List<HexTile>();
    private readonly List<HexTile> _cachedMoveHexes = new List<HexTile>();
    private readonly List<Tuple<HexTile, HexTile>> _cachedAttackHexes = new List<Tuple<HexTile, HexTile>>();
    private readonly Queue<Action> _queuedActions = new Queue<Action>();

    private HexTile _selectedHex;
    private HexUnit _selectedUnit;
    private ShortestPathGraphSearch<HexCoord, HexCoord> pathGraphSearch;

    protected HexUnit SelectedUnit
    {
        set
        {
            if (value != _selectedUnit) Messenger.Broadcast(UNIT_SELECTION_CHANGED_EVENT_NAME, value);
            _selectedUnit = value;
        }
    }

    private GameState CurrentGameState
    {
        get { return _currentGameState; }
        set
        {
            if (_currentGameState != value)
            {
                Messenger.Broadcast(GAME_STATE_CHANGED_EVENT_NAME, _currentGameState, value);
            }
            _currentGameState = value;
        }
    }

    void Start()
    {
        _hexGrid = FindObjectOfType<HexGrid>();
        if (_hexGrid == null) throw new Exception("No HexGrid found on the Scene.");
        pathGraphSearch = new ShortestPathGraphSearch<HexCoord, HexCoord>(new HexPathFinding(_hexGrid));

        TurnManager.Instance.Init();

        Messenger.AddListener<HexTile>(HexTile.ON_HEX_CLICKED_EVENT_NAME, OnHexClicked);

        StartCoroutine(ArmyBuild());
    }

    IEnumerator ArmyBuild()
    {
        int armySize = LoadArmyData();
        //Give everyone else time to register to events.
        yield return null;

        CurrentGameState = GameState.ArmyBuild;
        GameHud gameHud = GuiManager.Instance.CurrentState as GameHud;
        for (int i = 0; i < armySize; i++)
        {
            int turn = TurnManager.Instance.CurrentTurn;
            while (turn == TurnManager.Instance.CurrentTurn)
            {
                SelectedUnit = TurnManager.Instance.GetActivePlayer().GetUnit(i);
                _selectedUnit.gameObject.SetActive(true);
                gameHud.ShowPopup("Place Unit", new Vector2(Screen.width / 2f, Screen.height / 2f));
                //Wait until player places the Unit.
                while (_selectedUnit != null)
                {
                    RaycastHit hit;
                    Vector2 rayPos;
                    if (TurnManager.Instance.GetActivePlayer().Id % 2 == 0) rayPos = new Vector2(Mathf.Min(Screen.width * _maxSpawnArea, Input.mousePosition.x), Input.mousePosition.y);
                    else rayPos = new Vector2(Mathf.Max(Screen.width * (1f - _maxSpawnArea), Input.mousePosition.x), Input.mousePosition.y);
                    Ray ray = Camera.main.ScreenPointToRay(rayPos);

                    if (Physics.Raycast(ray, out hit))
                    {
                        _selectedUnit.transform.position = hit.point;
                    }
                    yield return null;
                }
                TurnManager.Instance.EndTurn();
            }
        }

        WaitForSeconds wait = new WaitForSeconds(1f);
        for (int i = 3; i >= 0; i--)
        {
            gameHud.ShowPopup(i == 0 ? "Battle!" : i.ToString(), new Vector2(Screen.width / 2f, Screen.height / 2f));
            yield return wait;
        }

        CurrentGameState = GameState.Battle;
    }

    int LoadArmyData()
    {
        TextAsset unitDataFile = Resources.Load(Path.Combine(CommonPaths.UNIT_DATA_DIR, "Army"), typeof(TextAsset)) as TextAsset;
        if (unitDataFile)
        {
            Dictionary<string, object> rawData = Json.Deserialize(unitDataFile.text) as Dictionary<string, object>;

            for (int i = 0; i < 5; i++)
            {
                string unitKey = UnitCard.UNIT_NAME_KEY + i;
                if (rawData != null && rawData.ContainsKey(unitKey))
                {
                    string unitName = rawData[unitKey] as string;
                    for (int j = 0; j < TurnManager.Instance.PlayerCount; j++)
                    {
                        GameObject newUnit = PoolManager.instance.GetObjectForName(unitName, false);
                        if (j % 2 == 0) newUnit.transform.localScale = new Vector3(1, 1, -1);
                        TurnManager.Instance.GetPlayer(j).AddUnit(newUnit.GetComponent<HexUnit>());
                        newUnit.SetActive(false);
                    }
                }
                else return i;
            }
            return 5;
        }
        return 0;
    }

    public override void Init()
    {

    }

    private void OnHexClickedBuild(HexTile targetHex)
    {
        if (targetHex.IsPassable)
        {
            _selectedUnit.transform.position = targetHex.transform.position;
            targetHex.OccupyingObject = _selectedUnit.gameObject;
            SelectedUnit = null;
        }
    }

    private void OnHexClicked(HexTile targetHex)
    {
        switch (CurrentGameState)
        {
            case GameState.None:
                break;
            case GameState.ArmyBuild:
                OnHexClickedBuild(targetHex);
                break;
            case GameState.Battle:
                OnHexClickedBattle(targetHex);
                break;
            case GameState.GameOver:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// To be called, when a Hex is clicked while the game is in Battle state.
    /// Processes and decides how the input should be handled.
    /// </summary>
    /// <param name="targetHex"></param>
    private void OnHexClickedBattle(HexTile targetHex)
    {
        if (!_selectedUnit)
        {
            SelectHex(targetHex);
            return;
        }

        //Still animating.
        if (_selectedUnit.IsBusy) return;

        //Only allow commanding if the unit is ours.
        if (_selectedUnit.OwnerId != TurnManager.Instance.GetActivePlayer().Id)
        {
            SelectHex(targetHex);
            return;
        }

        if (targetHex.IsOccupied)
        {
            HexUnit occupyingUnit = targetHex.OccupyingObject.GetComponent<HexUnit>();
            if (occupyingUnit && _selectedUnit.HasAttack() && occupyingUnit.OwnerId != TurnManager.Instance.GetActivePlayer().Id)
            {
                Tuple<HexTile, HexTile> attackTouple = new Tuple<HexTile, HexTile>(_selectedHex, targetHex);
                HexUnit targetTileUnit = targetHex.OccupyingObject.GetComponent<HexUnit>();

                //Can we attack directly?
                if (_cachedAttackHexes.Contains(attackTouple))
                {
                    _selectedUnit.Attack(_selectedHex.Coord, targetHex.Coord, _hexGrid, targetTileUnit, OnActionCompletedCallback);
                    return;
                }

                //Look if we can melee attack with movement.
                Tuple<HexTile, HexTile> tuple = _cachedAttackHexes.FirstOrDefault(c => c.second == targetHex);
                if (tuple != null)
                {
                    _queuedActions.Enqueue(() => _selectedUnit.Attack(tuple.first.Coord, targetHex.Coord, _hexGrid, targetTileUnit, OnActionCompletedCallback));
                    List<HexCoord> path = pathGraphSearch.GetShortestPath(_selectedHex.Coord, tuple.first.Coord);
                    if (path != null && path.Count > 0) _selectedUnit.Move(_selectedHex, path, _hexGrid, OnActionCompletedCallback);
                    return;
                }
            }
            SelectHex(targetHex);
        }
        else
        {
            //If we can move there, move.
            if (_cachedMoveHexes.Contains(targetHex))
            {
                List<HexCoord> path = pathGraphSearch.GetShortestPath(_selectedHex.Coord, targetHex.Coord);
                if (path != null && path.Count > 0) _selectedUnit.Move(_selectedHex, path, _hexGrid, OnActionCompletedCallback);
            }
            else SelectHex(targetHex);
        }
    }

    /// <summary>
    /// Called after a Move or Attack command is finished execuing until all the queued messages are processed.
    /// When no queued Actions left, it ends the turn for the Active player and clears player/turn specific data.
    /// </summary>
    private void OnActionCompletedCallback()
    {
        if (_queuedActions.Count > 0)
        {
            _queuedActions.Dequeue().Invoke();
        }
        else
        {
            ResetActiveHexes();
            SelectedUnit = null;
            _selectedHex = null;
            TurnManager.Instance.EndTurn();
        }
    }

    /// <summary>
    /// Selects and Highlights Hex depending on current player and Hex content. For example, shows Movement range or attack range for friendly HexUnits.
    /// </summary>
    /// <param name="toSelectCenter"></param>
    private void SelectHex(HexTile toSelectCenter)
    {
        if (toSelectCenter.OccupyingObject)
        {
            HexUnit occupyingUnit = toSelectCenter.OccupyingObject.GetComponent<HexUnit>();
            SelectedUnit = occupyingUnit;
        }
        else SelectedUnit = null;
        _selectedHex = toSelectCenter;

        HighlightActiveHex();
    }

    /// <summary>
    /// Highlights Hex depending on current player and Hex content. For example, shows Movement range or attack range for friendly HexUnits.
    /// </summary>
    private void HighlightActiveHex()
    {
        if (!_selectedHex)
        {
            return;
        }
        //If hex contains a unit
        if (!_selectedUnit)
        {
            ResetActiveHexes();
            _cachedOtherHexes.Add(_selectedHex);
            _selectedHex.HighlightTile();
        }
        else
        {
            ResetActiveHexes();

            //Mark movement range. Allows occupied hex in the last node.
            foreach (HexTile hexTile in _hexGrid.HexesInReachableRange(_selectedHex.Coord, _selectedUnit.MovementRange, true))
            {
                if (hexTile.IsOccupied)
                {
                    HexUnit hexUnit = hexTile.OccupyingObject.GetComponent<HexUnit>();
                    if (hexUnit == null || hexUnit.OwnerId == _selectedUnit.OwnerId || hexUnit.IsDead) continue;
                    //Melee attackable enemy found.
                    //Find previous move hex.
                    foreach (HexCoord neighbor in hexTile.Coord.Neighbors())
                    {
                        HexTile neighborHexTile = _hexGrid.GetHexTile(neighbor);
                        if (!_cachedMoveHexes.Contains(neighborHexTile)) continue;
                        _cachedAttackHexes.Add(new Tuple<HexTile, HexTile>(neighborHexTile, hexTile));
                        hexTile.HighlightTile(Color.red);
                        break;
                    }
                }
                else
                {
                    //Mark move hex.
                    _cachedMoveHexes.Add(hexTile);
                    if (_selectedUnit.OwnerId == TurnManager.Instance.GetActivePlayer().Id) hexTile.HighlightTile();
                    else hexTile.HighlightTile(Color.blue);
                }
            }

            //TODO: Optimize here!
            //Decide if we can use ranged attack, if not fallback to melee attack.
            Attack attackMode = !_selectedUnit.RangedAttack || _selectedHex.Coord.Neighbors()
                .Any(hex =>
                {
                    HexTile hexTile = _hexGrid.GetHexTile(hex);
                    if (hexTile.OccupyingObject)
                    {
                        HexUnit hexUnit = hexTile.OccupyingObject.GetComponent<HexUnit>();
                        return hexUnit != null && hexUnit.OwnerId != _selectedUnit.OwnerId && !hexUnit.IsDead;
                    }
                    return false;
                }) ? _selectedUnit.MeleeAttack : _selectedUnit.RangedAttack;

            //Mark attackable units
            if (attackMode)
            {
                foreach (
                    HexTile hexTile in
                        _hexGrid.HexesInRange(_selectedHex.Coord, attackMode.MinRange, attackMode.MaxRange)
                            .Where(tile =>
                            {
                                if (tile.OccupyingObject)
                                {
                                    HexUnit hexUnit = tile.OccupyingObject.GetComponent<HexUnit>();
                                    return hexUnit != null && hexUnit.OwnerId != _selectedUnit.OwnerId && !hexUnit.IsDead;
                                }
                                else return false;
                            }))
                {
                    Tuple<HexTile, HexTile> attackTouple = new Tuple<HexTile, HexTile>(_selectedHex, hexTile);
                    if (!_cachedAttackHexes.Contains(attackTouple)) _cachedAttackHexes.Add(attackTouple);
                    hexTile.HighlightTile(Color.red);
                }
            }
        }
    }


    private void ResetActiveHexes()
    {
        foreach (Tuple<HexTile, HexTile> hexTile in _cachedAttackHexes)
        {
            //First will be reset be cachedMoveHexes.
            hexTile.second.AutoSetState();
        }
        _cachedAttackHexes.Clear();
        foreach (HexTile hexTile in _cachedOtherHexes)
        {
            hexTile.AutoSetState();
        }
        _cachedOtherHexes.Clear();
        foreach (HexTile hexTile in _cachedMoveHexes)
        {
            hexTile.AutoSetState();
        }
        _cachedMoveHexes.Clear();
    }
}