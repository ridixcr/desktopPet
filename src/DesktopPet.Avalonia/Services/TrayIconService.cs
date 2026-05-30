using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Media.Imaging;
using global::Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DesktopPet.Avalonia.Services;

/// <summary>
/// Service for managing the system tray icon
/// </summary>
public class TrayIconService : IDisposable
{
    private TrayIcon _trayIcon;
    private NativeMenu _contextMenu;
    
    public event Action OnAddPetRequested;
    public event Action OnRemovePetRequested;
    public event Action OnOptionsRequested;
    public event Action OnAboutRequested;
    public event Action OnExitRequested;
    
    private string _petName = "eSheep";
    private string _author = "";
    private string _version = "";
    
    public TrayIconService()
    {
        CreateTrayIcon();
    }
    
    private void CreateTrayIcon()
    {
        _contextMenu = new NativeMenu();
        
        // Add pet
        var addItem = new NativeMenuItem($"Add {_petName}");
        addItem.Click += (s, e) => OnAddPetRequested?.Invoke();
        _contextMenu.Items.Add(addItem);
        
        // Remove pet
        var removeItem = new NativeMenuItem($"Remove {_petName}");
        removeItem.Click += (s, e) => OnRemovePetRequested?.Invoke();
        _contextMenu.Items.Add(removeItem);
        
        _contextMenu.Items.Add(new NativeMenuItemSeparator());
        
        // Options
        var optionsItem = new NativeMenuItem("Options");
        optionsItem.Click += (s, e) => OnOptionsRequested?.Invoke();
        _contextMenu.Items.Add(optionsItem);
        
        // About
        var aboutItem = new NativeMenuItem("About");
        aboutItem.Click += (s, e) => OnAboutRequested?.Invoke();
        _contextMenu.Items.Add(aboutItem);
        
        _contextMenu.Items.Add(new NativeMenuItemSeparator());
        
        // Exit
        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (s, e) => OnExitRequested?.Invoke();
        _contextMenu.Items.Add(exitItem);
        
        _trayIcon = new TrayIcon
        {
            ToolTipText = $"{_petName} Desktop Pet",
            Menu = _contextMenu,
            IsVisible = true
        };
        
        // Set default icon
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("DesktopPet.Avalonia.Assets.esheep.ico");
            if (stream != null)
            {
                _trayIcon.Icon = new WindowIcon(stream);
            }
        }
        catch (Exception ex)
        {
            StartUp.AddDebugInfo(StartUp.DebugType.Warning, $"Could not load default icon: {ex.Message}");
        }
    }
    
    public void SetIcon(MemoryStream iconStream, string petName, string author, string version)
    {
        _petName = petName;
        _author = author;
        _version = version;
        
        try
        {
            if (iconStream != null)
            {
                iconStream.Position = 0;
                _trayIcon.Icon = new WindowIcon(iconStream);
            }
            
            _trayIcon.ToolTipText = $"{petName} Desktop Pet";
            
            // Update menu items
            UpdateMenuItems();
        }
        catch (Exception ex)
        {
            StartUp.AddDebugInfo(StartUp.DebugType.Error, $"Error setting tray icon: {ex.Message}");
        }
    }
    
    private void UpdateMenuItems()
    {
        if (_contextMenu.Items.Count >= 2)
        {
            if (_contextMenu.Items[0] is NativeMenuItem addItem)
            {
                addItem.Header = $"Add {_petName}";
            }
            if (_contextMenu.Items[1] is NativeMenuItem removeItem)
            {
                removeItem.Header = $"Remove {_petName}";
            }
        }
    }
    
    public void ShowNotification(string title, string message)
    {
        // Avalonia doesn't have built-in balloon notifications
        // This would require platform-specific implementation
        StartUp.AddDebugInfo(StartUp.DebugType.Info, $"Notification: {title} - {message}");
    }
    
    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
