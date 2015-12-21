using System;

public static class LogHelper
{
    #region Other Members

    public static string GetTimeStamp()
    {
        return DateTime.Now.ToString("hh:mm:ss:ffff");
    }

    #endregion
}