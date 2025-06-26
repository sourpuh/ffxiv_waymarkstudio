using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Numerics;
using System.Runtime.InteropServices;

namespace WaymarkStudio;
internal static unsafe class NativeFunctions
{
    internal unsafe delegate byte PlaceWaymarkDelegate(MarkingController* markingController, uint marker, Vector3 wPos);
    internal static PlaceWaymarkDelegate PlaceWaymark;

    internal unsafe delegate byte ClearWaymarkDelegate(MarkingController* markingController, uint marker);
    internal static ClearWaymarkDelegate ClearWaymark;

    internal unsafe delegate byte ClearWaymarksDelegate(MarkingController* markingController);
    internal static ClearWaymarksDelegate ClearWaymarks;

    internal unsafe delegate byte WaymarkSafetyDelegate();
    internal static WaymarkSafetyDelegate WaymarkSafety;

    internal unsafe delegate byte PlacePresetDelegate(MarkingController* markingController, MarkerPresetPlacement* placement);
    internal static PlacePresetDelegate PlacePreset;
    public static void Initialize()
    {
        PlaceWaymark = Marshal.GetDelegateForFunctionPointer<PlaceWaymarkDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 EB 58 44 8B 83 ?? ?? ?? ??"));
        ClearWaymark = Marshal.GetDelegateForFunctionPointer<ClearWaymarkDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? EB D8 83 FB 09"));
        ClearWaymarks = Marshal.GetDelegateForFunctionPointer<ClearWaymarksDelegate>(Plugin.SigScanner.ScanText("41 55 48 83 EC 50 4C 8B E9"));
        WaymarkSafety = Marshal.GetDelegateForFunctionPointer<WaymarkSafetyDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 0D B0 05"));
        PlacePreset = Marshal.GetDelegateForFunctionPointer<PlacePresetDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 1B B0 01"));
    }
}
