// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using GlassPad.Engine;

namespace GlassPad.Queens;
internal static class Native
{
    private static readonly Dictionary<string,(long Length,long Modified,string Hash)> Hashes=[];
    [StructLayout(LayoutKind.Sequential)]internal struct Point{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]internal struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]internal struct TouchInput{public int X,Y;public nint Source;public uint Id,Flags,Mask,Time;public nuint Extra;public uint Width,Height;}
    [StructLayout(LayoutKind.Sequential)]internal struct PointerInfo
    {public uint Type,Id,Frame,Flags;public nint Source,Target;public Point Pixel,Himetric,PixelRaw,HimetricRaw;public uint Time,History;public int Input;public uint Keys;public ulong Performance;public uint ButtonChange;}
    [StructLayout(LayoutKind.Sequential)]internal struct PointerTouchInfo
    {public PointerInfo Pointer;public uint Flags,Mask;public Rect Contact,ContactRaw;public uint Orientation,Pressure;}
    [DllImport("user32.dll",SetLastError=true)]internal static extern bool GetPointerFrameTouchInfoHistory(uint id,ref uint entries,ref uint pointers,[Out]PointerTouchInfo[]? info);
    [DllImport("user32.dll",SetLastError=true)]internal static extern bool GetPointerTouchInfo(uint id,out PointerTouchInfo info);
    [DllImport("user32.dll")]internal static extern bool SkipPointerFrameMessages(uint id);
    [DllImport("user32.dll")]internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]internal static extern uint GetWindowThreadProcessId(nint hwnd,out uint pid);
    [DllImport("user32.dll")]internal static extern bool GetClientRect(nint hwnd,out Rect rect);
    [DllImport("user32.dll")]internal static extern bool ClientToScreen(nint hwnd,ref Point point);
    [DllImport("user32.dll")]internal static extern bool ScreenToClient(nint hwnd,ref Point point);
    [DllImport("user32.dll")]internal static extern bool RegisterTouchWindow(nint hwnd,uint flags);
    [DllImport("user32.dll")]internal static extern bool UnregisterTouchWindow(nint hwnd);
    [DllImport("user32.dll")]internal static extern bool GetTouchInputInfo(nint handle,int count,[Out]TouchInput[] points,int size);
    [DllImport("user32.dll")]internal static extern bool CloseTouchInputHandle(nint handle);
    [DllImport("user32.dll")]internal static extern nint GetMessageExtraInfo();
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]internal static extern nint GetWindowLongPtr(nint hwnd,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]internal static extern nint SetWindowLongPtr(nint hwnd,int index,nint value);
    [DllImport("user32.dll")]internal static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll")]internal static extern bool RegisterHotKey(nint hwnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]internal static extern bool UnregisterHotKey(nint hwnd,int id);
    [DllImport("user32.dll")]internal static extern int GetSystemMetrics(int index);
    internal static double Now=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    internal static void NoActivate(nint hwnd)
    {var style=GetWindowLongPtr(hwnd,-20).ToInt64();SetWindowLongPtr(hwnd,-20,(nint)(style|0x08000000L|0x80L));}
    internal static (nint Handle,string Path)? FindGame(GameProfile profile)
    {
        foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(profile.Executable)))
        {try{var path=p.MainModule?.FileName;if(path is null||p.MainWindowHandle==0)continue;var info=new FileInfo(path);string hash;lock(Hashes){if(!Hashes.TryGetValue(path,out var cached)||cached.Length!=info.Length||cached.Modified!=info.LastWriteTimeUtc.Ticks){using var f=File.OpenRead(path);cached=(info.Length,info.LastWriteTimeUtc.Ticks,Convert.ToHexString(SHA256.HashData(f)));Hashes[path]=cached;}hash=cached.Hash;}if(hash.Equals(profile.ExecutableSha256,StringComparison.OrdinalIgnoreCase))return(p.MainWindowHandle,path);}catch{}finally{p.Dispose();}}
        return null;
    }
}
