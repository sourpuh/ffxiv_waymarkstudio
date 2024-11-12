using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Lumina.Excel.Sheets;
using System;
using WaymarkStudio.Windows;


namespace WaymarkStudio;

public sealed class Plugin : IDalamudPlugin
{
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
    internal static WaymarkManager WaymarkManager { get; private set; } = null!;
    internal static PctOverlay Overlay { get; private set; } = null!;
    internal readonly WindowSystem WindowSystem = new("Waymark Studio");
    internal static ConfigWindow ConfigWindow { get; private set; } = null!;
    internal static StudioWindow StudioWindow { get; private set; } = null!;

    private const string CommandName = "/wms";

    public Plugin()
    {
        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();

        FieldMarkerAddon = new();
        ConfigWindow = new();
        StudioWindow = new();
        WaymarkManager = new();
        Overlay = new();
        Storage = new();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(StudioWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open/close main window"
        });

        OnTerritoryChange(ClientState.TerritoryType);
        ClientState.TerritoryChanged += OnTerritoryChange;
        Interface.UiBuilder.Draw += DrawUI;
        Interface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Interface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        FieldMarkerAddon.Dispose();
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        StudioWindow.Dispose();
        Overlay.Dispose();

        CommandManager.RemoveHandler(CommandName);

        ClientState.TerritoryChanged -= OnTerritoryChange;
        Interface.UiBuilder.Draw -= DrawUI;
        Interface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        Interface.UiBuilder.OpenMainUi -= ToggleMainUI;
    }

    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }


    private void OnTerritoryChange(ushort mapId)
    {
        try
        {
            TerritoryType territory = DataManager.GetExcelSheet<TerritoryType>().GetRow(mapId);
            WaymarkManager.OnTerritoryChange(territory);
        }
        catch (ArgumentOutOfRangeException e)
        {
            Log.Error(e.ToString());
        }
#if DEBUG
        if (EventFramework.GetCurrentContentType() == FFXIVClientStructs.FFXIV.Client.Game.Event.ContentType.Party)
        {
            Chat.Print("This is party content! Check if waymarks can be saved then go update IsSafeToDirectPlacePreset");
        }
#endif
    }
    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => StudioWindow.Toggle();
}
