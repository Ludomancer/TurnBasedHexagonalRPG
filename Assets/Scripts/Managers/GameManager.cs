using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using MiniJSON;
using Settworks.Hexagons;

public class GameManager : Manager
{
    public const string UNIT_SELECTION_CHANGED_EVENT_NAME = "OnUnitSelectionChanged";
    public const string GAME_STATE_CHANGED_EVENT_NAME = "OnGameStateChanged";

    [SerializeField, Range(1, 3)]
    private int _maxSpawnDistance = 2;

    [SerializeField, Range(1, 10)]
    private int _maxArmySize = 5;

    private GameState _currentGameState;

    private HexGrid _hexGrid;
    private readonly List<HexTile> _cachedOtherHexes = new List<HexTile>();
    private readonly List<HexTile> _cachedMoves = new List<HexTile>();
    private readonly List<HexTile> _cachedDirectActions = new List<HexTile>();
    private readonly List<HexTile> _cachedDirectAttacks = new List<HexTile>();
    private readonly List<Tuple<HexTile, HexTile>> _cachedMoveActions = new List<Tuple<HexTile, HexTile>>();
    private readonly Queue<Action> _queuedActions = new Queue<Action>();

    private HexTile _selectedHex;
    //The unit selected by player, can not be commanded.
    private HexUnit _selectedUnit;
    //The unit to be played this turn.
    private HexUnit _activeUnit;
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
        Messenger.AddListener<Ability>(Ability.ON_ABILITY_ACTIVATED, OnAbilityActivated);
        Messenger.AddListener<Ability>(Ability.ON_ABILITY_DEACTIVATED, OnAbilityDeactivated);
        Messenger.AddListener<Component, bool>(Ability.ON_CAST_COMPLETED, OnActionCompletedCallback);
        Messenger.AddListener<Component, bool>(HexUnit.ON_MOVE_COMPLETED, OnActionCompletedCallback);

        StartCoroutine(ArmyBuild());
    }

    private void OnAbilityDeactivated(Ability ability)
    {
        //Only do that if no active skills selected. Otherwise OnAbilityActivated will handle it.
        if (!_activeUnit.ActiveSkill()) HighlightActiveHex();
    }

    private void OnAbilityActivated(Ability ability)
    {
        HighlightActiveHex();
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
            Vector2 topLeftHex = Camera.main.WorldToScreenPoint(_hexGrid.GetWorldPositionOfHex(_hexGrid.GetHexTileDirect(0, 0).Coord));
            Vector2 bottomRightHex = Camera.main.WorldToScreenPoint(_hexGrid.GetWorldPositionOfHex(_hexGrid.GetHexTileDirect(_hexGrid.WidthInHexes - 1, _hexGrid.HeightInHexes - 1).Coord));
            float yMax = topLeftHex.y;
            float yMin = bottomRightHex.y;
            float xMax = bottomRightHex.x;
            float xMin = topLeftHex.x;
            while (turn == TurnManager.Instance.CurrentTurn)
            {
                SelectedUnit = TurnManager.Instance.GetActivePlayer().GetNextUnit();
                _selectedUnit.gameObject.SetActive(true);
                gameHud.ShowPopup("Place Unit", new Vector2(0.5f, 0.5f), Color.white);

                Vector3 refPosition;
                Func<float, float, float> limitFunction;
                if (TurnManager.Instance.GetActivePlayer().Id % 2 == 0)
                {
                    refPosition = _hexGrid.GetWorldPositionOfHex(_hexGrid.GetHexTileDirect((int)(_hexGrid.WidthInHexes / 2f), _maxSpawnDistance - 1).Coord);
                    limitFunction = Mathf.Min;
                }
                else
                {
                    refPosition = _hexGrid.GetWorldPositionOfHex(_hexGrid.GetHexTileDirect((int)(_hexGrid.WidthInHexes / 2f), _hexGrid.HeightInHexes - _maxSpawnDistance).Coord);
                    limitFunction = Mathf.Max;
                }
                float xPlayerLimit = Camera.main.WorldToScreenPoint(refPosition).x;

                Transform selectedUnitTransform = _selectedUnit.transform;
                selectedUnitTransform.position = refPosition;

                //Wait until player places the Unit.
                while (_selectedUnit != null)
                {
                    Vector2 rayPos = new Vector2(limitFunction(xPlayerLimit, Mathf.Clamp(Input.mousePosition.x, xMin, xMax)), Mathf.Clamp(Input.mousePosition.y, yMin, yMax));
                    Ray ray = Camera.main.ScreenPointToRay(rayPos);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        selectedUnitTransform.position = hit.point;
                    }
                    yield return null;
                }
                TurnManager.Instance.EndTurn();
            }
        }

        WaitForSeconds wait = new WaitForSeconds(1f);
        for (int i = 3; i >= 0; i--)
        {
            gameHud.ShowPopup(i == 0 ? "Battle!" : i.ToString(), new Vector2(0.5f, 0.5f), Color.white);
            yield return wait;
        }

        CurrentGameState = GameState.Battle;

        _activeUnit = TurnManager.Instance.GetActivePlayer().GetNextUnit();
        _selectedHex = _activeUnit.GetHexTile(_hexGrid);
        SelectHex(_selectedHex);
    }

    int LoadArmyData()
    {
        TextAsset unitDataFile = Resources.Load(Path.Combine(CommonPaths.UNIT_DATA_DIR, "Army"), typeof(TextAsset)) as TextAsset;
        if (unitDataFile)
        {
            Dictionary<string, object> rawData = Json.Deserialize(unitDataFile.text) as Dictionary<string, object>;

            for (int i = 0; i < _maxArmySize; i++)
            {
                string unitKey = UnitCard.UNIT_NAME_KEY + i;
                if (rawData != null && rawData.ContainsKey(unitKey))
                {
                    string unitName = rawData[unitKey] as string;
                    for (int j = 0; j < TurnManager.Instance.PlayerCount; j++)
                    {
                        GameObject newUnit = PoolManager.instance.GetObjectForName(unitName, false);
                        HexUnit hexUnit = newUnit.GetComponent<HexUnit>();
                        if (j % 2 == 0) hexUnit.FaceRight();
                        else hexUnit.FaceLeft();
                        TurnManager.Instance.GetPlayer(j).Enqueue(hexUnit);
                        newUnit.SetActive(false);
                    }
                }
                else return i;
            }
            return _maxArmySize;
        }
        return -1;
    }

    public override void Init()
    {

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

    private void OnHexClickedBuild(HexTile targetHex)
    {
        if (TurnManager.Instance.GetActivePlayer().Id % 2 == 0)
        {
            if (targetHex.Coord.r >= _maxSpawnDistance) return;
        }
        else
        {
            if (_hexGrid.HeightInHexes - _maxSpawnDistance > targetHex.Coord.r) return;
        }

        if (targetHex.IsPassable)
        {
            _selectedUnit.transform.position = targetHex.transform.position;
            targetHex.OccupyingObject = _selectedUnit.gameObject;
            SelectedUnit = null;
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
        if (_activeUnit.IsBusy || _activeUnit.IsAnySkillBusy) return;

        if (targetHex.IsOccupied)
        {
            HexUnit occupyingUnit = targetHex.OccupyingObject.GetComponent<HexUnit>();

            if (occupyingUnit)
            {
                //Ability will handle it.
                if (_cachedDirectActions.Contains(targetHex)) return;

                if (_cachedDirectAttacks.Contains(targetHex))
                {
                    //Deactivate active ability to prevent double cast.
                    Ability activeSkill = _activeUnit.ActiveSkill();
                    if (activeSkill) activeSkill.Deactivate();

                    if (_activeUnit.RangedAttack)
                    {
                        if (HexCoord.Distance(targetHex.Coord, _selectedHex.Coord) > 1)
                        {
                            _activeUnit.MeleeAttack.Deactivate();
                            _activeUnit.RangedAttack.ActivateAutoCast(occupyingUnit.gameObject);
                            return;
                        }
                        _activeUnit.RangedAttack.Deactivate();
                    }
                    _activeUnit.MeleeAttack.ActivateAutoCast(occupyingUnit.gameObject);
                    return;
                }

                //Look if we can melee attack with movement.
                //Only check cached attack tuples found with Breadth First Search.
                List<HexCoord> shortestPath = null;
                foreach (Tuple<HexTile, HexTile> tuple in _cachedMoveActions.Where(c => c.second == targetHex))
                {
                    List<HexCoord> path = pathGraphSearch.GetShortestPath(_selectedHex.Coord, tuple.first.Coord);
                    if (shortestPath == null) shortestPath = path;
                    else if (shortestPath.Count > path.Count) shortestPath = path;

                    //Can't find a shorter path, early exit.
                    if (shortestPath.Count == 1) break;
                }
                if (shortestPath != null && shortestPath.Count > 0)
                {
                    _queuedActions.Enqueue(() => _activeUnit.MeleeAttack.ActivateAutoCast(occupyingUnit.gameObject));
                    _activeUnit.Move(_selectedHex, shortestPath, _hexGrid);
                    return;
                }

            }
            SelectHex(targetHex);
        }
        else
        {
            //If we can move there, move.
            if (_cachedMoves.Contains(targetHex))
            {
                List<HexCoord> path = pathGraphSearch.GetShortestPath(_selectedHex.Coord, targetHex.Coord);
                if (path != null && path.Count > 0) _activeUnit.Move(_selectedHex, path, _hexGrid);
            }
            else SelectHex(targetHex);
        }
    }

    /// <summary>
    /// Called after completion of a command is finished execuing until all the queued messages are processed.
    /// When no queued Actions left, it ends the turn for the Active player and clears player/turn specific data.
    /// </summary>
    private void OnActionCompletedCallback(Component sender, bool isSuccess)
    {
        if (!isSuccess)
        {
            GameHud gameHud = GuiManager.Instance.CurrentState as GameHud;
            if (gameHud != null) gameHud.ShowPopup(sender.name + " has failed!", new Vector2(0.5f, 0.5f), Color.red);
        }

        if (_queuedActions.Count > 0)
        {
            _queuedActions.Dequeue().Invoke();
        }
        else
        {
            ResetActiveHexes();
            _selectedHex = null;
            TurnManager.Instance.EndTurn();
            _activeUnit = TurnManager.Instance.GetActivePlayer().GetNextUnit();
            _selectedHex = _activeUnit.GetHexTile(_hexGrid);
            SelectHex(_selectedHex);
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
        else SelectedUnit = _activeUnit;
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

        HexUnit hexUnit = null;
        if (_selectedHex.OccupyingObject) hexUnit = _selectedHex.OccupyingObject.GetComponent<HexUnit>();
        if (!hexUnit)
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
                //Only check for attack positions if its the active unit and can melee attack.
                if (hexTile.IsOccupied && _selectedUnit == _activeUnit && _selectedUnit.MeleeAttack)
                {
                    hexUnit = hexTile.OccupyingObject.GetComponent<HexUnit>();
                    if (hexUnit == null || hexUnit.OwnerId == _selectedUnit.OwnerId || hexUnit.IsDead) continue;
                    //Melee attackable enemy found.
                    //Find previous move hex.
                    foreach (HexCoord neighbor in hexTile.Coord.Neighbors())
                    {
                        if (!_hexGrid.IsCordinateValid(neighbor)) continue;
                        HexTile neighborHexTile = _hexGrid.GetHexTile(neighbor);
                        if (!_cachedMoves.Contains(neighborHexTile)) continue;
                        Tuple<HexTile, HexTile> meleeAttackTuple = new Tuple<HexTile, HexTile>(neighborHexTile, hexTile);
                        if (!_cachedMoveActions.Contains(meleeAttackTuple)) _cachedMoveActions.Add(meleeAttackTuple);
                        hexTile.HighlightTile(Color.red);
                    }
                }
                else
                {
                    //Mark move hex.
                    //Cache into other if it not the currently active unit.
                    if (_selectedUnit == _activeUnit)
                    {
                        _cachedMoves.Add(hexTile);
                        hexTile.HighlightTile();
                    }
                    else
                    {
                        _cachedOtherHexes.Add(hexTile);
                        hexTile.HighlightTile(Color.blue);
                    }
                }
            }

            //Only show attack and skill marking if the selected unit is active Unit.
            if (_selectedUnit == _activeUnit)
            {
                //Show active skilll.
                HighlightAndCache(_activeUnit.ActiveSkill(), Color.cyan, _cachedDirectActions);
                //Show melee attack.
                HighlightAndCache(_activeUnit.MeleeAttack, Color.red, _cachedDirectAttacks);
                //Show ranged attack.
                HighlightAndCache(_activeUnit.RangedAttack, Color.red, _cachedDirectAttacks);
            }

            //Always highlight Active Unit.
            if (!_cachedDirectActions.Contains(_selectedHex))
            {
                if (!_cachedOtherHexes.Contains(_selectedHex))
                    _cachedOtherHexes.Add(_selectedHex);
                _selectedHex.HighlightTile(Color.white);
            }
        }
    }

    private void HighlightAndCache(Ability ability, Color c, List<HexTile> cache)
    {
        if (!ability) return;
        AbilityAim aim = ability.GetComponent<AbilityAim>();
        List<HexTile> availableHexTiles = aim.GetAvailableHexes(_hexGrid);
        if (availableHexTiles == null) return;
        foreach (HexTile availableHex in aim.GetAvailableHexes(_hexGrid))
        {
            if (!cache.Contains(availableHex))
            {
                if (!cache.Contains(availableHex))
                {
                    cache.Add(availableHex);
                    availableHex.HighlightTile(c);
                }
            }
        }
    }

    private void ResetActiveHexes()
    {
        foreach (Tuple<HexTile, HexTile> tuple in _cachedMoveActions)
        {
            //First will be reset be cachedMoveHexes.
            tuple.second.AutoSetState();
        }
        _cachedMoveActions.Clear();

        foreach (HexTile hexTile in _cachedDirectActions)
        {
            hexTile.AutoSetState();
        }
        _cachedDirectActions.Clear();

        foreach (HexTile hexTile in _cachedDirectAttacks)
        {
            hexTile.AutoSetState();
        }
        _cachedDirectAttacks.Clear();

        foreach (HexTile hexTile in _cachedOtherHexes)
        {
            hexTile.AutoSetState();
        }
        _cachedOtherHexes.Clear();

        foreach (HexTile hexTile in _cachedMoves)
        {
            hexTile.AutoSetState();
        }
        _cachedMoves.Clear();
    }
}