using UnityEngine;

public class BitMaskAttribute : PropertyAttribute
{
    #region Fields

    public System.Type propType;

    #endregion

    #region Other Members

    public BitMaskAttribute(System.Type aType)
    {
        propType = aType;
    }

    #endregion
}