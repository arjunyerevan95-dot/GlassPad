// SPDX-License-Identifier: GPL-3.0-or-later
namespace GlassPad.Engine;

public readonly record struct PadState(ushort Buttons=0, byte LT=0, byte RT=0, double LX=0, double LY=0, double RX=0, double RY=0)
{
    public bool Neutral => this==default;
}
public sealed record RouterView(PadState State,int Contacts,Vec? MoveAnchor,Vec? MovePosition,string Panel,int Highlight,string Hint,bool MenuMode,bool Hidden,bool Focused,bool Gyro);
public enum TouchPhase { Down, Move, Up, Cancel }
public readonly record struct ContactEvent(int Id,TouchPhase Phase,Vec Position,double SourceTime,double ReceivedAt);

/// All state is protected by one gate shared with the publisher. Ownership is assigned once,
/// and action owners are aggregated afresh into one report (no per-button last-writer state).
public sealed class ContactRouter
{
    private sealed class Contact
    {
        public required int Id; public required string Role; public string Action="";
        public Vec Anchor,Position;public double Started,Source; public SwipeAim Aim=new();public bool Slid;
    }
    public object Gate { get; } = new();
    private readonly Dictionary<int,Contact> _contacts=[];
    private readonly Dictionary<string,double> _pulses=[];
    private readonly HashSet<string> _toggles=[];
    private readonly Queue<string> _commands=[];
    private GameProfile _profile;
    private double _width=1152,_height=720,_lastMoveUp=double.NegativeInfinity,_gyroAt;
    private Vec _lastMovePosition,_gyro;
    private bool _focused,_enabled=true,_menu,_hidden,_sprintHeld,_gyroEnabled;
    private string _panel="",_holdAction="",_hint="Draft • input suspended until game is foreground";
    private int _highlight=-1;
    public ContactRouter(GameProfile profile){_profile=profile;}
    public GameProfile Profile { get {lock(Gate)return _profile;} }
    public bool AcceptingGyro { get {lock(Gate)return _enabled&&_focused&&!_hidden&&!_menu&&_panel.Length==0&&_gyroEnabled;} }
    public void SetSize(double width,double height){lock(Gate){if(width!=_width||height!=_height){ClearUnsafe();_width=Math.Max(1,width);_height=Math.Max(1,height);}}}
    public void Configure(GameProfile p){lock(Gate){ClearUnsafe();_profile=p;_gyroEnabled=false;_menu=false;_hidden=false;}}
    public void SetFocus(bool value){lock(Gate){if(_focused!=value){ClearUnsafe();_focused=value;_hint=value?"Draft • gameplay controls":"Game not in foreground • inputs released";}}}
    public void SetEnabled(bool value){lock(Gate){ClearUnsafe();_enabled=value;}}
    public void ToggleMenu(){lock(Gate){ClearUnsafe();_menu=!_menu;_hint=_menu?"MANUAL MENU MODE • return to gameplay in More":"Gameplay mode • manual selection";}}
    public void ToggleHidden(){lock(Gate){ClearUnsafe();_hidden=!_hidden;}}
    public void SetGyroEnabled(bool enabled){lock(Gate){_gyroEnabled=enabled;_gyro=default;}}
    public void SetGyro(Vec value,double received){lock(Gate){_gyro=value;_gyroAt=received;}}
    public void Clear(string reason="Inputs released"){lock(Gate){ClearUnsafe();_hint=reason;}}
    private void ClearUnsafe(){_contacts.Clear();_pulses.Clear();_toggles.Clear();_commands.Clear();_panel="";_holdAction="";_highlight=-1;_sprintHeld=false;_gyro=default;_lastMoveUp=double.NegativeInfinity;}
    public string? TakeCommand(){lock(Gate)return _commands.TryDequeue(out var command)?command:null;}
    public Dictionary<int,string> Assignments(){lock(Gate)return _contacts.ToDictionary(k=>k.Key,v=>v.Value.Role+":"+v.Value.Action);}
    public void ShowPanel(string panel){lock(Gate){ClearUnsafe();_panel=panel;}}
    public void PreviewPanel(string panel){lock(Gate){_panel=panel;_highlight=-1;}}
    private Vec Normal(Vec p)=>new(p.X/_width,p.Y/_height);
    private double Unit=>Math.Min(_width,_height);
    public Vec RadialCenter=>new(Math.Clamp(_profile.RadialX*_width,Unit*(_profile.RadialRadius+.09),_width-Unit*(_profile.RadialRadius+.09)),Math.Clamp(_profile.RadialY*_height,Unit*(_profile.RadialRadius+.09),_height-Unit*(_profile.RadialRadius+.09)));
    public IReadOnlyList<string> PanelEntries => _panel switch {
        "radial" => _profile.Radial,
        "equipment" => ["nextThrow","nextWeapon","nextItem","previousItem","cancel"],
        "more" => ["customize","profiles","menu","hide","exit","cancel"],
        "hold" => [_holdAction,"cancel"],
        "gyroCalibrate" => ["gyroStart","cancel"],
        "gyroWait" => ["cancel"],
        _ => []
    };
    public IReadOnlyList<Control> ActiveControls()
    {
        if(!_menu)return _profile.Controls;
        return [
            new(){Id="navUp",Action="navUp",X=.15,Y=.57,Size=.10},new(){Id="navDown",Action="navDown",X=.15,Y=.84,Size=.10},
            new(){Id="navLeft",Action="navLeft",X=.07,Y=.705,Size=.10},new(){Id="navRight",Action="navRight",X=.23,Y=.705,Size=.10},
            new(){Id="confirm",Action="confirm",X=.91,Y=.78,Size=.12},new(){Id="back",Action="back",X=.80,Y=.84,Size=.09},
            new(){Id="menuLB",Action="menuLB",X=.12,Y=.20,Size=.08},new(){Id="menuRB",Action="menuRB",X=.88,Y=.20,Size=.08},
            new(){Id="menuLT",Action="menuLT",X=.23,Y=.20,Size=.08},new(){Id="menuRT",Action="menuRT",X=.77,Y=.20,Size=.08},
            _profile.Controls.First(c=>c.Action=="actions"),_profile.Controls.First(c=>c.Action=="pause")];
    }
    public ActionBinding? Resolve(string action)=> action switch {
        "navUp"=>Nav(action,"Up","Up","up"),"navDown"=>Nav(action,"Down","Down","down"),
        "navLeft"=>Nav(action,"Left","Left","left"),"navRight"=>Nav(action,"Right","Right","right"),
        "confirm"=>Nav(action,"Confirm","A","confirm"),"back"=>Nav(action,"Back","B","back"),
        "menuLB"=>Nav(action,"LB","LB","left"),"menuRB"=>Nav(action,"RB","RB","right"),
        "menuLT"=>Nav(action,"LT","LT","secondary"),"menuRT"=>Nav(action,"RT","RT","sword"),
        _=>_profile.Binding(action)
    };
    private static ActionBinding Nav(string id,string label,string output,string icon)=>new(){Id=id,Label=label,Output=output,Icon=icon};
    public void ProcessBatch(IEnumerable<ContactEvent> events,double now)
    {
        lock(Gate){foreach(var e in events)ProcessUnsafe(e,now);}
    }
    public void Process(ContactEvent e,double now){lock(Gate)ProcessUnsafe(e,now);}
    private void ProcessUnsafe(ContactEvent e,double now)
    {
        if(e.Phase==TouchPhase.Cancel){Release(e.Id,true,now);return;}
        if(!_enabled||!_focused||!double.IsFinite(e.Position.X)||!double.IsFinite(e.Position.Y)||!double.IsFinite(e.SourceTime)||!double.IsFinite(e.ReceivedAt))return;
        // Receipt is captured before queue/processing; processing never extends a stale sample.
        if(now-e.ReceivedAt>.1||e.ReceivedAt>now+.01){Release(e.Id,true,now);return;}
        if(e.Phase==TouchPhase.Down)
        {
            if(_contacts.ContainsKey(e.Id))return; // duplicate down cannot recenter or refire
            var c=new Contact{Id=e.Id,Role="ignored",Anchor=e.Position,Position=e.Position,Started=e.ReceivedAt,Source=e.SourceTime};
            if(_hidden){if(e.Position.X>_width-Unit*.13&&e.Position.Y<Unit*.14){c.Role="command";c.Action="recover";}}
            else if(_panel.Length>0)
            {
                c.Role="panel";c.Action=_panel;
                if(_panel=="hold"&&PanelHit(e.Position)==0){c.Role="button";c.Action=_holdAction;c.Aim.Begin(e.Position,e.SourceTime,e.ReceivedAt);BeginAction(c,now);}
            }
            else
            {
                var control=ActiveControls().Where(x=>x.Visible).OrderBy(x=>x.Size).FirstOrDefault(x=>(e.Position-new Vec(x.X*_width,x.Y*_height)).Length<=x.Size*Unit*x.HitScale/2);
                if(control is not null)
                {
                    c.Action=control.Action;
                    c.Role=GameProfile.Commands.Contains(c.Action)?"command":"button";
                    if(c.Action=="actions")
                    {
                        // Opening a modal releases gameplay owners. The opening contact remains.
                        ClearUnsafe();_panel="radial";c.Role="panel";c.Action="radial-open";
                    }
                    else if(c.Role=="button"){c.Aim.Begin(e.Position,e.SourceTime,e.ReceivedAt);BeginAction(c,now);}
                }
                else if(_profile.MoveZone.Contains(Normal(e.Position))&&!_contacts.Values.Any(x=>x.Role=="move"))
                {
                    c.Role="move";
                    if(!_menu&&_profile.Sprint=="DoubleTap"&&now-_lastMoveUp is >.06&&now-_lastMoveUp<=_profile.DoubleTapMs/1000&&(e.Position-_lastMovePosition).Length<Unit*.12){Pulse("run",now);_hint="Run requested • double tap";_lastMoveUp=double.NegativeInfinity;}
                }
                else if(!_menu&&_profile.LookZone.Contains(Normal(e.Position))&&!_contacts.Values.Any(x=>x.Role=="look"))
                {c.Role="look";c.Aim.Begin(e.Position,e.SourceTime,e.ReceivedAt);}
            }
            _contacts.Add(e.Id,c);return;
        }
        if(!_contacts.TryGetValue(e.Id,out var existing))return;
        if(e.SourceTime<existing.Source)return;
        if(e.Phase==TouchPhase.Up){existing.Position=e.Position;Release(e.Id,false,now);return;}
        if(e.SourceTime==existing.Source)return;
        existing.Position=e.Position;existing.Source=e.SourceTime;
        var binding=Resolve(existing.Action);
        if(existing.Role=="look"||existing.Role=="button"&&binding?.DragLook==true&&!_menu)
            existing.Aim.Move(e.Position,e.SourceTime,e.ReceivedAt,_profile.Look);
        if(existing.Role=="button"&&binding?.Interaction=="AimToggleSlide")existing.Slid=(e.Position-existing.Anchor).Length>Unit*_profile.SlideThreshold;
        if(existing.Role=="panel")_highlight=PanelHit(e.Position);
    }
    private void BeginAction(Contact c,double now)
    {
        var a=Resolve(c.Action);if(a is null)return;
        if(a.Interaction=="Tap")Pulse(c.Action,now);
        if(a.Interaction is "Toggle" or "AimToggleSlide"){if(!_toggles.Add(c.Action))_toggles.Remove(c.Action);}
    }
    private void Pulse(string id,double now)=>_pulses[id]=now+.10;
    private void Release(int id,bool cancel,double now)
    {
        if(!_contacts.Remove(id,out var c))return;
        if(c.Role=="move"){_sprintHeld=false;if(!cancel){_lastMoveUp=now;_lastMovePosition=c.Position;}}
        if(cancel){if(c.Role=="panel"){_panel="";_highlight=-1;}return;}
        if(c.Role=="command")Command(c.Action);
        if(c.Role=="panel")
        {
            var index=PanelHit(c.Position);var entries=PanelEntries;
            if(c.Action=="radial-open"&&(c.Position-c.Anchor).Length<Unit*.025&&now-c.Started<.35){_panel="radial";return;}
            if(index<0||index>=entries.Count){_panel="";_highlight=-1;return;}
            var selected=entries[index];_panel="";_highlight=-1;
            if(GameProfile.Commands.Contains(selected))Command(selected);
            else
            {
                var binding=Resolve(selected);
                if(binding?.Interaction=="Hold"){_holdAction=selected;_panel="hold";_hint=$"Hold {binding.Label}; drag to look, lift to release";}
                else if(binding?.Interaction=="Tap")Pulse(selected,now);
                else if(binding is not null){if(!_toggles.Add(selected))_toggles.Remove(selected);}
            }
        }
    }
    private void Command(string command)
    {
        switch(command)
        {
            case "equipment":_panel="equipment";break;
            case "more":_panel="more";break;
            case "menu":ClearUnsafe();_menu=!_menu;_hint=_menu?"MANUAL MENU MODE":"Gameplay mode";break;
            case "hide":ClearUnsafe();_hidden=true;break;
            case "recover":ClearUnsafe();_hidden=false;break;
            case "cancel":_panel="";break;
            default:_commands.Enqueue(command);break;
        }
    }
    public int PanelHit(Vec p)
    {
        if(_panel=="radial")
        {
            var d=p-RadialCenter;var r=Unit*_profile.RadialRadius;
            if(d.Length<r*.38||d.Length>r*1.65)return -1;
            var angle=(Math.Atan2(d.X,-d.Y)+Math.PI*2)%(Math.PI*2);
            return (int)Math.Floor((angle+Math.PI/_profile.Radial.Count)/(Math.PI*2/_profile.Radial.Count))%_profile.Radial.Count;
        }
        var entries=PanelEntries;var w=Unit*.48;var row=Unit*.085;
        var origin=new Vec(Math.Clamp(RadialCenter.X-w/2,8,_width-w-8),Math.Clamp(RadialCenter.Y-entries.Count*row/2,8,_height-entries.Count*row-8));
        if(p.X<origin.X||p.X>origin.X+w||p.Y<origin.Y||p.Y>=origin.Y+entries.Count*row)return -1;
        return (int)((p.Y-origin.Y)/row);
    }
    public PadState Sample(double now)
    {
        lock(Gate)
        {
            if(!_focused||!_enabled||_hidden)return default;
            ushort buttons=0;byte lt=0,rt=0;var move=default(Vec);var look=default(Vec);
            void Add(string id)
            {
                var a=Resolve(id);if(a is null)return;
                switch(a.Output){case "LT":lt=255;break;case "RT":rt=255;break;default:buttons|=Mask(a.Output);break;}
            }
            foreach(var c in _contacts.Values)
            {
                if(c.Role=="move")
                {
                    var d=(c.Position-c.Anchor)*(1/(Unit*_profile.MoveRadius));var length=d.Length;
                    if(length>_profile.MoveDeadzone)move=new Vec(d.X,-d.Y)*(Math.Min(1,(length-_profile.MoveDeadzone)/(1-_profile.MoveDeadzone))/length);
                    if(!_menu&&_profile.Sprint=="OuterHold")
                    {if(length>=_profile.SprintOuter)_sprintHeld=true;else if(length<_profile.SprintInner)_sprintHeld=false;if(_sprintHeld)Add("run");}
                }
                if(c.Role=="button")
                {
                    var a=Resolve(c.Action);
                    if(a?.Interaction=="Hold")Add(c.Action);
                    if(a?.Interaction=="AimToggleSlide"&&c.Slid&&_toggles.Contains(c.Action))Add(a.SlideAction);
                }
                if(!_menu&&(c.Role=="look"||c.Role=="button"))look+=c.Aim.Sample(now);
            }
            foreach(var id in _toggles)Add(id);
            foreach(var (id,until) in _pulses)if(now<until)Add(id);
            if(AcceptingGyro&&now-_gyroAt<.060&&(_profile.Gyro.Mode!="TouchingLook"||_contacts.Values.Any(c=>c.Role=="look")))look+=_gyro;
            look=look.Clamp();move=move.Clamp();return new(buttons,lt,rt,move.X,move.Y,look.X,look.Y);
        }
    }
    public RouterView View(double now)
    {
        lock(Gate){var move=_contacts.Values.FirstOrDefault(c=>c.Role=="move");return new(Sample(now),_contacts.Count,move?.Anchor,move?.Position,_panel,_highlight,_hint,_menu,_hidden,_focused,_gyroEnabled);}
    }
    public static ushort Mask(string output)=>output switch{"Up"=>1,"Down"=>2,"Left"=>4,"Right"=>8,"Start"=>0x10,"Back"=>0x20,"L3"=>0x40,"R3"=>0x80,"LB"=>0x100,"RB"=>0x200,"A"=>0x1000,"B"=>0x2000,"X"=>0x4000,"Y"=>0x8000,_=>0};
}
