using System;
using UnityEngine;
using System.Collections;

public static class LogHelper  {

    public static string GetTimeStamp()
    {
        return DateTime.Now.ToString("hh:mm:ss:ffff");
    } 
}
