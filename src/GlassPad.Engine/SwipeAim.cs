// SPDX-License-Identifier: GPL-3.0-or-later
// Adapted November 4, 2026 from BCDMouseJoystick.swift, WO-BCD-TOUCH-001
// (arjunkl/bcd-touch @ 907c6ea). Original touch-fork implementation, GPL-3.0-or-later.
// Windows adaptation: absolute samples, reject stale/out-of-order delivery, radial response.
namespace GlassPad.Engine;

public sealed class SwipeAim
{
    private Vec _position, _value;
    private double _source, _delivery, _cadence=1.0/60, _deliveryCadence=1.0/60, _expiry;
    public void Begin(Vec position,double source,double delivery){Reset();_position=position;_source=source;_delivery=delivery;}
    public void Reset(){_value=default;_expiry=0;_cadence=_deliveryCadence=1.0/60;_source=_delivery=double.NegativeInfinity;}
    public void Move(Vec position,double source,double delivery,LookCalibration c)
    {
        if(!double.IsFinite(source)||!double.IsFinite(delivery)||!double.IsFinite(position.X)||!double.IsFinite(position.Y)||source<=_source||delivery<_delivery)return;
        var delta=position-_position;var gap=source-_source;
        // A complete native pointer frame repeats stationary contacts whenever another
        // finger moves. Such samples must neither zero fresh motion nor renew its expiry.
        // Keep the last motion timestamp so velocity reflects this finger's own cadence.
        if(delta.X==0&&delta.Y==0)return;
        _position=position;_source=source;
        var interval=gap>.1?_cadence:Math.Max(gap,1.0/500);
        if(gap<=.1)_cadence=Math.Clamp(gap,1.0/500,1.0/20);
        var velocity=new Vec(delta.X*(c.InvertX?-1:1)*c.Horizontal, -delta.Y*(c.InvertY?-1:1)*c.Vertical)*(1/(interval*c.FullSpeed));
        var length=velocity.Length;
        _value=length>0?velocity*((c.GameDeadzone+(1-c.GameDeadzone)*Math.Pow(Math.Min(1,length),c.Response))/length):default;
        var deliveryGap=delivery-_delivery;
        if(deliveryGap>0&&deliveryGap<=.1)_deliveryCadence=Math.Clamp(deliveryGap,1.0/500,1.0/30);
        _delivery=delivery;
        _expiry=delivery+Math.Clamp(_deliveryCadence*1.25+.004,.012,.046);
    }
    public Vec Sample(double now)=>double.IsFinite(now)&&now<_expiry?_value:default;
}
