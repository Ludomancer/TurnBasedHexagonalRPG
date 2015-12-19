using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.UI;
using MiniJSON;

public class UnitCard : MonoBehaviour
{
    public const string UNIT_NAME_KEY = "name";
    private const string UNITY_PORTRAIT_KEY = "portrait";

    [SerializeField]
    private Transform _skillContainer;

    [SerializeField]
    private GameObject _skillPrefab;

    [SerializeField]
    private Color _enemyColor;

    [SerializeField]
    private Color _allyColor;

    [SerializeField]
    private Text _unitNameText;

    [SerializeField]
    private Image _unitImage;

    [SerializeField]
    private Text _unitInfoText;

    [SerializeField]
    private Slider _unitHealthBar;

    private string _lastLoadedKey;

    public void SetData(HexUnit unit)
    {
        //Do not reload resource if it is the same.
        if (unit.UniqueName != _lastLoadedKey)
        {
            TextAsset unitDataFile = Resources.Load(Path.Combine(CommonPaths.UNIT_DATA_DIR, unit.UniqueName), typeof(TextAsset)) as TextAsset;
            if (unitDataFile)
            {
                Dictionary<string, object> rawData = Json.Deserialize(unitDataFile.text) as Dictionary<string, object>;
                if (rawData != null)
                {
                    if (rawData.ContainsKey(UNIT_NAME_KEY))
                    {
                        string name = rawData[UNIT_NAME_KEY] as string;
                        if (!string.IsNullOrEmpty(name)) _unitNameText.text = string.Format("{0} (P{1})", name, unit.OwnerId + 1);
                    }
                    if (rawData.ContainsKey(UNITY_PORTRAIT_KEY))
                    {
                        string portraitKey = rawData[UNITY_PORTRAIT_KEY] as string;
                        _unitImage.sprite = !string.IsNullOrEmpty(portraitKey) ? Resources.Load<Sprite>(Path.Combine(CommonPaths.TEXTURES, portraitKey)) : null;
                    }
                    _lastLoadedKey = unit.UniqueName;
                }
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Armor: " + unit.Armor);
        sb.AppendLine("Dodge Chance: %" + unit.DodgeChance * 100);
        _unitInfoText.text = sb.ToString();

        _unitHealthBar.value = unit.HealthLeft / (float)unit.MaxHealth;

        _unitNameText.color = unit.OwnerId == TurnManager.Instance.GetActivePlayer().Id ? _allyColor : _enemyColor;

        while (_skillContainer.childCount > 0)
        {
            Transform tempTransform = _skillContainer.GetChild(0);
            tempTransform.GetComponent<Button>().onClick.RemoveAllListeners();
            PoolManager.instance.Recycle(tempTransform.gameObject);
        }

        AddSkillButton(unit, null, "AT", -1);

        for (int i = 0; i < unit.Skills.Length; i++)
        {
            AddSkillButton(unit, unit.Skills[i], unit.Skills[i].SkillName, i);
        }
    }

    private void AddSkillButton(HexUnit unit, Ability skill, string skillName, int i)
    {
        GameObject skillButton = PoolManager.instance.GetObjectForName(_skillPrefab.name, false);

        skillButton.GetComponentInChildren<Text>().text = skillName;

        Button button = skillButton.GetComponent<Button>();

        bool isEnabled;
        if (skill)
        {
            isEnabled = skill.MpCost <= unit.Mp;
        }
        else isEnabled = true;

        button.interactable = isEnabled;
        if (isEnabled)
        {
            //Copy needed to properly set event params.Otherwise it takes the final value for all events.
            int skillIndex = i;
            button.onClick.AddListener(delegate { unit.SelectSkill(skillIndex); });
        }

        skillButton.transform.SetParent(_skillContainer, false);
    }
}
