using Avalonia.Threading;
using DesktopPet.Avalonia.Models;
using DesktopPet.Avalonia.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DesktopPet.Avalonia.Services;

/// <summary>
/// Main startup class that initializes and manages the pet application
/// </summary>
public sealed class StartUp : IDisposable
{
    public const int MaxPets = 16;
    
    public enum DebugType
    {
        Info = 1,
        Warning = 2,
        Error = 3
    }
    
    private static List<string> _debugMessages = new List<string>();
    private static bool _debugActive = false;
    
    private readonly TrayIconService _trayIconService;
    private readonly List<PetWindow> _pets = new List<PetWindow>();
    private Animations _animations;
    private XmlParser _xml;
    private LocalData.LocalData _localData;
    
    public StartUp(TrayIconService trayIconService)
    {
        _trayIconService = trayIconService;
        
        // Subscribe to tray icon events
        _trayIconService.OnAddPetRequested += AddPet;
        _trayIconService.OnRemovePetRequested += RemovePet;
        _trayIconService.OnExitRequested += Exit;
        _trayIconService.OnAboutRequested += ShowAbout;
    }
    
    public void Start()
    {
        try
        {
            // Initialize local data storage
            string storageFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "eSheep"
            );
            string exePath = Assembly.GetExecutingAssembly().Location;
            
            _localData = new LocalData.LocalData(storageFolder, exePath);
            
            // Load animations
            LoadAnimations();
            
            // Create initial pet
            AddPet();
            
            AddDebugInfo(DebugType.Info, "eSheep started successfully");
        }
        catch (Exception ex)
        {
            AddDebugInfo(DebugType.Error, $"Startup error: {ex.Message}");
        }
    }
    
    private void LoadAnimations()
    {
        _xml = new XmlParser();
        _animations = new Animations();
        
        // Try to load custom XML, fallback to default
        string xmlContent = _localData.GetXml();
        
        if (string.IsNullOrEmpty(xmlContent))
        {
            // Load default animation from embedded resource
            xmlContent = LoadDefaultAnimation();
        }
        
        if (_xml.ReadXml(xmlContent))
        {
            _xml.LoadAnimations(_animations);
            
            // Set tray icon
            var iconStream = _xml.GetIconStream();
            _trayIconService.SetIcon(
                iconStream,
                _xml.GetPetName(),
                _xml.GetAuthor(),
                _xml.GetVersion()
            );
        }
        else
        {
            AddDebugInfo(DebugType.Error, "Failed to load animations");
        }
    }
    
    private string LoadDefaultAnimation()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // List available resources for debugging
            var resourceNames = assembly.GetManifestResourceNames();
            AddDebugInfo(DebugType.Info, $"Available resources: {string.Join(", ", resourceNames)}");
            
            // Try different resource name patterns
            string[] possibleNames = {
                "DesktopPet.Avalonia.Assets.animations.xml",
                "eSheep.Assets.animations.xml",
                "Assets.animations.xml",
                "animations.xml"
            };
            
            foreach (var resourceName in possibleNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    AddDebugInfo(DebugType.Info, $"Loaded animation from resource: {resourceName}");
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            
            // Also try to find any resource containing "animations"
            foreach (var name in resourceNames)
            {
                if (name.Contains("animations", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = assembly.GetManifestResourceStream(name);
                    if (stream != null)
                    {
                        AddDebugInfo(DebugType.Info, $"Loaded animation from resource: {name}");
                        using var reader = new StreamReader(stream);
                        return reader.ReadToEnd();
                    }
                }
            }
            
            // Fallback: try to load from file next to executable
            string basePath = AppContext.BaseDirectory;
            string[] filePaths = {
                Path.Combine(basePath, "Assets", "animations.xml"),
                Path.Combine(basePath, "animations.xml"),
                Path.Combine(basePath, "..", "Assets", "animations.xml")
            };
            
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    AddDebugInfo(DebugType.Info, $"Loaded animation from file: {filePath}");
                    return File.ReadAllText(filePath);
                }
            }
            
            AddDebugInfo(DebugType.Warning, "No animation file found in resources or filesystem");
        }
        catch (Exception ex)
        {
            AddDebugInfo(DebugType.Warning, $"Could not load default animation: {ex.Message}");
        }
        
        return null;
    }
    
    public void AddPet()
    {
        if (_pets.Count >= MaxPets)
        {
            AddDebugInfo(DebugType.Warning, $"Maximum number of pets ({MaxPets}) reached");
            return;
        }
        
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var pet = new PetWindow(_animations, _xml);
                pet.Initialize(_animations.SpriteWidth, _animations.SpriteHeight);
                
                // Add all sprite images
                foreach (var sprite in _animations.Sprites)
                {
                    pet.AddImage(sprite);
                }
                
                _pets.Add(pet);
                pet.Play(true);
                
                AddDebugInfo(DebugType.Info, $"Pet added. Total: {_pets.Count}");
            }
            catch (Exception ex)
            {
                AddDebugInfo(DebugType.Error, $"Error adding pet: {ex.Message}");
            }
        });
    }
    
    public void RemovePet()
    {
        if (_pets.Count == 0)
        {
            return;
        }
        
        Dispatcher.UIThread.Post(() =>
        {
            var pet = _pets[_pets.Count - 1];
            _pets.RemoveAt(_pets.Count - 1);
            pet.Kill();
            
            AddDebugInfo(DebugType.Info, $"Pet removed. Total: {_pets.Count}");
        });
    }
    
    public void KillAllPets()
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var pet in _pets)
            {
                pet.Kill();
            }
            _pets.Clear();
        });
    }
    
    public void ShowAbout()
    {
        // TODO: Implement about dialog
        AddDebugInfo(DebugType.Info, $"About: {_xml?.GetTitle()} v{_xml?.GetVersion()} by {_xml?.GetAuthor()}");
    }
    
    public void Exit()
    {
        KillAllPets();
        
        Dispatcher.UIThread.Post(() =>
        {
            if (App.Current is App app)
            {
                app.Shutdown();
            }
        });
    }
    
    public static void AddDebugInfo(DebugType type, string message)
    {
        string prefix = type switch
        {
            DebugType.Info => "[INFO]",
            DebugType.Warning => "[WARN]",
            DebugType.Error => "[ERROR]",
            _ => "[???]"
        };
        
        string fullMessage = $"{DateTime.Now:HH:mm:ss} {prefix} {message}";
        _debugMessages.Add(fullMessage);
        
        // Keep only last 100 messages
        if (_debugMessages.Count > 100)
        {
            _debugMessages.RemoveAt(0);
        }
        
        // Write to console for debugging
        Console.WriteLine(fullMessage);
    }
    
    public static bool IsDebugActive() => _debugActive;
    
    public static void SetDebugActive(bool active) => _debugActive = active;
    
    public static IReadOnlyList<string> GetDebugMessages() => _debugMessages.AsReadOnly();
    
    public void Dispose()
    {
        KillAllPets();
        _animations?.Dispose();
        _xml?.Dispose();
    }
}
