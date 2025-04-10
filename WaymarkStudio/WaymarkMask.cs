using Newtonsoft.Json;
using System;
namespace WaymarkStudio;

[Serializable]
public struct WaymarkMask
{
    [JsonProperty]
    private byte mask;

    public WaymarkMask(byte mask)
    {
        this.mask = mask;
    }

    public static implicit operator WaymarkMask(byte mask)
    {
        return new(mask);
    }

    public static implicit operator byte(WaymarkMask waymarkMask)
    {
        return waymarkMask.mask;
    }

    public bool IsSet(int pos)
    {
        return (mask & (1 << pos)) != 0;
    }

    public bool IsSet(Waymark waymark)
    {
        return IsSet((int)waymark);
    }

    public void Set(int pos, bool v)
    {
        mask = (byte)(v ? (mask | (1 << pos)) : (mask & ~(1 << pos)));
    }

    public void Set(Waymark waymark, bool v)
    {
        Set((int)waymark, v);
    }
    public bool IsAnySet()
    {
        return mask != 0;
    }
}
