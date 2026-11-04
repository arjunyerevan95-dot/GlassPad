// SPDX-License-Identifier: GPL-3.0-or-later
using GlassPad.Engine;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.IO.MemoryMappedFiles;

namespace GlassPad.Queens;
internal sealed class XInputOutput : IDisposable
{
    private ViGEmClient? _client;
    private IXbox360Controller? _controller;
    private PadState? _last;
    private long _submittedAt;
    private MemoryMappedFile? _route;
    private MemoryMappedViewAccessor? _routeView;
    private int _slot=-1;
    public int UserIndex => _controller?.UserIndex ?? -1;
    public void Connect()
    {
        _client=new ViGEmClient();_controller=_client.CreateXbox360Controller();_controller.AutoSubmitReport=false;_controller.Connect();
        _route=MemoryMappedFile.CreateOrOpen("Local\\GlassPadQueens.XInputRoute.v1",64);_routeView=_route.CreateViewAccessor();_routeView.Write(0,0x47505131);_routeView.Write(4,1);_routeView.Write(12,0);_routeView.Write(32,0);Publish(default);
    }
    public void Publish(PadState s)
    {
        var now=Environment.TickCount64;
        if(_controller is not null&&_routeView is not null)
        {
            if(_slot<0){try{_slot=_controller.UserIndex;}catch{}}
            if(_slot>=0){_routeView.Write(8,_slot);_routeView.Write(16,now);_routeView.Write(12,1);}
        }
        if(_controller is null||(_last==s&&now-_submittedAt<250))return;
        _controller.SetButtonsFull(s.Buttons);
        _controller.SetSliderValue(Xbox360Slider.LeftTrigger,s.LT);_controller.SetSliderValue(Xbox360Slider.RightTrigger,s.RT);
        static short Axis(double v)=>(short)Math.Round(Math.Clamp(v,-1,1)*(v<0?32768:32767));
        _controller.SetAxisValue(Xbox360Axis.LeftThumbX,Axis(s.LX));_controller.SetAxisValue(Xbox360Axis.LeftThumbY,Axis(s.LY));
        _controller.SetAxisValue(Xbox360Axis.RightThumbX,Axis(s.RX));_controller.SetAxisValue(Xbox360Axis.RightThumbY,Axis(s.RY));
        _controller.SubmitReport();_last=s;_submittedAt=now;
    }
    public int BridgePolls=>_routeView?.ReadInt32(32)??0;
    public void Dispose(){try{Publish(default);_controller?.Disconnect();}finally{_routeView?.Write(12,0);_routeView?.Dispose();_route?.Dispose();_routeView=null;_route=null;_client?.Dispose();_controller=null;_client=null;}}
}
