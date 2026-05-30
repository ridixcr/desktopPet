using global::Avalonia;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Markup.Xaml;
using DesktopPet.Avalonia.Services;
using DesktopPet.Avalonia.Views;
using System;
using AvaloniaShutdownMode = global::Avalonia.Controls.ShutdownMode;

namespace DesktopPet.Avalonia;

public partial class App : Application
{
    private StartUp _startUp;
    private TrayIconService _trayIconService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Initialize the tray icon service
            _trayIconService = new TrayIconService();
            
            // Initialize the main startup logic
            _startUp = new StartUp(_trayIconService);
            
            // Don't show a main window - the pet windows are created by StartUp
            desktop.ShutdownMode = AvaloniaShutdownMode.OnExplicitShutdown;
            
            // Start the application
            _startUp.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void Shutdown()
    {
        _startUp?.Dispose();
        _trayIconService?.Dispose();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
