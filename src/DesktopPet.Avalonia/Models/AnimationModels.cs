using System;
using System.Collections.Generic;
using System.Data;
using Avalonia;

namespace DesktopPet.Avalonia.Models;

/// <summary>
/// Represents a dynamic value that can be computed from expressions
/// </summary>
public struct TValue
{
    public bool IsDynamic;
    public bool IsScreen;
    public string Compute;
    public int Value;

    public int GetValue(int screenIndex, XmlParser xml)
    {
        if (IsDynamic || IsScreen)
        {
            return xml?.ParseValue(Compute, "TValue.GetValue()", screenIndex) ?? Value;
        }
        return Value;
    }
    
    public static TValue FromString(string value, XmlParser xml)
    {
        var result = new TValue();
        
        if (string.IsNullOrEmpty(value))
        {
            result.Value = 0;
            return result;
        }
        
        value = value.Trim();
        
        // Check if it's a pure number
        if (int.TryParse(value, out int intValue))
        {
            result.Value = intValue;
            return result;
        }
        
        // Check if it contains dynamic keywords
        result.Compute = value;
        result.IsDynamic = value.Contains("random") || 
                          value.Contains("imageX") || 
                          value.Contains("imageY") ||
                          value.Contains("imageW") ||
                          value.Contains("imageH");
        result.IsScreen = value.Contains("screenW") || 
                         value.Contains("screenH") ||
                         value.Contains("areaW") ||
                         value.Contains("areaH");
        
        return result;
    }
}

/// <summary>
/// Animation movement step
/// </summary>
public struct TMovement
{
    public TValue X;
    public TValue Y;
    public TValue Interval;
    public int OffsetY;
    public double Opacity;
}

/// <summary>
/// Animation sequence frame
/// </summary>
public class TSequence
{
    public List<int> Frames { get; set; } = new List<int>();
    public List<TMovement> Movements { get; set; } = new List<TMovement>();
    public int RepeatFrom { get; set; } = 0;
    public int RepeatCount { get; set; } = 0;
    public int Interval { get; set; } = 100;
    
    public int TotalSteps
    {
        get
        {
            if (Frames.Count == 0) return 0;
            return Frames.Count + (RepeatCount * (Frames.Count - RepeatFrom));
        }
    }
    
    public int GetFrameIndex(int step)
    {
        if (Frames.Count == 0) return -1;
        
        if (step < Frames.Count)
        {
            return Frames[step];
        }
        
        // Handle repeat
        int repeatStep = (step - Frames.Count) % (Frames.Count - RepeatFrom);
        return Frames[RepeatFrom + repeatStep];
    }
    
    public TMovement GetMovement(int step)
    {
        if (Movements.Count == 0) return new TMovement();
        
        if (step < Movements.Count)
        {
            return Movements[step];
        }
        
        // Return last movement for extra steps
        return Movements[Movements.Count - 1];
    }
}

/// <summary>
/// Next animation information
/// </summary>
public struct TNextAnimation
{
    public enum TOnly
    {
        None = 0x7F,
        Taskbar = 0x01,
        Window = 0x02,
        Horizontal = 0x04,
        HorizontalWindow = 0x06,
        Vertical = 0x08,
    }
    
    public int Id;
    public int Probability;
    public TOnly Only;
}

/// <summary>
/// Spawn information
/// </summary>
public struct TSpawn
{
    public int Id;
    public int Probability;
    public TPosition Start;
    public int Next;
}

/// <summary>
/// Position with X and Y values
/// </summary>
public struct TPosition
{
    public TValue X;
    public TValue Y;
}

/// <summary>
/// Complete animation definition
/// </summary>
public class TAnimation
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TSequence Sequence { get; set; } = new TSequence();
    public List<TNextAnimation> NextAnimations { get; set; } = new List<TNextAnimation>();
    public List<TNextAnimation> BorderAnimations { get; set; } = new List<TNextAnimation>();
    public List<TNextAnimation> GravityAnimations { get; set; } = new List<TNextAnimation>();
    public int Sound { get; set; } = -1;
}

/// <summary>
/// Child animation (spawned by parent)
/// </summary>
public struct TChild
{
    public int Id;
    public TPosition Position;
    public int Next;
}
