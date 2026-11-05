// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using GlassPad.Engine;
using Microsoft.Win32;

namespace GlassPad.Queens;
internal sealed class GameplayOverlay : Window
{
    private readonly ContactRouter _router;
    private readonly ControlSurface _surface;
    private readonly XInputOutput _output=new();
    private readonly DispatcherTimer _visualTimer;
    private readonly System.Threading.Timer _publisher;
    private readonly TouchInputTrace _trace=new();
    private readonly TabletGyro _gyro;
    private nint _hwnd,_game;
    private readonly nint _testTarget;
    private int _tick;
    private bool _closing,_driverReady;
    private double _nextReconnect;
    private long _uiHeartbeat=Environment.TickCount64;
    private Native.Rect _lastBounds;
    private RouterView? _previous;
    private string _status="Connecting…";
    private int _nativePackets,_maxContacts;
    private bool _observedMoveLookAttackJump;
    public event Action? CustomizeRequested;
    public event Action? ProfilesRequested;
    public event Action<GameProfile>? AutoSelected;
    internal ContactRouter Router=>_router;
    internal (int Packets,int Contacts) TouchMetrics=>(_nativePackets,_maxContacts);
    public GameplayOverlay(GameProfile profile,nint testTarget=0)
    {
        _testTarget=testTarget;
        _router=new(profile);_surface=new(_router);_gyro=new(_router);
        Title="GlassPad Queen’s Domain Overlay";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;ResizeMode=ResizeMode.NoResize;Content=_surface;
        Width=SystemParameters.PrimaryScreenWidth;Height=SystemParameters.PrimaryScreenHeight;
        SourceInitialized+=Initialize;
        _publisher=new(Publish,null,Timeout.Infinite,Timeout.Infinite);
        _visualTimer=new(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(33)};
        _visualTimer.Tick+=VisualTick;
        SystemEvents.PowerModeChanged+=PowerChanged;
        Closed+=Cleanup;
    }
    private void Initialize(object? sender,EventArgs e)
    {
        _hwnd=new WindowInteropHelper(this).Handle;Native.NoActivate(_hwnd);
        var source=HwndSource.FromHwnd(_hwnd);source.AddHook(Hook);
        Native.UnregisterTouchWindow(_hwnd); // Native WM_POINTER frames, bypass WPF/legacy touch promotion.
        Native.RegisterHotKey(_hwnd,1,0x4000|3,0x47); // Ctrl+Alt+G
        Native.RegisterHotKey(_hwnd,2,0x4000|3,0x4D); // Ctrl+Alt+M
        Native.RegisterHotKey(_hwnd,3,0x4000|3,0x51); // Ctrl+Alt+Q
        try{if(_testTarget==0){_output.Connect();_driverReady=true;_status="ViGEm connected";}else _status="Input lab • no game output";}
        catch(Exception ex){_status="Controller unavailable: "+ex.Message;}
        Locate();_router.SetSize(ActualWidth,ActualHeight);_publisher.Change(0,4);_visualTimer.Start();
    }
    private void Locate()
    {
        if(_testTarget!=0){_game=_testTarget;return;}
        var found=Native.FindGame(_router.Profile);_game=found?.Handle??0;var foreground=Native.GetForegroundWindow();if(_game!=0&&_game==foreground)return;
        foreach(var p in Settings.ApprovedProfiles())
        {if(p.Id==_router.Profile.Id)continue;var candidate=Native.FindGame(p);if(candidate?.Handle==foreground){_router.Configure(p);_game=foreground;AutoSelected?.Invoke(p);break;}}
    }
    private void Publish(object? state)
    {
        if(_closing)return;
        lock(_router.Gate)
        {
            if(_closing)return;
            if(Environment.TickCount64-Interlocked.Read(ref _uiHeartbeat)>200)_router.Clear("UI stalled • inputs released");
            _router.SetFocus(_testTarget!=0||_game!=0&&Native.GetForegroundWindow()==_game);
            try{if(_driverReady)_output.Publish(_router.Sample(Native.Now));}
            catch(Exception ex){_driverReady=false;_status="Controller disconnected: "+ex.Message;_router.Clear(_status);}
        }
    }
    private async void VisualTick(object? sender,EventArgs e)
    {
        if(_closing)return;
        Interlocked.Exchange(ref _uiHeartbeat,Environment.TickCount64);
        if(++_tick%30==0)Locate();
        if(!_driverReady&&_testTarget==0&&_game!=0&&Native.Now>=_nextReconnect)
        {
            _nextReconnect=Native.Now+5;
            lock(_router.Gate)
            {
                _router.Clear("Reconnecting controller • inputs released");
                try{_output.Dispose();_output.Connect();_driverReady=true;_status="ViGEm reconnected";}
                catch(Exception ex){_status="Controller unavailable: "+ex.Message;}
            }
        }
        var focused=_testTarget!=0||_game!=0&&Native.GetForegroundWindow()==_game;
        var style=Native.GetWindowLongPtr(_hwnd,-20).ToInt64();
        Native.SetWindowLongPtr(_hwnd,-20,(nint)(focused?style&~0x20L:style|0x20L));
        Opacity=focused?1:0;
        if(_game!=0&&Native.GetClientRect(_game,out var rect))
        {
            var origin=new Native.Point();Native.ClientToScreen(_game,ref origin);
            var bounds=new Native.Rect{Left=origin.X,Top=origin.Y,Right=origin.X+rect.Right,Bottom=origin.Y+rect.Bottom};
            if(bounds.Left!=_lastBounds.Left||bounds.Top!=_lastBounds.Top||bounds.Right!=_lastBounds.Right||bounds.Bottom!=_lastBounds.Bottom)
            {Native.SetWindowPos(_hwnd,(nint)(-1),origin.X,origin.Y,rect.Right-rect.Left,rect.Bottom-rect.Top,0x10|0x40);_lastBounds=bounds;}
            _router.SetSize(ActualWidth,ActualHeight);
        }
        _surface.DriverStatus=_status;
        try{_gyro.Sync();}catch(Exception ex){_router.SetGyroEnabled(false);_status="Gyro suspended: "+ex.Message;}_surface.GyroStatus=_gyro.Status;
        var view=_router.View(Native.Now);
        if(_tick%30==0)
        {
            try
            {
                Directory.CreateDirectory(Settings.Root);var path=Path.Combine(Settings.Root,"live-status.json");
                File.WriteAllText(path+".tmp",System.Text.Json.JsonSerializer.Serialize(new{At=DateTimeOffset.Now,GameFound=_game!=0,view.Focused,Driver=_status,view.MenuMode,view.Hidden,view.Gyro,TabletGyro=_gyro.Status,NativeTouchPackets=_nativePackets,MaximumConcurrentContacts=_maxContacts,ObservedMoveLookAttackJump=_observedMoveLookAttackJump,view.State,TouchDigitizer=Native.GetSystemMetrics(94),MaximumHardwareTouches=Native.GetSystemMetrics(95)},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));File.Move(path+".tmp",path,true);
            }
            catch{}
        }
        if(view!=_previous){_surface.InvalidateVisual();_previous=view;}
        while(_router.TakeCommand() is string command)
        {
            if(command=="customize"){_router.SetEnabled(false);CustomizeRequested?.Invoke();}
            if(command=="profiles"){_router.SetEnabled(false);ProfilesRequested?.Invoke();}
            if(command=="exit")Application.Current.Shutdown();
            if(command=="gyro")
            {
                if(_gyro.Available)
                {
                    if(!_router.View(Native.Now).Gyro&&string.IsNullOrEmpty(_router.Profile.Gyro.CalibratedAt))
                    {_router.ShowPanel("gyroCalibrate");_status="Gyro OFF • start stationary calibration";}
                    else{_router.SetGyroEnabled(!_router.View(Native.Now).Gyro);_status=_router.View(Native.Now).Gyro?"ViGEm connected • gyro ON":"ViGEm connected • gyro off";}
                }
                else {_router.Clear(_gyro.Status);MessageBox.Show(_gyro.Status,"Tablet gyro unavailable");}
            }
            if(command=="gyroStart")
            {
                _router.ShowPanel("gyroWait");_status="Gyro OFF • keep tablet still";
                try
                {
                    await Task.Delay(600);
                    if(_closing)return;
                    if(_router.View(Native.Now).Panel!="gyroWait")continue;
                    var bias=await TabletGyro.CalibrateAsync(_router.Profile);
                    if(_closing)return;
                    if(_router.View(Native.Now).Panel!="gyroWait"){_status="Gyro OFF • calibration cancelled";continue;}
                    lock(_router.Gate){_router.Profile.Gyro.BiasX=bias.Yaw;_router.Profile.Gyro.BiasY=bias.Pitch;_router.Profile.Gyro.CalibratedAt=DateTimeOffset.Now.ToString("O");}
                    Settings.Save(_router.Profile);_router.ShowPanel("");_router.SetGyroEnabled(true);_status="ViGEm connected • gyro ON";
                }
                catch(Exception ex){_router.SetGyroEnabled(false);_router.ShowPanel("gyroCalibrate");_status="Gyro remains OFF: "+ex.Message;}
            }
        }
    }
    private nint Hook(nint hwnd,int msg,nint wParam,nint lParam,ref bool handled)
    {
        if(msg is 0x21 or 0x024b){handled=true;return (nint)3;} // MA_NOACTIVATE / PA_NOACTIVATE
        if(msg is 0x0245 or 0x0246 or 0x0247)
        {
            var id=(uint)(wParam.ToInt64()&0xffff);var now=Native.Now;
            if(!Native.GetPointerTouchInfo(id,out var single))return 0;
            handled=true;
            uint entries=0,pointers=0;Native.GetPointerFrameTouchInfoHistory(id,ref entries,ref pointers,null);
            Native.PointerTouchInfo[] history;
            if(entries is >0 and <=128&&pointers is >0 and <=32)
            {history=new Native.PointerTouchInfo[entries*pointers];if(!Native.GetPointerFrameTouchInfoHistory(id,ref entries,ref pointers,history)){history=[single];entries=pointers=1;}}
            else{history=[single];entries=pointers=1;}
            var transform=HwndSource.FromHwnd(hwnd).CompositionTarget.TransformFromDevice;var events=new List<ContactEvent>(history.Length);
            for(int row=(int)entries-1;row>=0;row--)for(int col=0;col<pointers;col++)
            {
                var t=history[row*pointers+col];var p=t.Pointer;
                var point=p.PixelRaw;Native.ScreenToClient(hwnd,ref point);var dip=transform.Transform(new Point(point.X,point.Y));
                var source=p.Performance!=0?p.Performance/(double)System.Diagnostics.Stopwatch.Frequency:(Environment.TickCount64-unchecked((int)(unchecked((uint)Environment.TickCount64)-p.Time)))/1000.0;
                var age=p.Performance!=0?now-source:unchecked((int)(unchecked((uint)Environment.TickCount64)-p.Time))/1000.0;
                var phase=(p.Flags&0x40000)!=0?TouchPhase.Up:(p.Flags&0x10000)!=0?TouchPhase.Down:TouchPhase.Move;
                if((p.Flags&0x8000)!=0||age>.15||age<-.01)phase=TouchPhase.Cancel;
                var e=new ContactEvent((int)p.Id,phase,new(dip.X,dip.Y),source,now);events.Add(e);_trace.Record(e,(int)pointers);
            }
            _router.ProcessBatch(events,now);_nativePackets++;
            var view=_router.View(now);_maxContacts=Math.Max(_maxContacts,view.Contacts);var state=view.State;
            if(new Vec(state.LX,state.LY).Length>.1&&new Vec(state.RX,state.RY).Length>0&&state.RT>0&&(state.Buttons&0x1200)!=0)_observedMoveLookAttackJump=true;
            _trace.RecordBatch(events,_router,now);_surface.InvalidateVisual();Native.SkipPointerFrameMessages(id);return 0;
        }
        if(msg==0x0240)
        {
            var now=Native.Now;var count=(int)(wParam.ToInt64()&0xffff);var points=new Native.TouchInput[count];
            try
            {
                if(!Native.GetTouchInputInfo(lParam,count,points,Marshal.SizeOf<Native.TouchInput>())){_router.Clear("Touch read failed");return 0;}
                var transform=HwndSource.FromHwnd(hwnd).CompositionTarget.TransformFromDevice;
                var events=new List<ContactEvent>(count);
                foreach(var t in points)
                {
                    var age=unchecked((int)(unchecked((uint)Environment.TickCount64)-t.Time));
                    var point=new Native.Point{X=t.X/100,Y=t.Y/100};Native.ScreenToClient(hwnd,ref point);var dip=transform.Transform(new Point(point.X,point.Y));
                    var phase=(t.Flags&4)!=0?TouchPhase.Up:(t.Flags&2)!=0?TouchPhase.Down:TouchPhase.Move;
                    // Palm packets never become controls; a stale queued sample cannot replay motion.
                    if((t.Flags&0x80)!=0||age>150||age< -10)phase=TouchPhase.Cancel;
                    var ev=new ContactEvent((int)t.Id,phase,new(dip.X,dip.Y),(Environment.TickCount64-age)/1000.0,now);
                    events.Add(ev);_trace.Record(ev,count);
                }
                _router.ProcessBatch(events,now);_nativePackets++;
                var view=_router.View(now);_maxContacts=Math.Max(_maxContacts,view.Contacts);var state=view.State;
                if(new Vec(state.LX,state.LY).Length>.1&&new Vec(state.RX,state.RY).Length>0&&state.RT>0&&(state.Buttons&0x1200)!=0)_observedMoveLookAttackJump=true;
                _trace.RecordBatch(events,_router,now);_surface.InvalidateVisual();
            }
            finally{Native.CloseTouchInputHandle(lParam);handled=true;}
            return 0;
        }
        if(msg==0x024c){_router.Process(new((int)(wParam.ToInt64()&0xffff),TouchPhase.Cancel,default,0,Native.Now),Native.Now);}
        if(msg==0x001f){_router.Clear("Touch capture cancelled");}
        if(msg>=0x0200&&msg<=0x020E&&(Native.GetMessageExtraInfo().ToInt64()&0xffffff00L)==0xff515700L){handled=true;return 0;}
        if(msg==0x0312)
        {handled=true;switch(wParam.ToInt32()){case 1:_router.ToggleHidden();break;case 2:_router.ToggleMenu();break;case 3:Application.Current.Shutdown();break;}return 0;}
        if(msg==0x02e0){_router.Clear("DPI changed");}
        return 0;
    }
    public void Resume(GameProfile profile){_router.Configure(profile);_router.SetEnabled(true);Locate();_previous=null;}
    private void PowerChanged(object sender,PowerModeChangedEventArgs e)
    {
        _router.Clear("Power transition • inputs released");_router.SetGyroEnabled(false);
        if(e.Mode==PowerModes.Resume&&!Dispatcher.HasShutdownStarted)
            Dispatcher.BeginInvoke(new Action(()=>{lock(_router.Gate){_driverReady=false;_nextReconnect=0;_output.Dispose();_status="Resume • waiting to reconnect controller";}}));
    }
    private void Cleanup(object? sender,EventArgs e)
    {
        _closing=true;_visualTimer.Stop();_publisher.Dispose();_gyro.Dispose();SystemEvents.PowerModeChanged-=PowerChanged;
        lock(_router.Gate){_router.Clear("Shutdown");_output.Dispose();}
        Native.UnregisterTouchWindow(_hwnd);for(int i=1;i<=3;i++)Native.UnregisterHotKey(_hwnd,i);_trace.Dispose();
    }
}
internal sealed class TouchInputTrace : IDisposable
{
    private readonly StreamWriter? _writer;
    private int _count;
    public TouchInputTrace()
    {
        if(Environment.GetEnvironmentVariable("GLASSPAD_TRACE")!="1")return;
        Directory.CreateDirectory(Settings.Root);_writer=new(Path.Combine(Settings.Root,"touch-trace-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".jsonl"));
    }
    public void Record(ContactEvent e,int batch)
    {if(_writer is null||_count++>100000)return;_writer.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{e.Id,e.Phase,e.Position,e.SourceTime,e.ReceivedAt,batch}));if(_count%60==0)_writer.Flush();}
    public void RecordBatch(List<ContactEvent> events,ContactRouter router,double now)
    {if(_writer is null||_count>100000)return;_writer.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{kind="routed-batch",events=events.Count,roles=router.Assignments(),state=router.Sample(now)}));}
    public void Dispose()=>_writer?.Dispose();
}
