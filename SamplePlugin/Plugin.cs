using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text.Payloads;
using SamplePlugin.Windows;
using System;
using System.IO;
using Dalamud.Game.Config;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{



    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }
    //private MainWindow MainWindow { get; init; }
    private bool TooltipActive = false;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);

        Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"], PreReceiveEvent);

        WindowSystem.AddWindow(ConfigWindow);

        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");

    }


    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        //MainWindow.Dispose();
        Services.AddonLifecycle.UnregisterListener(PreReceiveEvent);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        ConfigWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();

    private unsafe void PreReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (!GameConfig.TryGet(UiConfigOption.LogCrossWorldName, out bool value)) //settings check failed
            return;
        if (value) //setting set to show world names
            return;
        if (args is not AddonReceiveEventArgs eventArgs)
            return;
        var id = eventArgs.Addon.Id;
        if (id == 0) //null pointer somehow
            return; 
        if (eventArgs.AtkEventType == (int)AtkEventType.LinkMouseOver)
        {
            if (eventArgs.AtkEventData == IntPtr.Zero) return; //if no info, return
            var linkData = ((LinkData**)eventArgs.AtkEventData)[0]; //get link data
            if (linkData == null || linkData->LinkType != (byte)LinkMacroPayloadType.Character) return;

            uint worldId = (uint)linkData->IntValue2; // IntValue2 of character link is world id
            var world = Services.DataManager.Excel.GetSheet<World>().GetRowOrDefault(worldId); 
            if (world == null) return;
            if (PlayerState.HomeWorld.RowId == worldId) return;

            Services.Log.Info($"Hovered player from {world?.Name}");
            AtkUnitBase* ptr = (AtkUnitBase*)eventArgs.Addon.Address;
            AtkStage.Instance()->TooltipManager.ShowTooltip(id, ptr->CursorTarget, world?.Name.ToString());
        }
        else if (eventArgs.AtkEventType == (int)AtkEventType.LinkMouseOut)
        {
            Services.Log.Info($"Stopped hovering");
            AtkStage.Instance()->TooltipManager.HideTooltip(id);
        }
    }

}
