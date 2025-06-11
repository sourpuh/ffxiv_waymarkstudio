namespace Pictomancy.VfxDraw;

internal class InterframeResourceTracker<T> : IDisposable where T : IDisposable
{
    private Dictionary<string, T> prevActive;
    private Dictionary<string, T> currActive;

    internal InterframeResourceTracker()
    {
        prevActive = new();
        currActive = new();
    }

    internal bool IsTouched(string key)
    {
        return currActive.ContainsKey(key);
    }

    internal bool TryTouchExisting(string key, out T o)
    {
        if (prevActive.TryGetValue(key, out o))
        {
            prevActive.Remove(key);
            currActive.Add(key, o);
            return true;
        }
        return false;
    }

    internal void TouchNew(string key, T t)
    {
        currActive.Add(key, t);
    }

    public void Update()
    {
        foreach (var item in prevActive)
        {
            item.Value.Dispose();
        }
        prevActive.Clear();

        var tmp = prevActive;
        prevActive = currActive;
        currActive = tmp;
    }

    public void Dispose()
    {
        foreach (var item in prevActive)
        {
            item.Value.Dispose();
        }
        foreach (var item in currActive)
        {
            item.Value.Dispose();
        }
    }
}