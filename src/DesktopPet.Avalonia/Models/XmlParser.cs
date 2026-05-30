using Avalonia.Media.Imaging;
using DesktopPet.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DesktopPet.Avalonia.Models;

/// <summary>
/// XML parser for animation files
/// </summary>
public class XmlParser : IDisposable
{
    public XmlData.RootNode AnimationXml { get; private set; }
    public string AnimationXmlString { get; private set; }
    
    public int ParentX { get; set; } = -1;
    public int ParentY { get; set; } = -1;
    public bool ParentFlipped { get; set; } = false;
    
    private int _randomSpawn;
    private readonly int _scale;
    private readonly DataTable _dataTable;
    
    // Screen dimensions (updated when needed)
    private int _screenWidth = 1920;
    private int _screenHeight = 1080;
    private int _areaWidth = 1920;
    private int _areaHeight = 1040;
    private int _imageWidth = 64;
    private int _imageHeight = 64;
    
    public XmlParser(int scaleFactor = 1)
    {
        _scale = scaleFactor;
        _randomSpawn = new Random().Next(10, 90);
        _dataTable = new DataTable();
        _dataTable.Locale = CultureInfo.InvariantCulture;
        _dataTable.Columns.Add("Expression", typeof(string), "");
    }
    
    public void SetScreenSize(int width, int height, int areaWidth, int areaHeight)
    {
        _screenWidth = width;
        _screenHeight = height;
        _areaWidth = areaWidth;
        _areaHeight = areaHeight;
    }
    
    public void SetImageSize(int width, int height)
    {
        _imageWidth = width;
        _imageHeight = height;
    }
    
    public bool ReadXml(string xmlContent)
    {
        try
        {
            AnimationXmlString = xmlContent;
            
            var serializer = new XmlSerializer(typeof(XmlData.RootNode));
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(xmlContent);
            writer.Flush();
            stream.Position = 0;
            
            AnimationXml = (XmlData.RootNode)serializer.Deserialize(stream);
            return true;
        }
        catch (Exception ex)
        {
            StartUp.AddDebugInfo(StartUp.DebugType.Error, $"Error parsing XML: {ex.Message}");
            return false;
        }
    }
    
    public int ParseValue(string expression, string errorContext, int screenIndex = -1)
    {
        if (string.IsNullOrEmpty(expression))
            return 0;
            
        try
        {
            // Replace keywords
            expression = expression
                .Replace("screenW", _screenWidth.ToString())
                .Replace("screenH", _screenHeight.ToString())
                .Replace("areaW", _areaWidth.ToString())
                .Replace("areaH", _areaHeight.ToString())
                .Replace("imageW", (_imageWidth * _scale).ToString())
                .Replace("imageH", (_imageHeight * _scale).ToString())
                .Replace("imageX", ParentX.ToString())
                .Replace("imageY", ParentY.ToString())
                .Replace("random", new Random().Next(0, 100).ToString())
                .Replace("randS", _randomSpawn.ToString());
            
            // Evaluate expression
            _dataTable.Columns["Expression"].Expression = expression;
            var row = _dataTable.NewRow();
            _dataTable.Rows.Add(row);
            var value = row["Expression"];
            _dataTable.Rows.Remove(row);
            
            // Handle both integer and decimal results
            if (value is double d)
                return (int)d;
            if (value is decimal m)
                return (int)m;
            if (value is int i)
                return i;
            // Try to parse as double first (handles decimal strings)
            if (double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double dv))
                return (int)dv;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            StartUp.AddDebugInfo(StartUp.DebugType.Warning, 
                $"Error evaluating '{expression}' in {errorContext}: {ex.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Parse a string value that may be an integer or an expression
    /// </summary>
    private int ParseIntValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;
            
        // Try to parse as integer first
        if (int.TryParse(value, out int intValue))
            return intValue;
            
        // Otherwise evaluate as expression
        return ParseValue(value, "ParseIntValue");
    }
    
    public void LoadAnimations(Animations animations)
    {
        if (AnimationXml == null)
        {
            StartUp.AddDebugInfo(StartUp.DebugType.Error, "No XML loaded");
            return;
        }
        
        animations.Xml = this;
        
        // Load sprites
        if (AnimationXml.Image != null)
        {
            animations.LoadSprites(
                AnimationXml.Image.Png,
                AnimationXml.Image.TilesX,
                AnimationXml.Image.TilesY,
                AnimationXml.Image.Transparency ?? ""
            );
            
            SetImageSize(animations.SpriteWidth, animations.SpriteHeight);
        }
        
        // Load spawns
        if (AnimationXml.Spawns?.Spawn != null)
        {
            int spawnId = 0;
            foreach (var spawn in AnimationXml.Spawns.Spawn)
            {
                var tSpawn = new TSpawn
                {
                    Id = spawnId++,
                    Probability = spawn.Probability,
                    Start = new TPosition
                    {
                        X = TValue.FromString(spawn.X, this),
                        Y = TValue.FromString(spawn.Y, this)
                    },
                    Next = spawn.Next?.Value ?? 0
                };
                animations.AddSpawn(tSpawn);
            }
        }
        
        // Load animations
        if (AnimationXml.Animations?.Animation != null)
        {
            foreach (var anim in AnimationXml.Animations.Animation)
            {
                var tAnim = ParseAnimation(anim);
                animations.AddAnimation(tAnim);
            }
        }
        
        // Load childs
        if (AnimationXml.Childs?.Child != null)
        {
            int childId = 0;
            foreach (var child in AnimationXml.Childs.Child)
            {
                var tChild = new TChild
                {
                    Id = childId++,
                    Position = new TPosition
                    {
                        X = TValue.FromString(child.X, this),
                        Y = TValue.FromString(child.Y, this)
                    },
                    Next = child.Next
                };
                animations.AddChild(tChild);
            }
        }
        
        StartUp.AddDebugInfo(StartUp.DebugType.Info, 
            $"Loaded {animations.SheepAnimations.Count} animations, {animations.SheepSpawns.Count} spawns");
    }
    
    private TAnimation ParseAnimation(XmlData.AnimationNode anim)
    {
        var result = new TAnimation
        {
            Id = anim.Id,
            Name = anim.Name ?? $"Animation_{anim.Id}"
        };
        
        // Parse sequence
        if (anim.Sequence != null)
        {
            result.Sequence = new TSequence
            {
                RepeatFrom = ParseIntValue(anim.Sequence.RepeatFrom),
                RepeatCount = ParseIntValue(anim.Sequence.RepeatCount)
            };
            
            if (anim.Sequence.Frame != null)
            {
                foreach (var frame in anim.Sequence.Frame)
                {
                    result.Sequence.Frames.Add(frame);
                }
            }
            
            if (anim.Sequence.Action != null)
            {
                foreach (var action in anim.Sequence.Action)
                {
                    var movement = new TMovement
                    {
                        X = TValue.FromString(action.X, this),
                        Y = TValue.FromString(action.Y, this),
                        Interval = TValue.FromString(action.Interval > 0 ? action.Interval.ToString() : "100", this),
                        OffsetY = action.OffsetY,
                        Opacity = action.Opacity > 0 ? action.Opacity : 1.0
                    };
                    result.Sequence.Movements.Add(movement);
                }
                
                // Set default interval from first action
                if (result.Sequence.Movements.Count > 0)
                {
                    result.Sequence.Interval = result.Sequence.Movements[0].Interval.Value;
                }
            }
        }
        
        // Parse next animations
        if (anim.Sequence?.Next != null)
        {
            foreach (var next in anim.Sequence.Next)
            {
                var tNext = new TNextAnimation
                {
                    Id = next.Value,
                    Probability = next.Probability,
                    Only = TNextAnimation.TOnly.None
                };
                result.NextAnimations.Add(tNext);
            }
        }
        
        // Parse border animations
        if (anim.Border?.Next != null)
        {
            foreach (var next in anim.Border.Next)
            {
                var tNext = new TNextAnimation
                {
                    Id = next.Value,
                    Probability = next.Probability,
                    Only = TNextAnimation.TOnly.None
                };
                result.BorderAnimations.Add(tNext);
            }
        }
        
        // Parse gravity animations
        if (anim.Gravity?.Next != null)
        {
            foreach (var next in anim.Gravity.Next)
            {
                var tNext = new TNextAnimation
                {
                    Id = next.Value,
                    Probability = next.Probability,
                    Only = TNextAnimation.TOnly.None
                };
                result.GravityAnimations.Add(tNext);
            }
        }
        
        return result;
    }
    
    public MemoryStream GetIconStream()
    {
        if (AnimationXml?.Header?.Icon != null)
        {
            try
            {
                byte[] iconBytes = Convert.FromBase64String(AnimationXml.Header.Icon);
                return new MemoryStream(iconBytes);
            }
            catch (Exception ex)
            {
                StartUp.AddDebugInfo(StartUp.DebugType.Warning, $"Error loading icon: {ex.Message}");
            }
        }
        return null;
    }
    
    public string GetTitle() => AnimationXml?.Header?.Title ?? "eSheep";
    public string GetAuthor() => AnimationXml?.Header?.Author ?? "Unknown";
    public string GetVersion() => AnimationXml?.Header?.Version ?? "1.0";
    public string GetPetName() => AnimationXml?.Header?.Petname ?? "eSheep";
    public string GetInfo() => AnimationXml?.Header?.Info ?? "";
    
    public void Dispose()
    {
        AnimationXml = null;
        AnimationXmlString = null;
    }
}
