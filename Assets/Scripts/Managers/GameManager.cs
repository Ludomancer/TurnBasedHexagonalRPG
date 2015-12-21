using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets;
using MiniJSON;
using Settworks.Hexagons;
using UnityEngine;

public class GameManager : Manager
{
    #region Fields

    public const string UNIT_SELECTION_CHANGED = "OnUnitSelectionChanged";
    public const string GAME_STATE_CHANGED = "OnGameStateChanged";
    private const string ARMY_UNITY_KEY = "name";
    public const int CACHE_DIRECT_ACTION = 0;
    public const int CACHE_DIRECT_ATTACK = 1;
    public const int CACHE_MOVES = 2;
    public const int CACHE_MOVE_ACTIONS = 3;
    public const int CACHE_OTHER = 4;
    //The unit to be played this turn.
    private HexUnit _activeUnit;
    private GameState _currentGameState = GameState.None;

    [SerializeField]
    private int _gameOverDelay = 1;

    private HexGrid _hexGrid;
    private bool _isInitialized;

    [SerializeField, Range(1, 10)]
    private int _maxArmySize = 5;

    [SerializeField, Range(1, 3)]
    private int _maxSpawnDistance = 2;

    private ShortestPathGraphSearch<HexCoord, HexCoord> _pathGraphSearch;
    private HexTile _selectedHex;
    //The unit selected by player, can not be commanded.
    private HexUnit _selectedUnit;
    private readonly List<Tuple<HexTile, HexTile>> _cachedMoveActions = new List<Tuple<HexTile, HexTile>>();
    private readonly Cache<HexTile> _hexTileCache = new Cache<HexTile>();
    private readonly Queue<Action> _queuedActions = new Queue<Action>();

    #endregion

    #region Properties

    protected HexUnit SelectedUnit
    {
        set
        {
            if (value != _selectedUnit) Messenger.Broadcast(UNIT_SELECTION_CHANGED, value);
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
                _currentGameState = value;
                Messenger.Broadcast(GAME_STATE_CHANGED, _currentGameState, value);
            }
        }
    }

    #endregion

    #region Other Members

    private void GameOver(int winnerId)
    {
        CurrentGameState = GameState.GameOver;
        _selectedHex = null;
        _selectedUnit = null;
        ResetActiveHexes();
        _queuedActions.Clear();
        StartCoroutine(FinalizeGame(winnerId));
    }

    public void StartGame()
    {
        if (!_isInitialized)
        {
            _hexGrid = FindObjectOfType<HexGrid>();
            if (_hexGrid == null) throw new Exception("No HexGrid found on the Scene.");
            _pathGraphSearch = new ShortestPathGraphSearch<HexCoord, HexCoord>(new HexPathFinding(_hexGrid));

            TurnManager.Instance.Init();

            Messenger.AddListener<HexTile>(HexTile.ON_HEX_CLICKED_EVENT_NAME, OnHexClicked);
            Messenger.AddListener<Ability>(Ability.ON_ABILITY_ACTIVATED, OnAbilityActivated);
            Messenger.AddListener<Ability>(Ability.ON_ABILITY_DEACTIVATED, OnAbilityDeactivated);
            Messenger.AddListener<Component, bool>(Ability.ON_CAST_COMPLETED, OnActionCompletedCallback);
            Messenger.AddListener<Component, bool>(HexUnit.ON_MOVE_COMPLETED, OnActionCompletedCallback);
            Messenger.AddListener<GameObject>(Player.ON_UNIT_REMOVED, OnUnitRemoved);
            _isInitialized = true;
        }

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

    private IEnumerator ArmyBuild()
    {
        int armySize = LoadArmyData();
        //Give everyone else time to register to events.
        yield return null;

        CurrentGameState = GameState.ArmyBuild;
        GameHud gameHud = GuiManager.Instance.CurrentState as GameHud;
        for (int i = 0; i < armySize; i++)
        {
            int turn = TurnManager.Instance.CurrentTurn;
            Vector2 topLeftHex =
                Camera.main.WorldToScreenPoint(_hexGrid.GetWorldPositionOfHex(_hexGrid.GetHexTileDirect(0, 0).Coord));
            Vector2 bottomRightHex =
                Camera.main.WorldToScreenPoint(
                    _hexGrid.GetWorldPositionOfHex(
                        _hexGrid.GetHexTileDirect(_hexGrid.WidthInHexes - 1, _hexGrid.HeightInHexes - 1).Coord));
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
                    for (int r = 0; r < _maxSpawnDistance; r++)
                    {
                        for (int q = 0; q < _hexGrid.GetRowLenght(r); q++)
                        {
                            HexTile hexTile = _hexGrid.GetHexTileDirect(q, r);
                            hexTile.HighlightTile();
                            _hexTileCache.Add(CACHE_OTHER, hexTile);
                        }
                    }
                    refPosition =
                        _hexGrid.GetWorldPositionOfHex(
                            _hexGrid.GetHexTileDirect((int)(_hexGrid.WidthInHexes / 2f), _maxSpawnDistance - 1).Coord);
                    limitFunction = Mathf.Min;
                }
                else
                {
                    for (int r = _hexGrid.HeightInHexes - _maxSpawnDistance; r < _hexGrid.HeightInHexes; r++)
                    {
                        for (int q = 0; q < _hexGrid.GetRowLenght(r); q++)
                        {
                            HexTile hexTile = _hexGrid.GetHexTileDirect(q, r);
                            hexTile.HighlightTile();
                            _hexTileCache.Add(CACHE_OTHER, hexTile);
                        }
                    }
                    refPosition =
                        _hexGrid.GetWorldPositionOfHex(
                            _hexGrid.GetHexTileDirect((int)(_hexGrid.WidthInHexes / 2f),
                                _hexGrid.HeightInHexes - _maxSpawnDistance).Coord);
                    limitFunction = Mathf.Max;
                }
                float xPlayerLimit = Camera.main.WorldToScreenPoint(refPosition).x;

                Transform selectedUnitTransform = _selectedUnit.transform;
                selectedUnitTransform.position = refPosition;

                //Wait until player places the Unit.
                while (_selectedUnit != null)
                {
                    Vector2 rayPos =
                        new Vector2(limitFunction(xPlayerLimit, Mathf.Clamp(Input.mousePosition.x, xMin, xMax)),
                            Mathf.Clamp(Input.mousePosition.y, yMin, yMax));
                    Ray ray = Camera.main.ScreenPointToRay(rayPos);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        selectedUnitTransform.position = hit.point;
                    }
                    yield return null;
                }
                TurnManager.Instance.EndTurn();
                ResetActiveHexes();
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

    private void OnUnitRemoved(GameObject go)
    {
        HexTile unitHexTile = go.GetComponent<HexUnit>().GetHexTile(_hexGrid);
        unitHexTile.OccupyingObject = null;
        TryGameOver();
        if (CurrentGameState == GameState.GameOver) return;
        _hexTileCache.Remove(unitHexTile);
        //Update tex grid.
        SelectHex(_selectedHex);
    }

    private IEnumerator FinalizeGame(int winnerId)
    {
        float waitUntil = Time.realtimeSinceStartup + _gameOverDelay;
        //Wait for all active actions to be finished.
        for (int p = 0; p < TurnManager.Instance.PlayerCount; p++)
        {
            Player player = TurnManager.Instance.GetPlayer(p);
            for (int u = 0; u < player.UnitCount; u++)
            {
                while (player.PeekUnit(u).IsAnySkillBusy || player.PeekUnit(u).IsBusy)
                {
                    yield return null;
                }
            }
        }

        while (waitUntil > Time.realtimeSinceStartup) yield return null;

        //Clear everything.
        for (int i = 0; i < TurnManager.Instance.PlayerCount; i++)
        {
            TurnManager.Instance.GetPlayer(i).RemoveAllUnits();
        }
        PoolManager.instance.RecycleAll();
        TurnManager.Instance.Reset();
        GuiManager.Instance.CurrentState = GuiManager.Instance.GetState(UiStates.GAME_OVER_STATE);
        ((GameOverPanel)GuiManager.Instance.CurrentState).SetWinner(winnerId);
    }

    private int LoadArmyData()
    {
#if UNITY_EDITOR
        for (int j = 0; j < TurnManager.Instance.PlayerCount; j++)
        {
            if (TurnManager.Instance.GetPlayer(j).UnitCount > 0)
            {
                Debug.LogError("Player " + j + " has " + TurnManager.Instance.GetPlayer(j).UnitCount
                    + " units at the start of the game!");
            }
        }
#endif
        TextAsset unitDataFile =
            Resources.Load(Path.Combine(CommonPaths.UNIT_DATA_DIR, "Army"), typeof(TextAsset)) as TextAsset;
        if (unitDataFile)
        {
            Dictionary<string, object> rawData = Json.Deserialize(unitDataFile.text) as Dictionary<string, object>;

            for (int i = 0; i < _maxArmySize; i++)
            {
                string unitKey = ARMY_UNITY_KEY + i;
                if (rawData != null && rawData.ContainsKey(unitKey))
                {
                    string unitName = rawData[unitKey] as string;
                    for (int j = 0; j < TurnManager.Instance.PlayerCount; j++)
                    {
                        GameObject newUnit = PoolManager.instance.GetObjectForName(unitName, false);
                        HexUnit hexUnit = newUnit.GetComponent<HexUnit>();
                        hexUnit.Reset();
                        if (j % 2 == 0) hexUnit.FaceRight();
                        else hexUnit.FaceLeft();
                        TurnManager.Instance.GetPlayer(j).AddUnit(hexUnit);
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

        if (_selectedUnit && targetHex.IsPassable)
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
                if (_hexTileCache.Contains(CACHE_DIRECT_ACTION, targetHex))
                {
                    //Deactivate normal attacks to prevent double cast.
                    if (_activeUnit.MeleeAttack) _activeUnit.MeleeAttack.Deactivate();
                    if (_activeUnit.RangedAttack) _activeUnit.RangedAttack.Deactivate();
                    return;
                }

                if (_hexTileCache.Contains(CACHE_DIRECT_ATTACK, targetHex))
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
                    List<HexCoord> path = _pathGraphSearch.GetShortestPath(_selectedHex.Coord, tuple.first.Coord);
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
            if (_hexTileCache.Contains(CACHE_MOVES, targetHex))
            {
                //Deactivate active ability to prevent double cast.
                Ability activeSkill = _activeUnit.ActiveSkill();
                if (activeSkill) activeSkill.Deactivate();
                //Deactivate normal attacks to prevent double cast.
                if (_activeUnit.MeleeAttack) _activeUnit.MeleeAttack.Deactivate();
                if (_activeUnit.RangedAttack) _activeUnit.RangedAttack.Deactivate();

                List<HexCoord> path = _pathGraphSearch.GetShortestPath(_selectedHex.Coord, targetHex.Coord);
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
            TryGameOver();
            if (CurrentGameState == GameState.GameOver) return;
            Ability ability = sender as Ability;
            if (ability && ability.IsPassive) return;
            ResetActiveHexes();
            _selectedHex = null;
            TurnManager.Instance.EndTurn();

            while (true)
            {
                _activeUnit = TurnManager.Instance.GetActivePlayer().GetNextUnit();
                //Unit might be dead but delayed. Check IsDead.
                //If the player has not Available units we should not be here at all. (TryGameOver handles that.)
                //Therefore we don't have to check for a case of infinite-loop.
                if (!_activeUnit.IsDead) break;
            }

            _selectedHex = _activeUnit.GetHexTile(_hexGrid);
            SelectHex(_selectedHex);
        }
    }

    private void TryGameOver()
    {
        if (CurrentGameState != GameState.Battle) return;
        //Assumes 2 players.
        for (int p = 0; p < TurnManager.Instance.PlayerCount; p++)
        {
            //Since unit removal delayed we have to check for IsDead
            //property rather than just UnitCount.
            if (!TurnManager.Instance.GetPlayer(p).IsAnyUnitAlive())
            {
                if (p == TurnManager.Instance.PlayerCount - 1)
                {
                    //If it is the last index, that means only this player has no units left.
                    GameOver(0);
                }
                else if (!TurnManager.Instance.GetPlayer(TurnManager.Instance.PlayerCount - 1).IsAnyUnitAlive())
                {
                    //This is very unlikely but might be possible with certain abiliy combinations.
                    //If it is not the last player, check for the last player and if it doens't have any units left
                    //Call it a draw.
                    GameOver(-1);
                }
                else
                {
                    //If it is the first Player and the second player still has units Player 2 wins.
                    GameOver(1);
                }
            }
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
            _hexTileCache.Add(CACHE_OTHER, _selectedHex);
            _selectedHex.HighlightTile();
        }
        else
        {
            ResetActiveHexes();
            bool hasValidMoves = false;

            //Mark movement range. Allows occupied hex in the last node.
            foreach (
                HexTile hexTile in _hexGrid.HexesInReachableRange(_selectedHex.Coord, _selectedUnit.MovementRange, true)
                )
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
                        if (!_hexTileCache.Contains(CACHE_MOVES, neighborHexTile)) continue;
                        Tuple<HexTile, HexTile> meleeAttackTuple = new Tuple<HexTile, HexTile>(neighborHexTile, hexTile);
                        if (!_cachedMoveActions.Contains(meleeAttackTuple)) _cachedMoveActions.Add(meleeAttackTuple);
                        hexTile.HighlightTile(Color.red);
                        hasValidMoves = true;
                    }
                }
                else
                {
                    //Mark move hex.
                    //Cache into other if it not the currently active unit.
                    if (_selectedUnit == _activeUnit)
                    {
                        _hexTileCache.Add(CACHE_MOVES, hexTile);
                        hexTile.HighlightTile();
                        hasValidMoves = true;
                    }
                    else
                    {
                        _hexTileCache.Add(CACHE_OTHER, hexTile);
                        hexTile.HighlightTile(Color.blue);
                    }
                }
            }

            //Only show attack and skill marking if the selected unit is active Unit.
            if (_selectedUnit == _activeUnit)
            {
                //Show active skilll.
                if (HighlightAndCache(_activeUnit.ActiveSkill(), Color.cyan, CACHE_DIRECT_ACTION)) hasValidMoves = true;
                //Show melee attack.
                if (HighlightAndCache(_activeUnit.MeleeAttack, Color.red, CACHE_DIRECT_ATTACK)) hasValidMoves = true;
                //Show ranged attack.
                if (HighlightAndCache(_activeUnit.RangedAttack, Color.red, CACHE_DIRECT_ATTACK)) hasValidMoves = true;
            }

            if (hasValidMoves)
            {
                //Always highlight Active Unit.
                if (!_hexTileCache.Contains(CACHE_DIRECT_ATTACK, _selectedHex))
                {
                    if (!_hexTileCache.Contains(CACHE_OTHER, _selectedHex))
                        _hexTileCache.Add(CACHE_OTHER, _selectedHex);
                    _selectedHex.HighlightTile(Color.white);
                }
            }
            else
            {
                OnActionCompletedCallback(this, true);
            }
        }
    }

    private bool HighlightAndCache(Ability ability, Color c, int cacheId)
    {
        if (!ability) return false;
        AbilityAim aim = ability.GetComponent<AbilityAim>();
        List<HexTile> availableHexTiles = aim.GetAvailableHexes(_hexGrid);
        if (availableHexTiles == null) return false;
        bool hasValidHexes = false;
        foreach (HexTile availableHex in availableHexTiles)
        {
            if (!_hexTileCache.Contains(cacheId, availableHex))
            {
                _hexTileCache.Add(cacheId, availableHex);
                availableHex.HighlightTile(c);
                hasValidHexes = true;
            }
        }
        return hasValidHexes;
    }

    private void ResetActiveHexes()
    {
        foreach (Tuple<HexTile, HexTile> tuple in _cachedMoveActions)
        {
            //First will be reset be cachedMoveHexes.
            tuple.second.AutoSetState();
        }
        _cachedMoveActions.Clear();

        foreach (KeyValuePair<int, List<HexTile>> subCache in _hexTileCache)
        {
            foreach (HexTile hexTile in subCache.Value)
            {
                hexTile.AutoSetState();
            }
        }
        _hexTileCache.ClearCache();
    }

    #endregion
}