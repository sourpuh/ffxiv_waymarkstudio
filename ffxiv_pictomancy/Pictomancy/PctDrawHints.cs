namespace Pictomancy;

///
/// <summary>
/// Hints for Pictomancy drawing.
/// These are called "hints" instead of "settings" because they may not always be respected.
/// </summary>
/// <param name="drawInCutscene">Draw during cutscenes.</param>
/// <param name="drawWhenFaded">Draw when the game is faded, such as loading a new area.</param>
/// <param name="autoDraw">Automatically draw to ImGui drawlist. If disabled, pictomancy only draws to a texture.</param>
/// <param name="maxAlpha">Max alpha value 0-255.</param>
/// <param name="alphaBlendMode">Alpha blend mode. Check AlphaBlendMode file for mode descriptions.</param>
/// <param name="clipNativeUI">Clip native UI if possible.</param>
/// 
public record struct PctDrawHints(
    bool drawInCutscene = false,
    bool drawWhenFaded = false,
    bool autoDraw = true,
    byte maxAlpha = 255,
    AlphaBlendMode alphaBlendMode = AlphaBlendMode.Add,
    bool clipNativeUI = true)
{
    // Surely there's a better way to do this?
    public PctDrawHints() : this(false, false, true, 255, AlphaBlendMode.Add, true) { }

    public bool DrawInCutscene => drawInCutscene;
    public bool DrawWhenFaded => drawWhenFaded;
    public bool AutoDraw => autoDraw;
    public float MaxAlphaFraction => maxAlpha / 255f;
    public AlphaBlendMode AlphaBlendMode => alphaBlendMode;
    public bool ClipNativeUI => clipNativeUI;
}
