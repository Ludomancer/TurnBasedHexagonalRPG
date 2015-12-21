using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniJSON;
using UnityEngine;
using UnityEngine.UI;

public class UnitCard : MonoBehaviour
{
    #region Fields

    private const string UNITY_PORTRAIT_KEY = "portrait";

    [SerializeField]
    private Color _allyColor;

    private bool _canShowSkills;

    [SerializeField]
    private Color _enemyColor;

    private string _lastLoadedUnitId;
    private HexUnit _selectedUnit;

    [SerializeField]
    private Transform _skillContainer;

    [SerializeField]
    private GameObject _skillPrefab;

    private ToggleGroup _toggleGroup;

    [SerializeField]
    private Slider _unitHealthBar;

    [SerializeField]
    private Image _unitImage;

    [SerializeField]
    private Text _unitInfoText;

    [SerializeField]
    private Slider _unitManaBar;

    [SerializeField]
    private Text _unitNameText;

    #endregion

    #region Other Members

    private void Awake()
    {
        Messenger.AddListener<Destructable, int>(Destructable.ON_HEALTH_CHANGED, OnHealthChanged);
        Messenger.AddListener<HexUnit, int>(HexUnit.ON_MANA_CHANGED, OnManaChanged);
        Messenger.AddListener<GameState, GameState>(GameManager.GAME_STATE_CHANGED, OnGameStateChanged);
        _toggleGroup = _skillContainer.GetComponent<ToggleGroup>();
    }

    private void OnGameStateChanged(GameState oldGameState, GameState newGameState)
    {
        _canShowSkills = newGameState == GameState.Battle;
        _skillContainer.gameObject.SetActive(_canShowSkills);
    }

    private void OnManaChanged(HexUnit hexUnit, int i)
    {
        if (gameObject.activeSelf)
        {
            if (hexUnit && hexUnit == _selectedUnit)
            {
                _unitManaBar.value = hexUnit.ManaLeft == 0 ? 0 : hexUnit.ManaLeft / (float)hexUnit.MaxMana;
            }
        }
    }

    private void OnHealthChanged(Destructable destructable, int i)
    {
        if (gameObject.activeSelf)
        {
            HexUnit hexUnit = destructable as HexUnit;
            if (hexUnit && hexUnit == _selectedUnit)
            {
                _unitHealthBar.value = hexUnit.HealthLeft == 0 ? 0 : hexUnit.HealthLeft / (float)hexUnit.MaxHealth;
            }
        }
    }

    public void SetData(HexUnit unit)
    {
        _selectedUnit = unit;
        //Do not reload resource if it is the same.
        if (unit.UniqueName != _lastLoadedUnitId)
        {
            TextAsset unitDataFile =
                Resources.Load(Path.Combine(CommonPaths.UNIT_DATA_DIR, unit.UniqueName), typeof (TextAsset)) as
                    TextAsset;
            if (unitDataFile)
            {
                Dictionary<string, object> rawData = Json.Deserialize(unitDataFile.text) as Dictionary<string, object>;
                if (rawData != null)
                {
                    if (rawData.ContainsKey(UNITY_PORTRAIT_KEY))
                    {
                        string portraitKey = rawData[UNITY_PORTRAIT_KEY] as string;
                        _unitImage.sprite = !string.IsNullOrEmpty(portraitKey)
                            ? Resources.Load<Sprite>(Path.Combine(CommonPaths.TEXTURES, portraitKey))
                            : null;
                    }
                    _lastLoadedUnitId = unit.UniqueName;
                }
            }
        }


        _unitNameText.text = string.Format("{0} (P{1})", unit.UnitName, unit.OwnerId + 1);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Armor: " + unit.Armor);
        sb.AppendLine("Dodge Chance: %" + unit.DodgeChance * 100);
        sb.AppendLine(string.Format("HP: {0}/{1}", unit.HealthLeft, unit.MaxHealth));
        if (unit.MaxMana > 0) sb.AppendLine(string.Format("Mana: {0}/{1}", unit.ManaLeft, unit.MaxMana));
        _unitInfoText.text = sb.ToString();

        _unitHealthBar.value = unit.HealthLeft == 0 ? 0 : unit.HealthLeft / (float)unit.MaxHealth;
        _unitManaBar.value = unit.ManaLeft == 0 ? 0 : unit.ManaLeft / (float)unit.MaxMana;

        _unitNameText.color = unit.OwnerId == TurnManager.Instance.GetActivePlayer().Id ? _allyColor : _enemyColor;


        _skillContainer.gameObject.SetActive(_canShowSkills && unit.OwnerId == TurnManager.Instance.GetActivePlayer().Id);
        if (_skillContainer.gameObject.activeSelf)
        {
            foreach (Toggle toggle in _skillContainer.GetComponentsInChildren<Toggle>())
            {
                toggle.onValueChanged.RemoveAllListeners();
                PoolManager.instance.Recycle(toggle.gameObject);
            }

            if (unit.MeleeAttack || unit.RangedAttack) AddSkillButton(unit, null, "Attack", -1, true);

            for (int i = 0; i < unit.Skills.Length; i++)
            {
                if (!unit.Skills[i].IsPassive)
                {
                    AddSkillButton(unit, unit.Skills[i], unit.Skills[i].SkillName, i, false);
                }
            }
        }
    }

    private void AddSkillButton(HexUnit unit, Ability skill, string skillName, int i, bool isOn)
    {
        GameObject skillButton = PoolManager.instance.GetObjectForName(_skillPrefab.name, false);

        skillButton.GetComponentInChildren<Text>().text = skillName;

        Toggle button = skillButton.GetComponent<Toggle>();
        button.group = _toggleGroup;
        button.isOn = isOn;

        bool isEnabled;
        if (skill)
        {
            isEnabled = skill.ManaCost <= unit.ManaLeft;
        }
        else isEnabled = true;

        button.interactable = isEnabled;
        if (isEnabled)
        {
            int iCopy = i;
            //Copy needed to properly set event params.Otherwise it takes the final value for all events.
            button.onValueChanged.AddListener(delegate { OnvalueChangedCallback(button, iCopy); });
        }

        skillButton.transform.SetParent(_skillContainer, false);
    }

    //There is no nice way of knowing which toggle is active.
    private void OnvalueChangedCallback(Toggle toggle, int index)
    {
        if (_toggleGroup.ActiveToggles().FirstOrDefault() == toggle)
        {
            _selectedUnit.SelectSkill(index);
        }
    }

    #endregion
}