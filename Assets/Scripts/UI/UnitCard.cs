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
                    if (rawData.ContainsKey(UNIT_NAME_KEY)) _unitNameText.text = rawData[UNIT_NAME_KEY] as string;
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
        if (unit.MeleeAttack) sb.AppendLine("Melee Attack: " + unit.MeleeAttack.Strength);
        if (unit.RangedAttack) sb.AppendLine("Ranged Attack: " + unit.RangedAttack.Strength);
        sb.AppendLine("Armor: " + unit.Armor);
        sb.AppendLine("Dodge Chance: %" + unit.DodgeChange * 100);
        _unitInfoText.text = sb.ToString();

        _unitHealthBar.value = unit.HealthLeft / (float)unit.MaxHealth;
    }
}
