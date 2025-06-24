using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System.Linq;
using WaymarkStudio.Triggers;
using WaymarkStudio.Windows;

namespace WaymarkStudio;

public sealed class Plugin : IDalamudPlugin
{
    public const string Tag = "Waymark Studio";

    [PluginService] internal static IDalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider Hooker { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    internal static FieldMarkerAddon FieldMarkerAddon { get; private set; } = null!;
    internal static Configuration Config { get; private set; } = null!;
    internal static PresetStorage Storage { get; private set; } = null!;
    internal static TriggerManager Triggers { get; private set; } = null!;
    internal static WaymarkManager WaymarkManager { get; private set; } = null!;
    internal static WaymarkVfx WaymarkVfx { get; private set; } = null!;
    internal static PctOverlay Overlay { get; private set; } = null!;
    internal readonly WindowSystem WindowSystem = new(Tag);
    internal static ConfigWindow ConfigWindow { get; private set; } = null!;
    internal static LibraryWindow LibraryWindow { get; private set; } = null!;
    internal static StudioWindow StudioWindow { get; private set; } = null!;
    internal static TriggerEditorWindow TriggerEditorWindow { get; private set; } = null!;

    private const string CommandName = "/wms";

    public Plugin()
    {
        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();

        FieldMarkerAddon = new();
        WaymarkManager = new();
        //WaymarkVfx = new();
        Storage = new();
        Triggers = new();

        ConfigWindow = new();
        WindowSystem.AddWindow(ConfigWindow);
        LibraryWindow = new();
        WindowSystem.AddWindow(LibraryWindow);
        StudioWindow = new();
        WindowSystem.AddWindow(StudioWindow);
        TriggerEditorWindow = new();
        WindowSystem.AddWindow(TriggerEditorWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open/close main window"
        });

        Overlay = new();

        OnTerritoryChange(ClientState.TerritoryType);
        ClientState.TerritoryChanged += OnTerritoryChange;
        Interface.UiBuilder.Draw += DrawUI;
        Interface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Interface.UiBuilder.OpenMainUi += ToggleMainUI;
        Framework.Update += Update;
    }

    public void Dispose()
    {
        FieldMarkerAddon.Dispose();
        //WaymarkVfx.Dispose();
        WindowSystem.RemoveAllWindows();

        Overlay.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ClientState.TerritoryChanged -= OnTerritoryChange;
        Interface.UiBuilder.Draw -= DrawUI;
        Interface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        Interface.UiBuilder.OpenMainUi -= ToggleMainUI;
        Framework.Update -= Update;
    }

    internal void Update(IFramework framework)
    {
        Triggers.Update();
        Storage.Update();
        //WaymarkVfx.Update();
    }

    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }
    private void OnTerritoryChange(ushort id)
    {
        if (DataManager.GetExcelSheet<TerritoryType>().TryGetRow(id, out var territory))
        {
            WaymarkManager.OnTerritoryChange(territory);
        }
        Overlay.OnTerritoryChange();
        Triggers.OnTerritoryChange();
        TriggerEditorWindow.IsOpen = false;
    }
    private void DrawUI() => WindowSystem.Draw();
    public static void ToggleConfigUI() => ConfigWindow.Toggle();
    public static void ToggleLibraryUI() => LibraryWindow.Toggle();
    public static void ToggleMainUI() => StudioWindow.Toggle();
    public static bool IsWPPInstalled() => Interface.InstalledPlugins.Where(x => x.InternalName == "WaymarkPresetPlugin" && x.IsLoaded).Any();
    public static bool IsMMInstalled() => Interface.InstalledPlugins.Where(x => x.InternalName == "MemoryMarker" && x.IsLoaded).Any();
}
