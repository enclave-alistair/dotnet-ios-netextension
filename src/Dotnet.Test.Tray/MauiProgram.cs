using CommunityToolkit.Maui;
using Plugin.LocalNotification;
using System.Diagnostics.CodeAnalysis;
using Dotnet.Test.Tray.Controls;
using Dotnet.Test.Tray.Pages;
using Dotnet.Test.Tray.Services;
using Dotnet.Test.Tray.ViewModels;

namespace Dotnet.Test.Tray;

[SuppressMessage("Interoperability", "CA1416", Justification = "Intentionally cross-platform")]
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fontawesome-webfont.ttf", "FontAwesome");
                fonts.AddFont("Roboto-Light.ttf", "RobotoLight");
                fonts.AddFont("Roboto-Regular.ttf", "Roboto");
                fonts.AddFont("SourceSansPro-Regular.otf", "SourceSansPro");
            });

        var fabricStatusProvider = FabricStatusProviderFactory.CreateStatusProvider();
        var fabricViewModel = new FabricViewModel(fabricStatusProvider);

        builder.Services.AddSingleton(fabricStatusProvider);
        builder.Services.AddSingleton(fabricViewModel);
        builder.Services.AddSingleton<DefaultHeaderView>();
        builder.Services.AddSingleton<HomeHeaderView>();
        builder.Services.AddSingleton<FabricPage>();
        builder.Services.AddSingleton<AppShell>();
#if ANDROID || IOS
        builder.UseLocalNotification();
#endif

        return builder.Build();
    }
}