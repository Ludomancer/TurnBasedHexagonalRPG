using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simple replica of Net Generic Tuple class.
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
[System.Serializable]
public class Tuple<T1, T2>
{
    #region Fields

    private static readonly IEqualityComparer Item1Comparer = EqualityComparer<T1>.Default;
    private static readonly IEqualityComparer Item2Comparer = EqualityComparer<T2>.Default;
    public readonly T1 first;
    public readonly T2 second;

    #endregion

    #region Other Members

    public Tuple(T1 first, T2 second)
    {
        this.first = first;
        this.second = second;
    }

    public override string ToString()
    {
        return string.Format("<{0}, {1}>", first, second);
    }

    public static bool operator ==(Tuple<T1, T2> a, Tuple<T1, T2> b)
    {
        if (IsNull(a) && !IsNull(b)) return false;
        if (!IsNull(a) && IsNull(b)) return false;
        if (IsNull(a) && IsNull(b)) return true;

        return a.first.Equals(b.first) && a.second.Equals(b.second);
    }

    public static bool operator !=(Tuple<T1, T2> a, Tuple<T1, T2> b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + first.GetHashCode();
        hash = hash * 23 + second.GetHashCode();
        return hash;
    }

    public override bool Equals(object obj)
    {
        var other = obj as Tuple<T1, T2>;
        if (ReferenceEquals(other, null)) return false;

        return Item1Comparer.Equals(first, other.first) && Item2Comparer.Equals(second, other.second);
    }

    private static bool IsNull(object obj)
    {
        return ReferenceEquals(obj, null);
    }

    #endregion
}