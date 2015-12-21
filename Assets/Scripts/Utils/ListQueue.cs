using System.Collections.Generic;

public class ListQueue<T> : List<T>
{
    #region Other Members

    public T Peek()
    {
        return base[0];
    }

    public T Dequeue()
    {
        var t = base[0];
        RemoveAt(0);
        return t;
    }

    public void Enqueue(T item)
    {
        Add(item);
    }

    #endregion
}