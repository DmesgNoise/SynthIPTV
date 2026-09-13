using System;
using System.Linq;
using MediaBrowser.Controller;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace SynthIPTV.Jellyfin.Compatibility;

/// <summary>
/// Replaces only Jellyfin's single ITranscodeManager registration with a
/// decorator. The decorator constructs Jellyfin's original descriptor and
/// delegates all stock behavior to it.
/// </summary>
public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(
        IServiceCollection serviceCollection,
        IServerApplicationHost applicationHost)
    {
        var originalDescriptor = serviceCollection.LastOrDefault(
            descriptor => descriptor.ServiceType == typeof(ITranscodeManager))
            ?? throw new InvalidOperationException(
                "Jellyfin ITranscodeManager registration was not found.");

        serviceCollection.Remove(originalDescriptor);

        serviceCollection.AddSingleton<ITranscodeManager>(serviceProvider =>
        {
            var inner = CreateOriginalTranscodeManager(
                serviceProvider,
                originalDescriptor);

            return ActivatorUtilities.CreateInstance<SynthTranscodeManager>(
                serviceProvider,
                inner);
        });
    }

    private static ITranscodeManager CreateOriginalTranscodeManager(
        IServiceProvider serviceProvider,
        ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is ITranscodeManager instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (ITranscodeManager)descriptor.ImplementationFactory(serviceProvider);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (ITranscodeManager)ActivatorUtilities.CreateInstance(
                serviceProvider,
                descriptor.ImplementationType);
        }

        throw new InvalidOperationException(
            "Unsupported Jellyfin ITranscodeManager service registration.");
    }
}
