using System;
using UnityEngine;
using UnityEngine.Assertions;

public class GuiManager : Manager
{
    #region Fields

    private PanelBase _currentState;

    [SerializeField]
    private GameObject _defaultPanelPrefab;

    private bool _isTransitioning;

    [SerializeField]
    private Transform _panelContainer;

    private PanelBase _previousState;

    #endregion

    #region Properties

    public PanelBase CurrentState
    {
        get { return _currentState; }
        set
        {
            if (_currentState == null)
            {
                _isTransitioning = false;
                _currentState = value;
                _currentState.gameObject.SetActive(true);
                _currentState.BringPanel();
            }
            else
            {
                if (_currentState.Equals(value) && !_currentState.CanTransitionToSelf) return;
                if (!_currentState.IsTransitioning)
                {
                    Debug.Log("Transition Interrupted: " + _previousState + "->" + _currentState);
                    _isTransitioning = false;
                }
                Debug.Log("Transition." + _currentState + "->" + value);
                _previousState = _currentState;
                _previousState.DismissPanel();
                _currentState = value;
                _currentState.ReturnAddress = _previousState;
                _isTransitioning = true;
            }
        }
    }

    public bool IsTransitioning
    {
        get { return _isTransitioning; }
    }

    #endregion

    #region Other Members

    public PanelBase GetState(string key)
    {
        GameObject panel = PoolManager.instance.GetObjectForName(key, false);
        if (panel)
        {
            PanelBase newPanelBase = panel.GetComponent<PanelBase>();
            if (newPanelBase)
            {
                newPanelBase.Init();
#if UNITY_EDITOR
                panel.name = panel.name.Replace("(Clone)", "");
#endif
                panel.transform.SetParent(_panelContainer, false);
                panel.SetActive(false);
            }
            else throw new Exception("Panel prefab does not have PanelBase attached to it!");
            return newPanelBase;
        }
        return null;
    }

    private void Start()
    {
        Init();
    }

    private void TryToFinalizeTransition()
    {
        if (_isTransitioning)
        {
            if (!_previousState.IsTransitioning)
            {
                _isTransitioning = false;
                PoolManager.instance.Recycle(_previousState.gameObject);
                _currentState.gameObject.SetActive(true);
                _currentState.BringPanel();
            }
            //else Debug.Log("Previous transition not finished.");
        }
        //else Debug.Log("No transition active.");
    }

    public override void Init()
    {
        if (_defaultPanelPrefab) CurrentState = GetState(_defaultPanelPrefab.name);
        Messenger.AddListener<PanelBase>(PanelBase.ON_TRANSITION_END_EVENT, OnTransitionEnd);
    }

    public void DestroyUnusedPanels()
    {
        if (_currentState)
        {
            for (int i = 0; i < _panelContainer.childCount; i++)
            {
                Transform tempTransform = _panelContainer.GetChild(i);
                if (tempTransform != _currentState.transform
                    && (_previousState == null || tempTransform != _previousState.transform))
                {
                    Destroy(tempTransform.gameObject);
                }
            }
        }
    }

    private void OnTransitionEnd(PanelBase panel)
    {
        TryToFinalizeTransition();
    }

    #endregion

    #region Singleton

    private static GuiManager _instance;

    internal static GuiManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            Assert.AreEqual(1, FindObjectsOfType<GuiManager>().Length);

            _instance = FindObjectOfType<GuiManager>();
            if (_instance != null) return _instance;

            return _instance;
        }
    }

    #endregion
}