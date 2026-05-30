using Avalonia.Media.Imaging;
using DesktopPet.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvaloniaPixelRect = global::Avalonia.PixelRect;
using AvaloniaPixelSize = global::Avalonia.PixelSize;
using AvaloniaRect = global::Avalonia.Rect;
using AvaloniaVector = global::Avalonia.Vector;

namespace DesktopPet.Avalonia.Models;

/// <summary>
/// Manages all animation data and provides methods to get animations
/// </summary>
public class Animations
{
    public Dictionary<int, TAnimation> SheepAnimations { get; } = new Dictionary<int, TAnimation>();
    public Dictionary<int, TSpawn> SheepSpawns { get; } = new Dictionary<int, TSpawn>();
    public Dictionary<int, TChild> SheepChilds { get; } = new Dictionary<int, TChild>();
    
    public int AnimationKill { get; set; } = -1;
    public int AnimationSync { get; set; } = -1;
    public int AnimationDrag { get; set; } = -1;
    public int AnimationFall { get; set; } = -1;
    
    public List<Bitmap> Sprites { get; } = new List<Bitmap>();
    public int SpriteWidth { get; set; }
    public int SpriteHeight { get; set; }
    
    private readonly Random _random = new Random();
    
    public XmlParser Xml { get; set; }
    
    public void AddAnimation(TAnimation animation)
    {
        SheepAnimations[animation.Id] = animation;
        
        // Check for special animations
        var nameLower = animation.Name.ToLowerInvariant();
        if (nameLower == "kill" || nameLower == "bye")
        {
            AnimationKill = animation.Id;
        }
        else if (nameLower == "sync")
        {
            AnimationSync = animation.Id;
        }
        else if (nameLower == "drag" || nameLower == "grab")
        {
            AnimationDrag = animation.Id;
        }
        else if (nameLower == "fall" || nameLower == "falling")
        {
            AnimationFall = animation.Id;
        }
    }
    
    public void AddSpawn(TSpawn spawn)
    {
        SheepSpawns[spawn.Id] = spawn;
    }
    
    public void AddChild(TChild child)
    {
        SheepChilds[child.Id] = child;
    }
    
    public TSpawn GetRandomSpawn()
    {
        if (SheepSpawns.Count == 0)
        {
            return new TSpawn();
        }
        
        int totalProbability = SheepSpawns.Values.Sum(s => s.Probability);
        int randomValue = _random.Next(totalProbability);
        int currentSum = 0;
        
        foreach (var spawn in SheepSpawns.Values)
        {
            currentSum += spawn.Probability;
            if (randomValue < currentSum)
            {
                return spawn;
            }
        }
        
        return SheepSpawns.Values.First();
    }
    
    public TSpawn GetSpawnByIndex(int index)
    {
        var keys = SheepSpawns.Keys.ToList();
        if (index >= 0 && index < keys.Count)
        {
            return SheepSpawns[keys[index]];
        }
        return GetRandomSpawn();
    }
    
    public int GetNextAnimation(int currentId, bool checkNext, bool checkBorder, bool checkGravity)
    {
        if (!SheepAnimations.ContainsKey(currentId))
        {
            return -1;
        }
        
        var current = SheepAnimations[currentId];
        List<TNextAnimation> candidates = new List<TNextAnimation>();
        
        if (checkNext && current.NextAnimations.Count > 0)
        {
            candidates.AddRange(current.NextAnimations);
        }
        
        if (checkBorder && current.BorderAnimations.Count > 0)
        {
            candidates.AddRange(current.BorderAnimations);
        }
        
        if (checkGravity && current.GravityAnimations.Count > 0)
        {
            candidates.AddRange(current.GravityAnimations);
        }
        
        if (candidates.Count == 0 && current.NextAnimations.Count > 0)
        {
            candidates.AddRange(current.NextAnimations);
        }
        
        if (candidates.Count == 0)
        {
            return -1;
        }
        
        // Select based on probability
        int totalProbability = candidates.Sum(n => n.Probability);
        int randomValue = _random.Next(totalProbability);
        int currentSum = 0;
        
        foreach (var next in candidates)
        {
            currentSum += next.Probability;
            if (randomValue < currentSum)
            {
                return next.Id;
            }
        }
        
        return candidates[0].Id;
    }
    
    public List<TNextAnimation> GetNextAnimations(int currentId, bool next, bool border, bool gravity)
    {
        var result = new List<TNextAnimation>();
        
        if (!SheepAnimations.ContainsKey(currentId))
        {
            return result;
        }
        
        var current = SheepAnimations[currentId];
        
        if (next) result.AddRange(current.NextAnimations);
        if (border) result.AddRange(current.BorderAnimations);
        if (gravity) result.AddRange(current.GravityAnimations);
        
        return result;
    }
    
    public void LoadSprites(string base64Image, int tilesX, int tilesY, string transparency)
    {
        try
        {
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            using var stream = new MemoryStream(imageBytes);
            using var fullImage = new Bitmap(stream);
            
            SpriteWidth = fullImage.PixelSize.Width / tilesX;
            SpriteHeight = fullImage.PixelSize.Height / tilesY;
            
            // Split into individual sprites
            for (int y = 0; y < tilesY; y++)
            {
                for (int x = 0; x < tilesX; x++)
                {
                    var cropRect = new AvaloniaPixelRect(
                        x * SpriteWidth, 
                        y * SpriteHeight, 
                        SpriteWidth, 
                        SpriteHeight
                    );
                    
                    // Create cropped bitmap
                    var cropped = CropBitmap(fullImage, cropRect);
                    Sprites.Add(cropped);
                }
            }
            
            StartUp.AddDebugInfo(StartUp.DebugType.Info, 
                $"Loaded {Sprites.Count} sprites ({SpriteWidth}x{SpriteHeight})");
        }
        catch (Exception ex)
        {
            StartUp.AddDebugInfo(StartUp.DebugType.Error, $"Error loading sprites: {ex.Message}");
        }
    }
    
    private Bitmap CropBitmap(Bitmap source, AvaloniaPixelRect rect)
    {
        // Create a render target bitmap for the cropped image
        var cropped = new RenderTargetBitmap(
            new AvaloniaPixelSize(rect.Width, rect.Height),
            new AvaloniaVector(96, 96)
        );
        
        using (var ctx = cropped.CreateDrawingContext())
        {
            ctx.DrawImage(source, 
                new AvaloniaRect(rect.X, rect.Y, rect.Width, rect.Height),
                new AvaloniaRect(0, 0, rect.Width, rect.Height));
        }
        
        return cropped;
    }
    
    public void Dispose()
    {
        foreach (var sprite in Sprites)
        {
            sprite?.Dispose();
        }
        Sprites.Clear();
        SheepAnimations.Clear();
        SheepSpawns.Clear();
        SheepChilds.Clear();
    }
}
