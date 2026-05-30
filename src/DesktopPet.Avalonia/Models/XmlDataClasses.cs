using System.Xml.Serialization;

namespace XmlData;

/// <summary>
/// Root node of the animation XML
/// </summary>
[XmlRoot("animations", Namespace = "https://esheep.petrucci.ch/", IsNullable = false)]
public class RootNode
{
    [XmlElement("header")]
    public HeaderNode Header;
    
    [XmlElement("image")]
    public ImageNode Image;
    
    [XmlElement("spawns")]
    public SpawnsNode Spawns;
    
    [XmlElement("animations")]
    public AnimationsNode Animations;
    
    [XmlElement("childs")]
    public ChildsNode Childs;
    
    [XmlElement("sounds")]
    public SoundsNode Sounds;
}

public class HeaderNode
{
    [XmlElement("author")]
    public string Author;
    
    [XmlElement("title")]
    public string Title;
    
    [XmlElement("petname")]
    public string Petname;
    
    [XmlElement("version")]
    public string Version;
    
    [XmlElement("info")]
    public string Info;
    
    [XmlElement("application")]
    public string Application;
    
    [XmlElement("icon")]
    public string Icon;
}

public class ImageNode
{
    [XmlElement("tilesx")]
    public int TilesX;
    
    [XmlElement("tilesy")]
    public int TilesY;
    
    [XmlElement("png")]
    public string Png;
    
    [XmlElement("transparency")]
    public string Transparency;
}

public class SpawnsNode
{
    [XmlElement("spawn")]
    public SpawnNode[] Spawn;
}

public class SpawnNode
{
    [XmlAttribute("probability")]
    public int Probability = 1;
    
    [XmlElement("x")]
    public string X;
    
    [XmlElement("y")]
    public string Y;
    
    [XmlElement("next")]
    public int Next;
}

public class AnimationsNode
{
    [XmlElement("animation")]
    public AnimationNode[] Animation;
}

public class AnimationNode
{
    [XmlAttribute("id")]
    public int Id;
    
    [XmlAttribute("name")]
    public string Name;
    
    [XmlElement("sequence")]
    public SequenceNode Sequence;
    
    [XmlElement("border")]
    public BorderNode Border;
    
    [XmlElement("gravity")]
    public GravityNode Gravity;
    
    [XmlElement("sound")]
    public int Sound = -1;
}

public class SequenceNode
{
    [XmlAttribute("repeatfrom")]
    public int RepeatFrom;
    
    [XmlAttribute("repeat")]
    public int RepeatCount;
    
    [XmlElement("frame")]
    public int[] Frame;
    
    [XmlElement("action")]
    public ActionNode[] Action;
    
    [XmlElement("next")]
    public NextNode[] Next;
}

public class ActionNode
{
    [XmlElement("x")]
    public string X;
    
    [XmlElement("y")]
    public string Y;
    
    [XmlElement("offsety")]
    public int OffsetY;
    
    [XmlElement("interval")]
    public int Interval = 100;
    
    [XmlElement("opacity")]
    public double Opacity = 1.0;
}

public class NextNode
{
    [XmlAttribute("probability")]
    public int Probability = 1;
    
    [XmlAttribute("only")]
    public string Only;
    
    [XmlText]
    public int Value;
}

public class BorderNode
{
    [XmlElement("next")]
    public NextNode[] Next;
}

public class GravityNode
{
    [XmlElement("next")]
    public NextNode[] Next;
}

public class ChildsNode
{
    [XmlElement("child")]
    public ChildNode[] Child;
}

public class ChildNode
{
    [XmlAttribute("animationid")]
    public int AnimationId;
    
    [XmlElement("x")]
    public string X;
    
    [XmlElement("y")]
    public string Y;
    
    [XmlElement("next")]
    public int Next;
}

public class SoundsNode
{
    [XmlElement("sound")]
    public SoundNode[] Sound;
}

public class SoundNode
{
    [XmlAttribute("id")]
    public int Id;
    
    [XmlElement("probability")]
    public int Probability = 100;
    
    [XmlElement("loop")]
    public bool Loop;
    
    [XmlElement("base64")]
    public string Base64;
}
