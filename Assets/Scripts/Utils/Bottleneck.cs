using UnityEngine;

internal class Bottleneck : MonoBehaviour
{
    #region Fields

    #region Variables

    /// <summary>
    /// How long the main thread should be occupied in Miliseconds.
    /// </summary>
    [Range(0, 250)]
    public int blockMainThreadFor = 10;

    #endregion

    #endregion

    #region Other Members

    #region Methods

    private void Update()
    {
        if (blockMainThreadFor > 0) System.Threading.Thread.Sleep(blockMainThreadFor);
    }

    #endregion

    #endregion

    #region Singleton

    private static Bottleneck instance;

    internal static Bottleneck Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = FindObjectOfType(typeof (Bottleneck)) as Bottleneck;
            if (instance != null) return instance;

            var container = new GameObject("Bottleneck");
            instance = container.AddComponent<Bottleneck>();
            return instance;
        }
    }

    #endregion

    #region Enumerations

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}