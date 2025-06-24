using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System.Numerics;

namespace WaymarkStudio;
internal static class Raycaster
{
    const bool DebugCast = false;
    public unsafe static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance = 1120403456)
    {
        var framework = Framework.Instance();
        if (framework == null)
        {
            hitInfo = default;
            return false;
        }
        var bgCollisionModule = framework->BGCollisionModule;
        if (bgCollisionModule == null)
        {
            hitInfo = default;
            return false;
        }
        // These are the same flags used by ActionManager_UpdateAreaTargetingMode for Eden 4
        var flags = stackalloc int[] { 0x8004000, 0, 0, 0 };
        var hit = new RaycastHit();
        var result = bgCollisionModule->RaycastMaterialFilter(&hit, &origin, &direction, maxDistance, 1, flags);
        hitInfo = hit;

        if (DebugCast)
        {
            var flags2 = stackalloc int[] { 0x4000, 0, 0x4000, 0 };

            RaycastHit hitInfo2;
            var result2 = bgCollisionModule->RaycastMaterialFilter(&hitInfo2, &origin, &direction, maxDistance, 1, flags2);

            if (result != result2 || hit.Point != hitInfo2.Point)
            {
                Plugin.Overlay.DeferDrawDebugRay(hit, hitInfo2);
                Plugin.Chat.PrintError("MISMATCH");
            }
        }

        return result;
    }

    public static unsafe bool ScreenToWorld(Vector2 screenPos, out Vector3 worldPos, float rayDistance = 100000f)
    {
        // The game is only visible in the main viewport, so if the cursor is outside
        // of the game window, do not bother calculating anything
        var windowPos = ImGuiHelpers.MainViewport.Pos;
        var windowSize = ImGuiHelpers.MainViewport.Size;

        if (screenPos.X < windowPos.X || screenPos.X > windowPos.X + windowSize.X ||
            screenPos.Y < windowPos.Y || screenPos.Y > windowPos.Y + windowSize.Y)
        {
            worldPos = default;
            return false;
        }

        var camera = CameraManager.Instance()->CurrentCamera;
        if (camera == null)
        {
            worldPos = default;
            return false;
        }

        var ray = camera->ScreenPointToRay(screenPos);
        var result = Raycast(ray.Origin, ray.Direction, out var hit);
        worldPos = hit.Point;

        return result;
    }

    public static bool CheckAndSnapY(ref Vector3 worldPos, float threshold = 0.7f, float castHeight = 2)
    {
        Vector3 castOffset = new(0, castHeight / 2, 0);
        Vector3 castOrigin = worldPos + castOffset;
        if (Raycast(castOrigin, -Vector3.UnitY, out RaycastHit hit, castHeight))
        {
            var d = Vector3.Dot(hit.ComputeNormal(), Vector3.UnitY);
            if (d < threshold) return false;
            worldPos.Y = hit.Point.Y;
            worldPos = worldPos.Round();
            return true;
        }
        return false;
    }
}
