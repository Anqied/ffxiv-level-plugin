using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System.Collections.Generic;

namespace TotalCharacterLevel;

public sealed class TotalCharacterLevel : IDalamudPlugin
{
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IUnlockState UnlockState { get; private set; } = null!;

    private static readonly uint[] Tanks = [1, 3, 32, 37];
    private static readonly uint[] Healers = [6, 33, 40];
    private static readonly uint[] Melees = [2, 4, 29, 34, 39, 41];
    private static readonly uint[] Pranges = [5, 31, 38];
    private static readonly uint[] Mranges = [7, 26, 35, 42, 36];
    private static readonly uint[] DoHs = [8, 9, 10, 11, 12, 13, 14, 15];
    private static readonly uint[] DoLs = [16, 17, 18];

    public TotalCharacterLevel(IDalamudPluginInterface pluginInterface)
    {
        AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "CharacterClass", RequestedUpdate);
    }
    private unsafe void RequestedUpdate(AddonEvent type, AddonArgs args)
    {
        // can't find character levels
        if (TotalLvlByCategory() is var levels && levels == null) return;

        // get ui nodes
        var addon = args.Addon;
        var atkUnitBase = (AtkUnitBase*)args.Addon.Address;
        if (atkUnitBase == null) return;

        // get text nodes and write text
        var tankText = (AtkTextNode*)atkUnitBase->GetNodeById(5);
        if (tankText != null) addLeveltoText(tankText, levels[0]);

        var healerText = (AtkTextNode*)atkUnitBase->GetNodeById(17);
        if (healerText != null) addLeveltoText(healerText, levels[1]);

        var meleeDPSText = (AtkTextNode*)atkUnitBase->GetNodeById(29);
        if (meleeDPSText != null) addLeveltoText(meleeDPSText, levels[2]);

        var PRangText = (AtkTextNode*)atkUnitBase->GetNodeById(45);
        if (PRangText != null) addLeveltoText(PRangText, levels[3]);

        var magicDPSText = (AtkTextNode*)atkUnitBase->GetNodeById(55);
        if (magicDPSText != null) addLeveltoText(magicDPSText, levels[4]);

        var DoHText = (AtkTextNode*)atkUnitBase->GetNodeById(69);
        if (DoHText != null) addLeveltoText(DoHText, levels[5]);
        var DoLText = (AtkTextNode*)atkUnitBase->GetNodeById(81);
        if (DoLText != null) addLeveltoText(DoLText, levels[6]);

        var button = atkUnitBase->GetNodeById(2)->GetComponent();
        if (button == null) return;

        var DoWDoMText = (AtkTextNode*)button->GetNodeById(6);
        if (DoWDoMText != null) addLeveltoText(DoWDoMText, levels[0] + levels[1] + levels[2] + levels[3] + levels[4]);
        var DoHDoLText = (AtkTextNode*)button->GetNodeById(7);
        if (DoHDoLText != null) addLeveltoText(DoHDoLText, levels[5] + levels[6]);
    }

    private unsafe void addLeveltoText(AtkTextNode* node, int level)
    {
        if (node==null) return;
        var text = new string(node->GetText());
        if (!string.IsNullOrEmpty(text) && text[^1] != ')')
            text += " (" + level + ")";
        node->SetText(text);
    }

    /// <summary>
    /// Fetches and sums the job levels of the current character, by category
    /// tank, healer, melee, physical ranged, magical ranged, DoH, DoL
    /// </summary>
    /// <returns>
    /// null if lookup failed, int array of level subtotals in order of 
    /// tank, healer, melee, physical ranged, magical ranged, DoH, DoL
    /// </returns>
    private static int[]? TotalLvlByCategory()
    {
        if (!PlayerState.IsLoaded) return null;

        ClassJob? scholar = null;

        var levels = new Dictionary<uint, short>();
        foreach (var job in DataManager.GetExcelSheet<ClassJob>())
        {
            if (job.Name.ToString() == "") continue;
            if (job.RowId == 28) scholar = job;
            var jobLevel = PlayerState.GetClassJobLevel(job);
            levels.Add(job.RowId, jobLevel);
        }

        var subtotals = new int[7];

        foreach (var key in Tanks)
        {
            if (!levels.TryGetValue(key, out var value)) continue;
            subtotals[0] += value;
        }
        foreach (var key in Healers)
        {
            if (!levels.TryGetValue(key, out var value)) continue;
            subtotals[1] += value;
        }
        // special check for Scholar (if unlock quest has been completed)
        if (scholar is not null && UnlockState.IsUnlockLinkUnlocked(((ClassJob)scholar).UnlockQuest.RowId)) {
            if (levels.TryGetValue(26, out var value)) subtotals[1] += value;
        }
        foreach (var key in Melees)
        {
            if (!levels.TryGetValue(key, out var value)) continue;
            subtotals[2] += value;
        }
        foreach (var key in Pranges)
        {
            if (!levels.TryGetValue(key, out var value)) continue;
            subtotals[3] += value;
        }
        foreach (var key in Mranges)
        {
            if (!levels.TryGetValue(key, out var value)) continue;
            subtotals[4] += value;
        }
        foreach (var key in DoHs)
        {
            if (!levels.TryGetValue(key, out var value)) continue;
            subtotals[5] += value;
        }
        foreach (var key in DoLs)
        {
            if (!levels.TryGetValue(key, out var value)) continue;
            subtotals[6] += value;
        }
        return subtotals;
    }
    public void Dispose()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate);
    }

}
