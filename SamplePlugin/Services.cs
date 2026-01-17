using Dalamud.IoC;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SamplePlugin
{
    public sealed class Services
    {
        [PluginService] public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
        [PluginService] public static IDataManager DataManager { get; set; } = null!;
        [PluginService] public static IPluginLog Log { get; set; } = null!;
    }
}
