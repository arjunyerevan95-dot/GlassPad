// SPDX-License-Identifier: GPL-3.0-or-later
using GlassPad.Engine;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
namespace GlassPad.Queens;
internal sealed class TabletGyro : IDisposable
{
    private readonly ContactRouter _router;
    private readonly Gyrometer? _sensor;
    private bool _running;
    private uint _previousInterval;
    private DisplayOrientations _previousTransform;
    public bool Available=>_sensor is not null;
    public string Status{get;private set;}="Tablet gyro unavailable";
    public string DeviceId=>_sensor?.DeviceId??"";
    public TabletGyro(ContactRouter router)
    {
        _router=router;
        try
        {
            var sensor=Gyrometer.GetDefault();
            // The integrated AMD sensor observed on this Legion Go is ACPI enumerated.
            // Never silently substitute a USB/Bluetooth controller's IMU.
            if(sensor is not null&&sensor.DeviceId.Contains("ACPI#AMDI0080",StringComparison.OrdinalIgnoreCase))
            {_sensor=sensor;Status="Tablet gyro ready • off";}
            else Status=sensor is null?"No Windows tablet Gyrometer exposed":"Default gyro is not verified AMD tablet sensor";
        }
        catch(Exception e){Status="Tablet gyro unavailable: "+e.Message;}
    }
    public void Sync()
    {
        var wanted=_router.AcceptingGyro;
        if(wanted==_running||_sensor is null)return;
        if(wanted)
        {
            _previousInterval=_sensor.ReportInterval;_previousTransform=_sensor.ReadingTransform;
            _sensor.ReportInterval=Math.Max(_sensor.MinimumReportInterval,8);
            _sensor.ReadingTransform=Enum.Parse<DisplayOrientations>(_router.Profile.Gyro.ScreenOrientation);
            _sensor.ReadingChanged+=Reading;_running=true;Status="Tablet gyro ON";
        }
        else
        {_sensor.ReadingChanged-=Reading;_sensor.ReportInterval=_previousInterval;_sensor.ReadingTransform=_previousTransform;_running=false;_router.SetGyro(default,Native.Now);Status="Tablet gyro off / suspended";}
    }
    private void Reading(Gyrometer sender,GyrometerReadingChangedEventArgs args)
    {
        var now=Native.Now;var r=args.Reading;
        // Driver timestamps are wall-clock based. Reject buffered history after a stall.
        if((DateTimeOffset.Now-r.Timestamp).TotalMilliseconds>100||!_router.AcceptingGyro){_router.SetGyro(default,now);return;}
        var p=_router.Profile;_router.SetGyro(GyroMath.Map(r.AngularVelocityX,r.AngularVelocityY,r.AngularVelocityZ,p.Gyro,p.Look),now);
    }
    public static async Task<(double Yaw,double Pitch)> CalibrateAsync(GameProfile profile)
    {
        var sensor=Gyrometer.GetDefault();if(sensor is null||!sensor.DeviceId.Contains("ACPI#AMDI0080",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Verified AMD tablet gyro is unavailable");
        var previous=sensor.ReportInterval;var transform=sensor.ReadingTransform;var samples=new List<Vec>();var gate=new object();
        void Reading(Gyrometer sender,GyrometerReadingChangedEventArgs args)
        {var r=args.Reading;double Axis(string id)=>id switch{"X"=>r.AngularVelocityX,"Y"=>r.AngularVelocityY,_=>r.AngularVelocityZ};lock(gate)samples.Add(new(Axis(profile.Gyro.YawAxis),Axis(profile.Gyro.PitchAxis)));}
        try
        {
            sensor.ReportInterval=Math.Max(sensor.MinimumReportInterval,8);sensor.ReadingTransform=Enum.Parse<DisplayOrientations>(profile.Gyro.ScreenOrientation);sensor.ReadingChanged+=Reading;
            await Task.Delay(1500);
        }
        finally{sensor.ReadingChanged-=Reading;sensor.ReportInterval=previous;sensor.ReadingTransform=transform;}
        lock(gate)
        {
            if(samples.Count<20)throw new InvalidOperationException("Insufficient fresh gyro readings; calibration was not changed");
            var x=samples.Average(v=>v.X);var y=samples.Average(v=>v.Y);var variance=samples.Average(v=>(v-new Vec(x,y)).Length*(v-new Vec(x,y)).Length);
            if(variance>4||Math.Abs(x)>10||Math.Abs(y)>10)throw new InvalidOperationException("Tablet moved during calibration; keep it still and retry");return(x,y);
        }
    }
    public void Dispose()
    {if(_sensor is not null&&_running){_sensor.ReadingChanged-=Reading;_sensor.ReportInterval=_previousInterval;_sensor.ReadingTransform=_previousTransform;}_running=false;_router.SetGyro(default,Native.Now);}
}
