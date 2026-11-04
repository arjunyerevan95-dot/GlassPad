// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GlassPad.Engine;

public readonly record struct Vec(double X, double Y)
{
    public double Length => Math.Sqrt(X * X + Y * Y);
    public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec operator *(Vec a, double b) => new(a.X * b, a.Y * b);
    public Vec Clamp() => Length > 1 ? this * (1 / Length) : this;
}
public sealed record Zone(double X, double Y, double Width, double Height)
{
    public bool Contains(Vec p) => p.X >= X && p.Y >= Y && p.X <= X + Width && p.Y <= Y + Height;
}
public sealed class ActionBinding
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Output { get; set; } = "";
    public string Icon { get; set; } = "sword";
    public string Interaction { get; set; } = "Hold";
    public bool DragLook { get; set; }
    public string SlideAction { get; set; } = "";
}
public sealed class Control
{
    public string Id { get; set; } = "";
    public string Action { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; } = .10;
    public double HitScale { get; set; } = 1.25;
    public double Opacity { get; set; } = .8;
    public bool Visible { get; set; } = true;
    public bool Label { get; set; }
}
public sealed class LookCalibration
{
    public double Horizontal { get; set; } = 1;
    public double Vertical { get; set; } = 1;
    public double FullSpeed { get; set; } = 900;
    public double Response { get; set; } = 1;
    public double GameDeadzone { get; set; } = .18;
    public bool InvertX { get; set; }
    public bool InvertY { get; set; }
    public string Verification { get; set; } = "Uncalibrated: verify game sensitivity and maximum turn rate";
}
public sealed class GyroCalibration
{
    public double Sensitivity { get; set; } = 1;
    public double Deadzone { get; set; } = .5;
    public string Mode { get; set; } = "Always";
    public double BiasX { get; set; }
    public double BiasY { get; set; }
    public string YawAxis { get; set; } = "Y";
    public string PitchAxis { get; set; } = "X";
    public bool InvertX { get; set; }
    public bool InvertY { get; set; }
    public string ScreenOrientation { get; set; } = "Landscape";
    public string CalibratedAt { get; set; } = "";
}
public sealed class GameProfile
{
    public int Version { get; set; } = 1;
    public string Id { get; set; } = "queens-domain-draft";
    public string Title { get; set; } = "Queen’s Domain";
    public string Approval { get; set; } = "Draft";
    public string Backend { get; set; } = "XInput";
    public string Executable { get; set; } = "QueensDomain-Win64-Shipping.exe";
    public string ExecutableSha256 { get; set; } = "8D805CFF01753F9066BF60E4432A02D91533E4741AC0EAF1649B3C1CD17EE9CF";
    public double Opacity { get; set; } = .78;
    public Zone MoveZone { get; set; } = new(.02, .28, .42, .68);
    public Zone LookZone { get; set; } = new(.46, .10, .52, .86);
    public double MoveRadius { get; set; } = .115;
    public double MoveDeadzone { get; set; } = .10;
    public string Sprint { get; set; } = "Disabled";
    public double SprintOuter { get; set; } = .94;
    public double SprintInner { get; set; } = .78;
    public double DoubleTapMs { get; set; } = 280;
    public double SlideThreshold { get; set; } = .025;
    public double RadialX { get; set; } = .74;
    public double RadialY { get; set; } = .51;
    public double RadialRadius { get; set; } = .21;
    public LookCalibration Look { get; set; } = new();
    public GyroCalibration Gyro { get; set; } = new();
    public List<ActionBinding> Actions { get; set; } = [];
    public List<Control> Controls { get; set; } = [];
    public List<string> Radial { get; set; } = ["special", "throw", "item", "equipment", "status", "gyro", "more"];

    public static readonly HashSet<string> Icons = ["sword", "secondary", "jump", "dash", "hand", "throw", "item", "status", "radial", "settings", "pause", "special", "run", "gyro", "up", "down", "left", "right", "confirm", "back"];
    public static readonly HashSet<string> Outputs = ["A", "B", "X", "Y", "LB", "RB", "LT", "RT", "L3", "R3", "Start", "Back", "Up", "Down", "Left", "Right"];
    public static readonly HashSet<string> Commands = ["actions", "equipment", "gyro", "gyroStart", "more", "customize", "profiles", "menu", "hide", "exit", "cancel", "recover"];
    public ActionBinding? Binding(string id) => Actions.FirstOrDefault(x => x?.Id == id);
    public GameProfile Clone() => ProfileStore.Parse(ProfileStore.Serialize(this));
    public static GameProfile QueensDraft()
    {
        var p = new GameProfile();
        void Add(string id, string label, string output, string icon, bool drag = false, string interaction = "Hold") => p.Actions.Add(new() { Id=id, Label=label, Output=output, Icon=icon, DragLook=drag, Interaction=interaction });
        Add("primary", "Primary attack", "RT", "sword", true);
        Add("secondary", "Secondary attack", "LT", "secondary", true);
        Add("jump", "Jump", "A", "jump"); Add("dash", "Wind dash", "RB", "dash");
        Add("interact", "Interact", "X", "hand"); Add("special", "Special attack", "Y", "special", true);
        Add("throw", "Throw", "LB", "throw", true); Add("item", "Use item", "B", "item");
        Add("pause", "Pause", "Start", "pause", false, "Tap"); Add("status", "Status", "Back", "status", false, "Tap");
        Add("run", "Run", "L3", "run", false, "Tap");
        Add("nextThrow", "Next throwing weapon", "Left", "throw", false, "Tap");
        Add("nextWeapon", "Next weapon", "Right", "sword", false, "Tap");
        Add("nextItem", "Next item", "Up", "item", false, "Tap");
        Add("previousItem", "Previous item", "Down", "item", false, "Tap");
        void Place(string id, string action, double x, double y, double size, bool visible=true) => p.Controls.Add(new(){Id=id,Action=action,X=x,Y=y,Size=size,Visible=visible});
        Place("primary", "primary", .886, .795, .145); Place("secondary", "secondary", .780, .862, .091);
        Place("jump", "jump", .937, .612, .094); Place("dash", "dash", .829, .633, .091);
        Place("interact", "interact", .960, .437, .069); Place("actions", "actions", .701, .879, .071);
        Place("pause", "pause", .962, .065, .049); Place("special", "special", .742, .665, .068, false);
        Place("run", "run", .060, .430, .065, false);
        return p;
    }
}

public static class ProfileStore
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented=true, PropertyNameCaseInsensitive=true, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
    public static string Serialize(GameProfile p) => JsonSerializer.Serialize(p, Json);
    public static GameProfile Parse(string json)
    {
        if (json.Length > 256_000) throw new InvalidDataException("Profile exceeds 256 KB");
        using(var document=JsonDocument.Parse(json))
        {
            if(document.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Profile must be a JSON object");
            foreach(var key in new[]{"Version","Id","Title","Approval","Backend","Executable","ExecutableSha256","Actions","Controls","Radial","MoveZone","LookZone","Look","Gyro"})
                if(!document.RootElement.EnumerateObject().Any(p=>p.Name.Equals(key,StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Missing profile field: "+key);
        }
        var p = JsonSerializer.Deserialize<GameProfile>(json, Json) ?? throw new InvalidDataException("Empty profile");
        var errors = Validate(p); if(errors.Count > 0) throw new InvalidDataException(string.Join("\n", errors)); return p;
    }
    public static List<string> Validate(GameProfile p)
    {
        var errors = new List<string>();
        void Range(double n,double min,double max,string name){if(!double.IsFinite(n)||n<min||n>max)errors.Add($"{name}: expected {min}–{max}");}
        if(p.Version != 1) errors.Add("Unsupported profile version");
        if(p.Backend != "XInput") errors.Add("Only XInput output is implemented in this build");
        if(string.IsNullOrWhiteSpace(p.Id)||p.Id.Length>80||p.Id.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'&&c!='_'))errors.Add("Invalid stable profile ID");
        if(string.IsNullOrWhiteSpace(p.Title)||p.Title.Length>120||p.Title.Any(char.IsControl))errors.Add("Invalid profile title");
        if(p.Approval is not ("Draft" or "Approved"))errors.Add("Approval must be Draft or Approved");
        if(string.IsNullOrWhiteSpace(p.Executable)||Path.GetFileName(p.Executable)!=p.Executable)errors.Add("Executable must be a filename");
        if(p.ExecutableSha256 is null||p.ExecutableSha256.Length!=64||p.ExecutableSha256.Any(c=>!Uri.IsHexDigit(c)))errors.Add("Executable SHA256 required for identity matching");
        if(p.Actions is null||p.Controls is null||p.Radial is null||p.Look is null||p.Gyro is null||p.MoveZone is null||p.LookZone is null){errors.Add("Required collection/calibration is null");return errors;}
        if(p.Actions.Count>64||p.Controls.Count>40||p.Radial.Count is <1 or >8)errors.Add("Too many actions/controls or invalid radial count");
        Range(p.Opacity,.15,1,"Global opacity"); Range(p.MoveRadius,.03,.3,"Movement radius"); Range(p.MoveDeadzone,0,.5,"Movement deadzone");
        Range(p.SprintOuter,.5,1,"Sprint outer"); Range(p.SprintInner,.1,p.SprintOuter-.05,"Sprint hysteresis"); Range(p.DoubleTapMs,100,500,"Double tap ms"); Range(p.SlideThreshold,.005,.15,"Slide threshold");
        if(p.Sprint is not ("Disabled" or "DoubleTap" or "OuterHold"))errors.Add("Unknown sprint gesture");
        foreach(var (z,name) in new[]{(p.MoveZone,"Move zone"),(p.LookZone,"Look zone")}){Range(z.X,0,1,name);Range(z.Y,0,1,name);Range(z.Width,.1,1-z.X,name);Range(z.Height,.1,1-z.Y,name);}
        Range(p.RadialRadius,.12,.3,"Radial radius");Range(p.RadialX,.18,.82,"Radial X");Range(p.RadialY,.28,.72,"Radial Y");
        Range(p.Look.Horizontal,.05,8,"Horizontal sensitivity");Range(p.Look.Vertical,.05,8,"Vertical sensitivity");Range(p.Look.FullSpeed,100,4000,"Full-speed velocity");Range(p.Look.Response,.3,3,"Response");Range(p.Look.GameDeadzone,0,.5,"Game deadzone");
        Range(p.Gyro.Sensitivity,.1,8,"Gyro sensitivity");Range(p.Gyro.Deadzone,0,10,"Gyro deadzone");Range(p.Gyro.BiasX,-100,100,"Gyro bias X");Range(p.Gyro.BiasY,-100,100,"Gyro bias Y");
        if(p.Gyro.Mode is not ("Always" or "TouchingLook"))errors.Add("Unknown gyro mode");
        if(p.Gyro.YawAxis is not ("X" or "Y" or "Z")||p.Gyro.PitchAxis is not ("X" or "Y" or "Z"))errors.Add("Unknown gyro axis");
        if(p.Gyro.ScreenOrientation is not ("Landscape" or "Portrait" or "LandscapeFlipped" or "PortraitFlipped"))errors.Add("Unknown gyro screen orientation");
        var ids=new HashSet<string>();
        foreach(var a in p.Actions){if(a is null){errors.Add("Null action");continue;}if(string.IsNullOrWhiteSpace(a.Id)||!ids.Add(a.Id)||GameProfile.Commands.Contains(a.Id))errors.Add("Missing/duplicate/reserved action ID");if(!GameProfile.Outputs.Contains(a.Output))errors.Add($"Unknown output: {a.Output}");if(!GameProfile.Icons.Contains(a.Icon))errors.Add($"Unknown icon: {a.Icon}");if(a.Interaction is not ("Hold" or "Tap" or "Toggle" or "AimToggleSlide"))errors.Add("Unknown interaction");if(a.Interaction=="AimToggleSlide"&&p.Binding(a.SlideAction) is null)errors.Add("Slide action missing");}
        var controls=new HashSet<string>();
        foreach(var c in p.Controls){if(c is null){errors.Add("Null control");continue;}if(string.IsNullOrWhiteSpace(c.Id)||!controls.Add(c.Id))errors.Add("Duplicate control ID");if(!ids.Contains(c.Action)&&!GameProfile.Commands.Contains(c.Action))errors.Add($"Unknown action: {c.Action}");Range(c.Size,.035,.22,"Control size");Range(c.HitScale,1,1.8,"Hit target");Range(c.Opacity,.15,1,"Control opacity");Range(c.X,c.Size/2,1-c.Size/2,"Control X");Range(c.Y,c.Size/2,1-c.Size/2,"Control Y");}
        foreach(var id in p.Radial)if(!ids.Contains(id)&&!GameProfile.Commands.Contains(id))errors.Add($"Unknown radial action: {id}");
        if(!p.Controls.Any(c=>c is not null&&c.Action=="pause"&&c.Visible))errors.Add("A reachable Pause control is required");
        if(!p.Controls.Any(c=>c is not null&&c.Action=="actions"&&c.Visible))errors.Add("A reachable Actions control is required");
        foreach(var c in p.Controls.Where(c=>c is not null&&c.Visible))
            if(p.Controls.Any(other=>other is not null&&other.Visible&&other.Id!=c.Id&&other.Size<=c.Size&&Math.Abs(other.X-c.X)<.005&&Math.Abs(other.Y-c.Y)<.005))errors.Add("Overlapping control centers make a control unreachable: "+c.Id);
        return errors;
    }
    public static void SaveAtomic(string path,GameProfile p)
    {
        var text=Serialize(p);Parse(text);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,text);if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
