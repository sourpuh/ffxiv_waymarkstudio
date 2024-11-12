# FFXIV Pictomancy
Pictomancy is a library for drawing 3D world overlays in Dalamud plugins.
Pictomancy has an ImGui-like interface that operates in world space instead of a 2D canvas.
Pictomancy simplifies the hard parts of 3D overlays by correctly clipping objects behind the camera and clipping around the native UI.

# Pictomancy is still in development and does not have a stable API. Use at your own risk.

## Installation
Use as a git sub-module.
```bash
git submodule add https://github.com/sourpuh/ffxiv_pictomancy
```

## Use
Library initialization:
```c#
public MyPlugin(DalamudPluginInterface pluginInterface)
{
    PictoService.Initialize(pluginInterface);

    ... Your Code Here ...
}

public void Dispose()
{
    PictoService.Dispose();

    ... Your Code Here ...
}
```

Drawing various shapes:
```c#
using (var drawList = PictoService.Draw())
{
    if (drawList == null)
        return;
    // Draw a circle around a GameObject's hitbox
    Vector3 worldPosition = gameObject.Position;
    float radius = gameObject.HitboxRadius;
    drawList.AddCircleFilled(worldPosition, radius, fillColor);
    drawList.AddCircle(worldPosition, radius, outlineColor);
}
```
