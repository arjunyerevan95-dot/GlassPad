// SPDX-License-Identifier: GPL-3.0-or-later
namespace GlassPad.Engine;
public static class GyroMath
{
    public static Vec Map(double x,double y,double z,GyroCalibration c,LookCalibration look)
    {
        if(!double.IsFinite(x)||!double.IsFinite(y)||!double.IsFinite(z))return default;
        double Axis(string name)=>name switch{"X"=>x,"Y"=>y,"Z"=>z,_=>0};
        var yaw=Axis(c.YawAxis)-c.BiasX;var pitch=Axis(c.PitchAxis)-c.BiasY;
        double Trim(double v)=>Math.Abs(v)<=c.Deadzone?0:Math.CopySign(Math.Abs(v)-c.Deadzone,v);
        var rate=new Vec(Trim(yaw)*(c.InvertX?-1:1),Trim(pitch)*(c.InvertY?-1:1))*(c.Sensitivity/90);
        var length=rate.Length;if(length==0)return default;
        return rate*((look.GameDeadzone+(1-look.GameDeadzone)*Math.Min(1,length))/length);
    }
}
