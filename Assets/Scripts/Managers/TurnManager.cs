using UnityEngine;
using UnityEngine.Assertions;

public class TurnManager : Manager
{
    #region Singleton

    private static TurnManager _instance;

    internal static TurnManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            Assert.AreEqual(1, FindObjectsOfType<TurnManager>().Length);

            _instance = FindObjectOfType<TurnManager>();
            if (_instance != null) return _instance;

            return _instance;
        }
    }
    #endregion

    public const string ON_TURN_END = "OnTurnEnd";

    private Player[] _players;
    private int _currentTurn = 0;
    private int _activePlayer;
    private int _firstPlayer;

    public override void Init()
    {
        _players = new Player[2];
        for (int i = 0; i < 2; i++)
        {
            _players[i] = new GameObject("Player " + (i + 1), typeof(Player)).GetComponent<Player>();
            _players[i].Initialize(i);
        }
        Reset();
    }

    public void Reset()
    {
        _currentTurn = 0;
        _firstPlayer = Random.Range(0, 1);
        _activePlayer = _firstPlayer;
    }

    public void EndTurn()
    {
        _activePlayer++;
        if (_activePlayer == _players.Length) _activePlayer = 0;
        if (_firstPlayer == _activePlayer) _currentTurn = CurrentTurn + 1;
        Messenger.Broadcast(ON_TURN_END);
    }

    public Player GetActivePlayer()
    {
        return _players[_activePlayer];
    }

    public Player GetPlayer(int i)
    {
        return _players[i];
    }

    public int PlayerCount
    {
        get { return _players == null ? 0 : _players.Length; }
    }

    public int CurrentTurn
    {
        get { return _currentTurn; }
    }
}
