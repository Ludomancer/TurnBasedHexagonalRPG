using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class PathExtensions
{
    /// <summary>
    /// Combines paths with forward slashes to be used in Unity Resources Load.
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <returns></returns>
    public static string CombineForward(string path1, string path2)
    {
        return Path.Combine(path1, path2).Replace("\\", "/");
    }
}