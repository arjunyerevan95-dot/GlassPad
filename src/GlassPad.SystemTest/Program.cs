// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Text.Json;
using GlassPad.Engine;
using GlassPad.Queens;
using Windows.Devices.Sensors;
using Nefarius.ViGEm.Client;
using System.Reflection;

if(args.Contains("--calibrate-profile"))
{
    var index=Array.IndexOf(args,"--calibrate-profile");var destination=args[index+2];var p=ProfileStore.Parse(File.ReadAllText(File.Exists(destination)?destination:args[index+1]));
    var bias=await TabletGyro.CalibrateAsync(p);p.Gyro.BiasX=bias.Yaw;p.Gyro.BiasY=bias.Pitch;p.Gyro.CalibratedAt=DateTimeOffset.Now.ToString("O");ProfileStore.SaveAtomic(destination,p);
    Console.WriteLine($"PASS verified tablet stationary calibration saved: yaw {bias.Yaw:F4}, pitch {bias.Pitch:F4} deg/s; gyro still defaults OFF");return;
}

if(args.Contains("--gyro-live"))
{
    var profile=GameProfile.QueensDraft();var gyroRouter=new ContactRouter(profile);gyroRouter.SetFocus(true);
    using var gyro=new TabletGyro(gyroRouter);Console.WriteLine(gyro.Status+" "+gyro.DeviceId);
    if(!gyro.Available)throw new Exception("Tablet gyro not available");
    var sensor=Gyrometer.GetDefault()!;var readings=0;double max=0;
    void Count(Gyrometer s,GyrometerReadingChangedEventArgs e){Interlocked.Increment(ref readings);}
    sensor.ReadingChanged+=Count;
    try
    {
        gyro.Sync();await Task.Delay(100);if(gyroRouter.Sample(Native.Now).RX!=0||gyroRouter.Sample(Native.Now).RY!=0)throw new Exception("Gyro not off by default");
        var bias=await TabletGyro.CalibrateAsync(profile);profile.Gyro.BiasX=bias.Yaw;profile.Gyro.BiasY=bias.Pitch;Console.WriteLine($"Stationary calibration yaw {bias.Yaw:F4}, pitch {bias.Pitch:F4} deg/s");
        gyroRouter.SetGyroEnabled(true);gyro.Sync();for(int i=0;i<50;i++){await Task.Delay(8);var state=gyroRouter.Sample(Native.Now);max=Math.Max(max,Math.Sqrt(state.RX*state.RX+state.RY*state.RY));}
        Console.WriteLine($"Actual sensor callbacks: {readings}; calibrated stationary peak stick {max:F4}; {gyro.Status}");
        if(readings<20)throw new Exception("No live sensor callback stream");
        gyroRouter.ToggleMenu();gyro.Sync();if(!gyroRouter.Sample(Native.Now).Neutral)throw new Exception("Menu retained gyro");
        gyroRouter.ToggleMenu();gyroRouter.SetFocus(false);gyro.Sync();if(!gyroRouter.Sample(Native.Now).Neutral)throw new Exception("Focus loss retained gyro");
        Console.WriteLine("PASS ACPI tablet sensor stream, calibration, default off, menu/focus suspension. Physical rotation/detached use pending owner check.");
    }
    finally{sensor.ReadingChanged-=Count;}return;
}

if(args.Contains("--diagnose-vigem"))
{
    using var client=new ViGEmClient();var controller=client.CreateXbox360Controller();
    object Handle(object o){for(var t=o.GetType();t is not null;t=t.BaseType){var p=t.GetProperty("NativeHandle",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(p is not null)return p.GetValue(o)!;}throw new Exception("Native handle missing");}
    var add=typeof(ViGEmClient).GetMethod("vigem_target_add",BindingFlags.NonPublic|BindingFlags.Static)!;
    var result=add.Invoke(null,[Handle(client),Handle(controller)]);Console.WriteLine("vigem_target_add: "+result+" / 0x"+Convert.ToUInt32(result).ToString("X8"));
    foreach(System.Diagnostics.ProcessModule m in System.Diagnostics.Process.GetCurrentProcess().Modules)if(m.ModuleName.Contains("vigem",StringComparison.OrdinalIgnoreCase))Console.WriteLine(m.FileName+" "+m.FileVersionInfo.FileVersion);
    if(Convert.ToUInt32(result)==0x20000000)typeof(ViGEmClient).GetMethod("vigem_target_remove",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[Handle(client),Handle(controller)]);
    return;
}

if(args.Contains("--sensors"))
{
    try{var gyro=Gyrometer.GetDefault();var accel=Accelerometer.GetDefault();var orientation=OrientationSensor.GetDefault();Console.WriteLine(JsonSerializer.Serialize(new{Gyrometer=gyro?.DeviceId,Accelerometer=accel?.DeviceId,Orientation=orientation?.DeviceId,Reading=gyro?.GetCurrentReading()},new JsonSerializerOptions{WriteIndented=true}));}
    catch(Exception e){Console.WriteLine(e.ToString());Environment.ExitCode=1;}return;
}
var foreground=Native.GetForegroundWindow();Native.GetWindowThreadProcessId(foreground,out var pid);
Console.WriteLine($"Foreground PID {pid}; test PID {Environment.ProcessId}");
try{using var p=System.Diagnostics.Process.GetProcessById((int)pid);if(p.ProcessName.Contains("QueensDomain")&&!args.Contains("--game-test"))throw new InvalidOperationException("Put another app in foreground before broad XInput tests; game input is not safe for an unattended report sweep.");}catch(ArgumentException){}
var baseline=new HashSet<int>();for(int slot=0;slot<4;slot++)if(XInput.Get(slot,out _)==0)baseline.Add(slot);
Console.WriteLine("Existing occupied XInput slots: "+string.Join(",",baseline));
using var output=new XInputOutput();output.Connect();Thread.Sleep(500);
var connected=-1;Console.WriteLine("Reported UserIndex: "+output.UserIndex);
if(args.Contains("--game-test"))
{for(int slot=0;slot<4;slot++)if(!baseline.Contains(slot)&&XInput.Get(slot,out _)==0){connected=slot;break;}}
else
{
    // HC/physical controllers may reconnect during enumeration; match our unique report,
    // rather than assuming an occupied/new slot belongs to this client.
    output.Publish(new(LT:191,RT:73,LX:.123,LY:-.456,RX:.222,RY:-.333));Thread.Sleep(100);
    for(int slot=0;slot<4;slot++)if(XInput.Get(slot,out var marker)==0)
    {Console.WriteLine($"slot {slot}: "+JsonSerializer.Serialize(marker.Pad,new JsonSerializerOptions{IncludeFields=true}));if(marker.Pad.LT==191&&marker.Pad.RT==73&&Math.Abs(marker.Pad.LX-4030)<5)connected=slot;}
    output.Publish(default);
}
if(connected<0)throw new Exception("No new XInput slot (all slots used or hidden by existing HidHide policy)");
Console.WriteLine("Pilot XInput slot: "+connected);
if(args.Contains("--bridge-test"))
{
    var arg=Array.IndexOf(args,"--bridge-test");var module=NativeLibrary.Load(Path.GetFullPath(args[arg+1]));var get=Marshal.GetDelegateForFunctionPointer<XInput.GetDelegate>(NativeLibrary.GetExport(module,"XInputGetState"));
    try
    {
        output.Publish(new(LT:73,RT:191,LX:.45,LY:-.2));Thread.Sleep(50);
        var result=get(0,out var read);if(result!=0||read.Pad.LT!=73||read.Pad.RT!=191||Math.Abs(read.Pad.LX-14745)>3)throw new Exception("Bridge did not route pilot XInput state to slot zero");
        Console.WriteLine("PASS game-local bridge routes the existing pilot pad to first XInput slot; calls "+output.BridgePolls);
        Thread.Sleep(350);var expired=get(0,out _);var direct=XInput.Get(0,out _);if(expired!=direct)throw new Exception("Expired route did not restore direct device status");
        output.Dispose();if(get(0,out _)!=XInput.Get(0,out _))throw new Exception("Disabled route did not restore direct device status");Console.WriteLine("PASS stale heartbeat and disposal restore unmodified system XInput path");
    }
    finally{NativeLibrary.Free(module);}return;
}
if(args.Contains("--game-test"))
{
    // Explicit bounded controller-output test, never keyboard/mouse injection.
    var index=Array.IndexOf(args,"--game-test");var input=args.Length>index+1?args[index+1]:"Down";
    for(int tick=0;tick<125;tick++){output.Publish(default);Thread.Sleep(20);}output.Publish(new(Buttons:ContactRouter.Mask(input)));for(int tick=0;tick<12;tick++){output.Publish(new(Buttons:ContactRouter.Mask(input)));Thread.Sleep(20);}for(int tick=0;tick<15;tick++){output.Publish(default);Thread.Sleep(20);}Console.WriteLine("Controller pulse "+input+"; bridge polls "+output.BridgePolls);return;
}
int assertions=0;
void Check(PadState s,string label)
{
    output.Publish(s);Thread.Sleep(12);if(XInput.Get(connected,out var actual)!=0)throw new Exception(label+": XInput unavailable");
    static bool Axis(double expected,short actual)=>Math.Abs(expected-actual/(actual<0?32768.0:32767))<.0001;
    if(actual.Pad.Buttons!=s.Buttons||actual.Pad.LT!=s.LT||actual.Pad.RT!=s.RT||!Axis(s.LX,actual.Pad.LX)||!Axis(s.LY,actual.Pad.LY)||!Axis(s.RX,actual.Pad.RX)||!Axis(s.RY,actual.Pad.RY))throw new Exception(label+": readback mismatch expected "+JsonSerializer.Serialize(s)+" actual "+JsonSerializer.Serialize(actual.Pad,new JsonSerializerOptions{IncludeFields=true}));assertions++;
}
Check(default,"neutral");
foreach(var bit in new ushort[]{1,2,4,8,0x10,0x20,0x40,0x80,0x100,0x200,0x1000,0x2000,0x4000,0x8000}){Check(new(Buttons:bit),"button "+bit.ToString("X"));Check(default,"release");}
foreach(var hat in new ushort[]{1,9,8,10,2,6,4,5})Check(new(Buttons:hat),"hat "+hat);
foreach(var value in new[]{-1.0,-.75,-.1,0,.1,.75,1})Check(new(LX:value,LY:-value,RX:-value,RY:value),"axes "+value);
Check(new(LT:255,RT:127),"triggers");
var r=new ContactRouter(GameProfile.QueensDraft());r.SetSize(1280,800);r.SetFocus(true);double time=1;
void Touch(int id,TouchPhase phase,double x,double y){r.Process(new(id,phase,new(x,y),time,time),time);}
Touch(1,TouchPhase.Down,200,600);Touch(2,TouchPhase.Down,800,300);Touch(3,TouchPhase.Down,1134,636);Touch(4,TouchPhase.Down,1199,490);
time+=.016;Touch(1,TouchPhase.Move,270,600);Touch(2,TouchPhase.Move,815,300);Check(r.Sample(time),"real router simultaneous report");
time+=.08;Check(r.Sample(time),"stationary right-stick expiry while action held");
foreach(var reason in new[]{"focus","hide","menu","editor","profile","suspend","shutdown"}){r.Clear(reason);Check(r.Sample(time),"neutral "+reason);}
for(int i=0;i<100;i++){Check(new(Buttons:0x1200,RT:255,LX:.8,LY:.3,RX:-.2,RY:.4),"cycle press");Check(default,"cycle release");}
output.Dispose();Thread.Sleep(100);if(XInput.Get(connected,out _)==0)throw new Exception("Disconnected pilot remained visible");
using(var again=new XInputOutput()){again.Connect();Thread.Sleep(100);again.Publish(default);}
Console.WriteLine($"PASS {assertions} XInput readback reports, disposal and reconnection; no driver installation or global remapping performed.");

internal static class XInput
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]internal delegate uint GetDelegate(int slot,out State state);
    [StructLayout(LayoutKind.Sequential)]internal struct Gamepad{public ushort Buttons;public byte LT,RT;public short LX,LY,RX,RY;}
    [StructLayout(LayoutKind.Sequential)]internal struct State{public uint Packet;public Gamepad Pad;}
    [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")]internal static extern uint Get(int slot,out State state);
}


