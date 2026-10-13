using System.Collections;

namespace Deskair.Tests.Fakes;

/// <summary>A list a background thread can add to while a test reads it: every read works on a copy, so it never fails with "collection was modified".</summary>
public sealed class ThreadSafeLog<T> : IReadOnlyList<T>
{
    private readonly Lock gate = new();
    private readonly List<T> items = [];

    public int Count
    {
        get
        {
            lock (gate)
            {
                return items.Count;
            }
        }
    }

    public T this[int index]
    {
        get
        {
            lock (gate)
            {
                return items[index];
            }
        }
    }

    public void Add(T item)
    {
        lock (gate)
        {
            items.Add(item);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        T[] copy;

        lock (gate)
        {
            copy = [.. items];
        }

        return ((IEnumerable<T>)copy).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
