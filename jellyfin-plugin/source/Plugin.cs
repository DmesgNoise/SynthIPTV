using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace SynthIPTV.Jellyfin.Compatibility;

public sealed class Plugin : BasePlugin<BasePluginConfiguration>
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override Guid Id => new("3e6356c6-9b6c-4fd2-beb5-d7205de89c36");

    public override string Name => "SynthIPTV Jellyfin Compatibility";

    public override string Description =>
        "Applies SynthIPTV Live TV timestamp compatibility to Jellyfin FFmpeg launches.";
}
