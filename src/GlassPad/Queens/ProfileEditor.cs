// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GlassPad.Engine;
using Control = GlassPad.Engine.Control;
using Microsoft.Win32;
namespace GlassPad.Queens;
internal sealed class ProfileEditor : Window
{
    private GameProfile _draft;
    private readonly ContactRouter _router;
    private readonly ControlSurface _surface;
    private readonly StackPanel _inspector=new(){Margin=new Thickness(14)};
    private readonly ComboBox _controls=new(){Margin=new Thickness(0,4,0,12)};
    private readonly TextBlock _error=new(){Foreground=Brushes.OrangeRed,TextWrapping=TextWrapping.Wrap};
    private bool _drag;
    private readonly GameProfile _defaults;
    private Vec _offset;
    private Control? Selected=>_draft.Controls.FirstOrDefault(c=>c.Id==_controls.SelectedItem as string);
    public GameProfile? Result{get;private set;}
    public ProfileEditor(GameProfile profile)
    {
        var defaultPath=Path.Combine(AppContext.BaseDirectory,"profiles",profile.Id+".json");_defaults=File.Exists(defaultPath)?ProfileStore.Parse(File.ReadAllText(defaultPath)):profile.Clone();_draft=profile.Clone();_router=new(_draft);_surface=new(_router){Preview=true,DebugTargets=true,Width=1280,Height=800};_router.SetSize(1280,800);
        Title="Customize Controls • "+profile.Approval+" profile";Width=1200;Height=860;MinWidth=900;MinHeight=620;Background=new SolidColorBrush(Color.FromRgb(20,29,26));Foreground=Brushes.WhiteSmoke;
        var root=new DockPanel{Margin=new Thickness(14)};Content=root;
        var footer=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        void Button(string label,Action action){var b=new Button{Content=label,Padding=new Thickness(14,8,14,8),Margin=new Thickness(0,0,8,0)};b.Click+=(_,_)=>action();footer.Children.Add(b);}
        Button("Save profile",Save);Button("Cancel",()=>{DialogResult=false;Close();});Button("Restore defaults",()=>{_draft=_defaults.Clone();Refresh();});Button("Import",Import);Button("Export",Export);
        var right=new ScrollViewer{Width=340,Content=_inspector,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};DockPanel.SetDock(right,Dock.Right);root.Children.Add(right);
        var canvas=new Grid{Background=Brushes.Transparent};canvas.Children.Add(new Viewbox{Child=_surface,Stretch=Stretch.Uniform});root.Children.Add(canvas);
        canvas.MouseLeftButtonDown+=(_,e)=>{
            var pt=e.GetPosition(_surface);var point=new Vec(pt.X,pt.Y);
            var hit=_draft.Controls.OrderBy(c=>c.Size).FirstOrDefault(c=>(point-new Vec(c.X*1280,c.Y*800)).Length<=c.Size*800/2);
            if(hit is null)return;_controls.SelectedItem=hit.Id;_surface.SelectedId=hit.Id;_offset=point-new Vec(hit.X*1280,hit.Y*800);_drag=true;canvas.CaptureMouse();_surface.InvalidateVisual();e.Handled=true;
        };
        canvas.MouseMove+=(_,e)=>{if(!_drag||Selected is not Control c)return;var pt=e.GetPosition(_surface);c.X=Math.Clamp((pt.X-_offset.X)/1280,c.Size/2,1-c.Size/2);c.Y=Math.Clamp((pt.Y-_offset.Y)/800,c.Size/2,1-c.Size/2);_surface.InvalidateVisual();};
        canvas.MouseLeftButtonUp+=(_,e)=>{if(!_drag)return;_drag=false;canvas.ReleaseMouseCapture();BuildInspector();};
        canvas.LostMouseCapture+=(_,_)=>_drag=false;
        Refresh();
    }
    private void Refresh()
    { _router.Configure(_draft);_controls.ItemsSource=_draft.Controls.Select(c=>c.Id).ToList();_controls.SelectedIndex=0;BuildInspector();_surface.InvalidateVisual(); }
    private void Changed(){_surface.InvalidateVisual();}
    private void Heading(string text)=>_inspector.Children.Add(new TextBlock{Text=text,FontSize=18,Margin=new Thickness(0,14,0,8)});
    private void Note(string text)=>_inspector.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray,Margin=new Thickness(0,4,0,5)});
    private void Number(string title,double value,double min,double max,Action<double> change)
    {
        var label=new TextBlock{Text=$"{title}: {value:0.###}",Margin=new Thickness(0,6,0,2)};_inspector.Children.Add(label);
        var slider=new Slider{Minimum=min,Maximum=max,Value=value,Margin=new Thickness(0,2,0,5)};System.Windows.Automation.AutomationProperties.SetName(slider,title);slider.ValueChanged+=(_,e)=>{change(e.NewValue);label.Text=$"{title}: {e.NewValue:0.###}";Changed();};_inspector.Children.Add(slider);
    }
    private void Check(string text,bool value,Action<bool> change)
    {var c=new CheckBox{Content=text,IsChecked=value,Margin=new Thickness(0,6,0,3),Foreground=Brushes.WhiteSmoke};c.Click+=(_,_)=>{change(c.IsChecked==true);Changed();};_inspector.Children.Add(c);}
    private void Choice(string title,IEnumerable<string> values,string value,Action<string> change)
    {
        _inspector.Children.Add(new TextBlock{Text=title,Margin=new Thickness(0,8,0,3)});
        var c=new ComboBox{ItemsSource=values.ToList(),SelectedItem=value,Margin=new Thickness(0,2,0,6)};c.SelectionChanged+=(_,_)=>{if(c.SelectedItem is string s){change(s);Changed();}};_inspector.Children.Add(c);
    }
    private void BuildInspector()
    {
        _inspector.Children.Clear();Heading("EDIT / touch layout");Note("Drag controls in the preview. Dotted circles are effective touch targets; invisible controls appear dimmed. Changes apply only on Save.");
        _inspector.Children.Add(_controls);_controls.SelectionChanged-=_selection;_controls.SelectionChanged+=_selection;
        if(Selected is Control c)
        {
            _surface.SelectedId=c.Id;Heading(c.Id);
            Choice("Action",_draft.Actions.Select(a=>a.Id).Concat(["actions"]),c.Action,s=>{c.Action=s;BuildInspector();});
            Check("Visible",c.Visible,v=>c.Visible=v);Check("Show label",c.Label,v=>c.Label=v);
            Number("X",c.X,c.Size/2,1-c.Size/2,v=>c.X=v);Number("Y",c.Y,c.Size/2,1-c.Size/2,v=>c.Y=v);
            Number("Size / short edge",c.Size,.035,.22,v=>{c.Size=v;c.X=Math.Clamp(c.X,v/2,1-v/2);c.Y=Math.Clamp(c.Y,v/2,1-v/2);});
            Number("Touch target scale",c.HitScale,1,1.8,v=>c.HitScale=v);Number("Control opacity",c.Opacity,.15,1,v=>c.Opacity=v);
            if(_draft.Binding(c.Action) is ActionBinding a)
            {
                Choice("Icon",GameProfile.Icons.Order(),a.Icon,v=>a.Icon=v);Choice("Controller input",GameProfile.Outputs.Order(),a.Output,v=>a.Output=v);
                Choice("Interaction",["Hold","Tap","Toggle","AimToggleSlide"],a.Interaction,v=>a.Interaction=v);
                Check("Drag to look while held",a.DragLook,v=>a.DragLook=v);
                Choice("Slide-to-fire action (generic profiles)",_draft.Actions.Select(x=>x.Id).Prepend(""),a.SlideAction,v=>a.SlideAction=v);
            }
        }
        Heading("Promote an action");
        Choice("Add direct control",_draft.Actions.Select(a=>a.Id).Prepend("Choose…"),"Choose…",id=>{
            if(id=="Choose…")return;var existing=_draft.Controls.FirstOrDefault(x=>x.Action==id);if(existing is not null){existing.Visible=true;_controls.SelectedItem=existing.Id;}
            else{var control=new Control{Id="direct-"+id+"-"+_draft.Controls.Count,Action=id,X=.65,Y=.72};_draft.Controls.Add(control);Refresh();_controls.SelectedItem=control.Id;}
        });
        Heading("Global");Number("Global opacity",_draft.Opacity,.15,1,v=>_draft.Opacity=v);
        Check("Preview touch targets",_surface.DebugTargets,v=>_surface.DebugTargets=v);
        Heading("Floating movement");ZoneEditor("Movement",()=>_draft.MoveZone,z=>_draft.MoveZone=z);
        Number("Travel radius",_draft.MoveRadius,.03,.3,v=>_draft.MoveRadius=v);Number("Movement deadzone",_draft.MoveDeadzone,0,.5,v=>_draft.MoveDeadzone=v);
        Choice("Run gesture",["Disabled","DoubleTap","OuterHold"],_draft.Sprint,v=>_draft.Sprint=v);
        Note("Choose DoubleTap only for game tap-to-run; OuterHold only for a verified held-run binding. The run hint is a request, not game-state detection.");
        Number("Double tap window (ms)",_draft.DoubleTapMs,100,500,v=>_draft.DoubleTapMs=v);
        Number("Sprint outer threshold",_draft.SprintOuter,.5,1,v=>_draft.SprintOuter=v);Number("Sprint inner threshold",_draft.SprintInner,.1,.9,v=>_draft.SprintInner=v);
        Number("Slide gesture threshold",_draft.SlideThreshold,.005,.15,v=>_draft.SlideThreshold=v);
        Heading("Swipe look");ZoneEditor("Look",()=>_draft.LookZone,z=>_draft.LookZone=z);
        Number("Horizontal sensitivity",_draft.Look.Horizontal,.05,8,v=>_draft.Look.Horizontal=v);Number("Vertical sensitivity",_draft.Look.Vertical,.05,8,v=>_draft.Look.Vertical=v);
        Number("Full-speed swipe (DIP/s)",_draft.Look.FullSpeed,100,4000,v=>_draft.Look.FullSpeed=v);Number("Response power",_draft.Look.Response,.3,3,v=>_draft.Look.Response=v);
        Number("Game deadzone compensation",_draft.Look.GameDeadzone,0,.5,v=>_draft.Look.GameDeadzone=v);Check("Invert horizontal",_draft.Look.InvertX,v=>_draft.Look.InvertX=v);Check("Invert vertical",_draft.Look.InvertY,v=>_draft.Look.InvertY=v);
        Note("XInput turning is bounded by the game’s turn speed. Calibration is provisional until tested in this game.");
        Heading("Actions radial");Number("Radial X",_draft.RadialX,.18,.82,v=>_draft.RadialX=v);Number("Radial Y",_draft.RadialY,.28,.72,v=>_draft.RadialY=v);Number("Radial radius",_draft.RadialRadius,.12,.3,v=>_draft.RadialRadius=v);
        for(int i=0;i<_draft.Radial.Count;i++){var slot=i;Choice($"Slot {i+1}",_draft.Actions.Select(a=>a.Id).Concat(["equipment","gyro","more"]),_draft.Radial[i],v=>_draft.Radial[slot]=v);}
        Heading("Tablet gyro");Note("Off by default. Uses the Windows ACPI AMD tablet sensor; controller IMUs are excluded. Menu, editor and focus loss suspend gyro.");
        Number("Gyro sensitivity",_draft.Gyro.Sensitivity,.1,8,v=>_draft.Gyro.Sensitivity=v);Number("Gyro deadzone (deg/s)",_draft.Gyro.Deadzone,0,10,v=>_draft.Gyro.Deadzone=v);
        Choice("Gyro mode",["Always","TouchingLook"],_draft.Gyro.Mode,v=>_draft.Gyro.Mode=v);
        Choice("Screen orientation",["Landscape","Portrait","LandscapeFlipped","PortraitFlipped"],_draft.Gyro.ScreenOrientation,v=>_draft.Gyro.ScreenOrientation=v);
        Choice("Yaw axis",["X","Y","Z"],_draft.Gyro.YawAxis,v=>_draft.Gyro.YawAxis=v);Choice("Pitch axis",["X","Y","Z"],_draft.Gyro.PitchAxis,v=>_draft.Gyro.PitchAxis=v);
        Check("Invert gyro horizontal",_draft.Gyro.InvertX,v=>_draft.Gyro.InvertX=v);Check("Invert gyro vertical",_draft.Gyro.InvertY,v=>_draft.Gyro.InvertY=v);
        Number("Gyro bias X",_draft.Gyro.BiasX,-10,10,v=>_draft.Gyro.BiasX=v);Number("Gyro bias Y",_draft.Gyro.BiasY,-10,10,v=>_draft.Gyro.BiasY=v);
        var calibrate=new Button{Content="Calibrate tablet gyro — keep still",Padding=new Thickness(8),Margin=new Thickness(0,8,0,8)};
        calibrate.Click+=async(_,_)=>{calibrate.IsEnabled=false;calibrate.Content="Keep tablet still…";try{var bias=await TabletGyro.CalibrateAsync(_draft);_draft.Gyro.BiasX=bias.Yaw;_draft.Gyro.BiasY=bias.Pitch;_draft.Gyro.CalibratedAt=DateTimeOffset.Now.ToString("O");BuildInspector();}catch(Exception ex){_error.Text=ex.Message;calibrate.Content="Calibration failed; retry";calibrate.IsEnabled=true;}};_inspector.Children.Add(calibrate);
        _inspector.Children.Add(_error);
    }
    private void _selection(object sender,SelectionChangedEventArgs e){BuildInspector();Changed();}
    internal void TestChange(string field,double value)
    {
        var slider=_inspector.Children.OfType<Slider>().First(s=>System.Windows.Automation.AutomationProperties.GetName(s)==field);
        slider.Value=value;
    }
    internal void TestSelect(string id)=>_controls.SelectedItem=id;
    internal void TestSave()=>Save();
    internal void TestCancel(){DialogResult=false;Close();}
    internal (int Fields,double Extent,double Viewport) TestDiagnostics()
    {UpdateLayout();var scroller=(ScrollViewer)_inspector.Parent;return(_inspector.Children.Count,scroller.ExtentHeight,scroller.ViewportHeight);}
    private void ZoneEditor(string label,Func<Zone> get,Action<Zone> set)
    {
        var z=get();
        Number(label+" zone X",z.X,0,.85,v=>{var old=get();set(old with{X=v,Width=Math.Min(old.Width,1-v)});});
        Number(label+" zone Y",z.Y,0,.85,v=>{var old=get();set(old with{Y=v,Height=Math.Min(old.Height,1-v)});});
        Number(label+" zone width",z.Width,.1,1-z.X,v=>{set(get() with{Width=v});});
        Number(label+" zone height",z.Height,.1,1-z.Y,v=>{set(get() with{Height=v});});
    }
    private void Save()
    {
        try{var checkedProfile=ProfileStore.Parse(ProfileStore.Serialize(_draft));Settings.Save(checkedProfile);Result=checkedProfile;DialogResult=true;Close();}
        catch(Exception ex){_error.Text=ex.Message;MessageBox.Show(this,ex.Message,"Profile needs correction");}
    }
    private void Import(){var d=new OpenFileDialog{Filter="JSON profile|*.json"};if(d.ShowDialog(this)==true){try{_draft=ProfileStore.Parse(File.ReadAllText(d.FileName));_draft.Approval="Draft";if(_draft.Id.EndsWith("-approved"))_draft.Id=_draft.Id[..^9]+"-draft";Refresh();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Import rejected");}}}
    private void Export(){var d=new SaveFileDialog{Filter="JSON profile|*.json",FileName="queens-domain-draft.json"};if(d.ShowDialog(this)==true){try{ProfileStore.SaveAtomic(d.FileName,_draft);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Export rejected");}}}
}
