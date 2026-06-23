using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;

namespace CampLedger.Services;

public static class ServiceHelper
{
    private static IServiceProvider? _services;

    public static IServiceProvider Services
    {
        get => _services ?? ResolveServices() ?? throw new InvalidOperationException("The MAUI service provider is not available yet.");
        set => _services = value;
    }

    public static T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    private static IServiceProvider? ResolveServices()
    {
        if (IPlatformApplication.Current?.Services is { } platformServices)
        {
            return platformServices;
        }

        if (Application.Current?.Handler?.MauiContext?.Services is { } mauiContextServices)
        {
            return mauiContextServices;
        }

        return _services;
    }
}
