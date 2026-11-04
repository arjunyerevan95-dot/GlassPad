// SPDX-License-Identifier: GPL-3.0-or-later
// OS-injected, self-owned app integration check. This is NOT physical touchscreen evidence.
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using GlassPad.Engine;
namespace GlassPad.Queens;
internal static class TouchIntegration
{
    [StructLayout(LayoutKind.Sequential)]private struct PointerInfo
    {public uint Type,Id,Frame,Flags;public nint Source,Target;public Native.Point Pixel,Himetric,PixelRaw,HimetricRaw;public uint Time,History;public int Input;public uint Keys;public ulong Performance;public uint ButtonChange;}
    [StructLayout(LayoutKind.Sequential)]private struct TouchInfo
    {public PointerInfo Pointer;public uint Flags,Mask;public Native.Rect Contact,ContactRaw;public uint Orientation,Pressure;}
    [DllImport("user32.dll",SetLastError=true)]private static extern bool InitializeTouchInjection(uint max,uint feedback);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool InjectTouchInput(uint count,[In]TouchInfo[] contacts);
    internal static async Task RunAsync(Window owner,string directory)
    {
        Directory.CreateDirectory(directory);var lines=new List<string>();GameplayOverlay? overlay=null;var contacts=new TouchInfo[5];var down=false;
        void Check(bool result,string label){if(!result)throw new Exception(label);lines.Add("PASS "+label);}
        void Inject(uint flag)
        {for(int i=0;i<contacts.Length;i++)contacts[i].Pointer.Flags=flag;if(!InjectTouchInput((uint)contacts.Length,contacts))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),"InjectTouchInput");}
        try
        {
            owner.Width=Math.Min(1000,SystemParameters.WorkArea.Width-40);owner.Height=Math.Min(620,SystemParameters.WorkArea.Height-60);owner.Left=SystemParameters.WorkArea.Left+10;owner.Top=SystemParameters.WorkArea.Top+10;owner.UpdateLayout();
            var profile=GameProfile.QueensDraft();var hwnd=new WindowInteropHelper(owner).Handle;
            var originalForeground=Native.GetForegroundWindow();
            overlay=new GameplayOverlay(profile,hwnd);overlay.Show();await Task.Delay(500);
            Check(Native.GetForegroundWindow()==originalForeground,"test overlay preserves original foreground; lab emits no controller output");
            if(!InitializeTouchInjection(10,3))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),"InitializeTouchInjection");
            var w=overlay.ActualWidth;var h=overlay.ActualHeight;var u=Math.Min(w,h);
            Vec Control(string id){var c=profile.Controls.First(x=>x.Id==id);return new(c.X*w,c.Y*h);}
            var positions=new[]{new Vec(w*.18,h*.74),new Vec(w*.62,h*.30),Control("primary"),Control("jump"),Control("dash")};
            void Locate(int i,Vec position)
            {var pixel=overlay.PointToScreen(new Point(position.X,position.Y));var x=(int)pixel.X;var y=(int)pixel.Y;contacts[i].Pointer.Pixel=new(){X=x,Y=y};contacts[i].Contact=new(){Left=x-2,Right=x+2,Top=y-2,Bottom=y+2};}
            for(int i=0;i<contacts.Length;i++){contacts[i]=new(){Pointer=new(){Type=2,Id=(uint)(1+i)},Mask=7,Orientation=90,Pressure=512};Locate(i,positions[i]);}
            lines.Add($"Injection structure bytes {Marshal.SizeOf<TouchInfo>()}; desktop {Native.GetSystemMetrics(0)}x{Native.GetSystemMetrics(1)}; points "+string.Join(" ",contacts.Select(c=>$"{c.Pointer.Pixel.X},{c.Pointer.Pixel.Y}")));
            Inject(0x10006);down=true;await Task.Delay(60);
            Check(overlay.Router.View(Native.Now).Contacts==5,"five simultaneous native contacts arrive in one central router");
            positions[0]+=new Vec(u*.15,0);positions[1]+=new Vec(u*.03,0);for(int i=0;i<5;i++)Locate(i,positions[i]);Inject(0x20006);await Task.Delay(12);
            var state=overlay.Router.Sample(Native.Now);
            Check(state.LX>.5&&state.RX>0&&state.RT==255&&(state.Buttons&0x1200)==0x1200,"native WM_POINTER frame delivers move + swipe + attack + jump + dash simultaneously");
            await Task.Delay(80);state=overlay.Router.Sample(Native.Now);Check(state.RX==0&&state.RT==255&&state.LX>.5,"stationary swipe expires independently while move and attack remain held");
            for(int i=0;i<contacts.Length;i++)contacts[i].Pointer.Flags=i==2?0x40000u:0x20006u;
            if(!InjectTouchInput((uint)contacts.Length,contacts))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());await Task.Delay(25);
            state=overlay.Router.Sample(Native.Now);Check(state.RT==0&&state.LX>.5&&(state.Buttons&0x1200)==0x1200,"releasing attack preserves other fingers and their capture ownership");contacts=contacts.Where((_,i)=>i!=2).ToArray();
            Inject(0x40000);down=false;await Task.Delay(40);Check(overlay.Router.Sample(Native.Now).Neutral&&overlay.Router.View(Native.Now).Contacts==0,"all five native releases neutralize the controller");
            Check(Native.GetForegroundWindow()==originalForeground,"native touches do not activate overlay or forward mouse clicks");
            lines.Add($"Native packets {overlay.TouchMetrics.Packets}; maximum contacts {overlay.TouchMetrics.Contacts}");
            lines.Add("PHYSICAL Legion Go multitouch, palm rejection and finger timing still require hands-on verification.");
        }
        catch(Exception e){lines.Add("FAIL "+e);}
        finally{if(down){try{Inject(0x40000);}catch{}}overlay?.Close();File.WriteAllLines(Path.Combine(directory,"native-touch-integration.log"),lines);}
    }
}
