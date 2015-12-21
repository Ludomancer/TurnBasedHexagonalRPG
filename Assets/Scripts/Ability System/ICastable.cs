/// <summary>
/// Any features that should be triggered when the ability has casted.
/// </summary>
internal interface ICastable
{
    #region Other Members

    void Cast();
    bool IsBusy();

    #endregion
}