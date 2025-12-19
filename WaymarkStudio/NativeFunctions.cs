using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Numerics;

namespace WaymarkStudio;
internal static unsafe class NativeFunctions
{
    internal static unsafe byte PlaceWaymark(Waymark waymark, Vector3 wPos)
    {
        return MarkingController.Instance()->PlaceFieldMarker((uint)waymark, &wPos);
    }

    internal static unsafe byte ClearWaymark(Waymark waymark)
    {
        return MarkingController.Instance()->ClearFieldMarker((uint)waymark);
    }

    internal static unsafe byte ClearWaymarks()
    {
        return MarkingController.Instance()->ClearFieldMarkers();
    }

    internal static unsafe byte PlacePreset(MarkerPresetPlacement* placement)
    {
        return MarkingController.Instance()->PlacePreset(placement);
    }
}
